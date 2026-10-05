using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace MateEngine.Shoji
{
    // Entry points called from Assembly-CSharp (inserted by the patcher).
    // Under ShojiWM the X11 view of MateEngine (xwayland-satellite) has no
    // real window position, no pointer outside its own surfaces and drops
    // move requests; these hooks answer from the compositor instead.
    // Outside a ShojiWM session, or with MATEENGINE_SHOJI_BRIDGE=0, every hook
    // reports "not handled" and the original code runs unchanged.
    public static partial class ShojiBridge
    {
        static bool _initialized;
        static bool _active;
        static ShojiIpc _ipc;
        static int _mainThread;
        static int _snapshotFrame = -1;
        static volatile ShojiSnapshot _frameSnapshot;

        static int _errorCount;

        static bool Active
        {
            get
            {
                if (!_initialized) Initialize();
                return _active;
            }
        }

        // First reached from WindowManager.Awake (QueryMonitors), on the main thread.
        static void Initialize()
        {
            _initialized = true;
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            try
            {
                if (Environment.GetEnvironmentVariable("MATEENGINE_SHOJI_BRIDGE") == "0") return;
                string desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
                if (desktop.IndexOf("ShojiWM", StringComparison.OrdinalIgnoreCase) < 0) return;
                string path = ShojiIpc.DefaultSocketPath();
                if (!File.Exists(path))
                {
                    Debug.Log("[ShojiBridge] no IPC socket at " + path + "; bridge off");
                    return;
                }
                _ipc = new ShojiIpc(path);
                _ipc.Start();
                _active = true;
            }
            catch (Exception e)
            {
                _ipc = null;
                _active = false;
                Report(e);
            }
        }

        // One compositor reply per frame: every hook of a frame (HandHolder reads
        // the pointer and the window position separately) sees the same one,
        // even when the IPC thread publishes a newer reply in between.
        static ShojiSnapshot Snapshot()
        {
            if (!Active) return null;
            if (Thread.CurrentThread.ManagedThreadId != _mainThread) return _frameSnapshot;
            int frame = Time.frameCount;
            if (frame != _snapshotFrame)
            {
                _snapshotFrame = frame;
                _frameSnapshot = _ipc.Unsupported ? null : _ipc.Latest;
            }
            return _frameSnapshot;
        }

        static void Report(Exception e)
        {
            if (_errorCount++ < 5) Debug.LogWarning("[ShojiBridge] " + e);
        }

        public static bool IsWindowIntegrationReady()
        {
            var snapshot = Snapshot();
            return snapshot != null && _ipc.Connected && snapshot.HasWindow && snapshot.HasPointer &&
                snapshot.HasSurfaces && snapshot.MotionSupported;
        }

        // -1: unavailable/stale, 0: active or inhibited, 1: compositor idle.
        public static int GetScreensaverState(int timeout, bool enabled)
        {
            if (!Active) return -1;
            _ipc.SetScreensaver(timeout, enabled);
            var snapshot = Snapshot();
            if (!_ipc.IsFresh(snapshot) || !snapshot.HasIdleState || snapshot.IdleTimeout != timeout ||
                snapshot.IdleEnabled != enabled || !snapshot.WindowVisible) return -1;
            return enabled && snapshot.DesktopIdle ? 1 : 0;
        }

        // ---- WindowManager geometry hooks ------------------------------------

        // Only what the compositor last reported: window and pointer come from
        // the same reply, so pointer-relative maths (hands, menus) stay
        // consistent. A move shows up with the next reply, which is requested
        // right after the move is sent.
        public static bool TryGetWindowPosition(out Vector2Int position)
        {
            position = default(Vector2Int);
            try
            {
                ShojiSnapshot snapshot = Snapshot();
                if (snapshot == null || !snapshot.HasWindow) return false;
                position = snapshot.Window.position;
                return true;
            }
            catch (Exception e) { Report(e); return false; }
        }

        public static bool TryGetPointer(out Vector2Int position)
        {
            position = default(Vector2Int);
            try
            {
                ShojiSnapshot snapshot = Snapshot();
                if (snapshot == null || !snapshot.HasPointer) return false;
                position = snapshot.Pointer;
                return true;
            }
            catch (Exception e) { Report(e); return false; }
        }

        public static bool TryGetWindowRect(IntPtr window, IntPtr unityWindow, out RectInt rect, out bool found)
        {
            rect = default(RectInt);
            found = false;
            try
            {
                if (IsVirtualHandle(window))
                {
                    ShojiSurface surface = Surface(window);
                    found = surface != null;
                    if (found) rect = surface.Rect;
                    return true; // A closed virtual window must never reach Xlib.
                }
                if (window == IntPtr.Zero || window != unityWindow) return false;
                ShojiSnapshot snapshot = Snapshot();
                if (snapshot == null || !snapshot.HasWindow) return false;
                rect = snapshot.Window;
                found = true;
                return true;
            }
            catch (Exception e) { Report(e); return false; }
        }

        static readonly Dictionary<string, IntPtr> _surfaceHandles = new Dictionary<string, IntPtr>();
        static readonly Dictionary<IntPtr, string> _surfaceKeys = new Dictionary<IntPtr, string>();
        static long _nextSurfaceHandle = -1;
        static readonly Dictionary<string, IntPtr> _outputHandles = new Dictionary<string, IntPtr>();
        static ShojiOutput[] _monitorLayout;

        static bool IsVirtualHandle(IntPtr handle)
        {
            lock (_surfaceHandles) return _surfaceKeys.ContainsKey(handle);
        }

        static ShojiSurface Surface(IntPtr handle)
        {
            string key;
            ShojiSnapshot snapshot = Snapshot();
            lock (_surfaceHandles) if (!_surfaceKeys.TryGetValue(handle, out key)) return null;
            if (snapshot == null || !snapshot.HasSurfaces) return null;
            foreach (ShojiSurface surface in snapshot.Surfaces) if (surface.Id == key) return surface;
            return null;
        }

        public static bool TryGetStackingList(IntPtr unityWindow, out List<IntPtr> windows)
        {
            windows = null;
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null || !snapshot.HasSurfaces || unityWindow == IntPtr.Zero) return false;
            windows = new List<IntPtr>();
            foreach (ShojiSurface surface in snapshot.Surfaces)
            {
                if (!surface.Visible && !(surface.Dock &&
                    (surface.Id == _seatTargetId || surface.Id == _dockSnapIntent))) continue;
                if (surface.Self) { windows.Add(unityWindow); continue; }
                IntPtr handle;
                lock (_surfaceHandles)
                {
                    if (!_surfaceHandles.TryGetValue(surface.Id, out handle))
                    {
                        handle = new IntPtr(_nextSurfaceHandle--);
                        _surfaceHandles.Add(surface.Id, handle);
                        _surfaceKeys.Add(handle, surface.Id);
                    }
                }
                windows.Add(handle);
            }
            return true;
        }

        // Flags: visible, maximized, fullscreen, dock, desktop. Even missing
        // virtual handles are handled here; none may leak into an X11 call.
        public static bool TryWindowFlag(IntPtr handle, int flag, out bool value)
        {
            value = false;
            if (!IsVirtualHandle(handle)) return false;
            ShojiSurface surface = Surface(handle);
            if (surface == null) return true;
            switch (flag)
            {
                case 0: value = surface.Visible || (surface.Dock &&
                    (surface.Id == _seatTargetId || surface.Id == _dockSnapIntent)); break;
                case 1: value = surface.Maximized; break;
                case 2: value = surface.Fullscreen; break;
                case 3: value = surface.Dock; break;
            }
            return true;
        }

        public static bool TryGetWindowPid(IntPtr handle, out int pid)
        {
            pid = 0;
            return IsVirtualHandle(handle);
        }

        public static bool TryGetClassName(IntPtr handle, out string name)
        {
            name = "";
            if (!IsVirtualHandle(handle)) return false;
            ShojiSurface surface = Surface(handle);
            if (surface != null) name = surface.ClassName;
            return true;
        }

        public static bool TryIsOccluded(out bool covered)
        {
            covered = false;
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null) return false;
            if (!snapshot.WindowVisible) { covered = true; return true; }
            if (!snapshot.HasSurfaces || !snapshot.HasPointer) return false;
            covered = snapshot.Covered;
            return true;
        }

        public static bool AllowPointerTracking()
        {
            ShojiSnapshot snapshot = Snapshot();
            return snapshot == null || snapshot.WindowVisible;
        }

        public static bool GetMouseButton(int button)
        {
            return AllowPointerTracking() && Input.GetMouseButton(button);
        }

        public static Vector3 GetUnityMousePosition()
        {
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot != null && !snapshot.WindowVisible) return new Vector3(-100000, -100000, 0);
            if (snapshot == null || !snapshot.HasWindow || !snapshot.HasPointer) return Input.mousePosition;
            RectInt window = snapshot.Window;
            return new Vector3((snapshot.Pointer.x - window.x) * (float)Screen.width / window.width,
                Screen.height - (snapshot.Pointer.y - window.y) * (float)Screen.height / window.height, 0);
        }

        public static Vector3 ScreenToWorldPointer(Camera camera, Vector3 original)
        {
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot != null && (!snapshot.WindowVisible || (snapshot.HasWindow && snapshot.HasPointer)))
            {
                Vector3 pointer = GetUnityMousePosition();
                original.x = pointer.x;
                original.y = pointer.y;
            }
            // Keep the original depth (hand/chest) and IK rules.
            return camera.ScreenToWorldPoint(original);
        }

        public static bool TryGetTaskbarRect(out RectInt rect)
        {
            rect = default(RectInt);
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null || !snapshot.HasSurfaces || !snapshot.HasWindow) return false;
            ShojiOutput output = OutputFor(snapshot, snapshot.Window);
            if (output == null) return true;
            foreach (ShojiSurface surface in snapshot.Surfaces)
            {
                if (surface.Dock && (surface.Visible || surface.Id == _seatTargetId) && output.Full.Contains(new Vector2Int(surface.Rect.x + surface.Rect.width / 2,
                    surface.Rect.y + surface.Rect.height / 2))) { rect = surface.Rect; break; }
            }
            return true;
        }

        public static bool TryQueryMonitors(ref Dictionary<IntPtr, RectInt> monitors)
        {
            try
            {
                ShojiSnapshot snapshot = Snapshot();
                if (snapshot == null || snapshot.Outputs == null || snapshot.Outputs.Length == 0) return false;
                if (monitors == null) monitors = new Dictionary<IntPtr, RectInt>();
                monitors.Clear();
                for (int i = 0; i < snapshot.Outputs.Length; i++)
                {
                    ShojiOutput output = snapshot.Outputs[i];
                    IntPtr handle;
                    if (!_outputHandles.TryGetValue(output.Name, out handle))
                    {
                        handle = new IntPtr(_outputHandles.Count + 1);
                        _outputHandles.Add(output.Name, handle);
                    }
                    monitors[handle] = output.Full;
                }
                return true;
            }
            catch (Exception e) { Report(e); return false; }
        }

        static void RefreshMonitorLayout(WindowManager windowManager)
        {
            ShojiSnapshot snapshot = Snapshot();
            if (windowManager == null || snapshot == null || snapshot.Outputs == null || snapshot.Outputs.Length == 0) return;
            bool changed = _monitorLayout == null || _monitorLayout.Length != snapshot.Outputs.Length;
            for (int i = 0; !changed && i < snapshot.Outputs.Length; i++)
                changed = _monitorLayout[i].Name != snapshot.Outputs[i].Name ||
                    !_monitorLayout[i].Full.Equals(snapshot.Outputs[i].Full);
            if (!changed) return;
            windowManager.QueryMonitors();
            _monitorLayout = snapshot.Outputs;
        }

        // X11 moves are ignored by satellite, so in a ShojiWM session every
        // move goes through the compositor (or nowhere while disconnected).
        public static bool TrySetWindowPosition(Vector2Int position)
        {
            try
            {
                if (!Active || _ipc.Unsupported) return false;
                ShojiSnapshot snapshot = Snapshot();
                if (snapshot != null && snapshot.SeatingSupported && !snapshot.WindowVisible) return true;
                if (BigScreenPlacement(ref position))
                    _ipc.RequestMove(position, EdgeOwnsMovement() ? "output:" + _edgeOutput : null);
                return true;
            }
            catch (Exception e) { Report(e); return false; }
        }

        // ---- Per-frame work and drag lifecycle ---------------------------------

        static bool _wasFocused = true;
        static bool _releaseForced;
        static float _nextButtonCheck;
        static int _buttonMismatch;

        public static void SetClientDragging(bool dragging)
        {
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot != null && snapshot.HasSurfaces) _ipc.SetDragging(dragging);
        }

        // Called first thing in WindowManager.Update. Returns true when the
        // current drag must end although no release reached Unity.
        // (Parameters are MonoBehaviour so the hook signatures in
        // Assembly-CSharp never reference Assembly-CSharp itself.)
        public static bool Tick(MonoBehaviour owner, bool dragging)
        {
            if (!Active) return false;
            var windowManager = owner as WindowManager;
            bool release = false;
            try
            {
                UpdatePolling(dragging);
                RefreshMonitorLayout(windowManager);
                release = CheckDragLifecycle(windowManager, dragging);
                SetClientDragging(dragging && !release);
                AvatarBigScreenHandler bigScreen = ActiveBigScreen();
                UpdateBigScreenExitButton(bigScreen != null);
                UpdateBigScreen(bigScreen);
            }
            catch (Exception e) { Report(e); }
            try { UpdateMedia(); }
            catch (Exception e) { Report(e); }
            try
            {
                UpdateShadow(windowManager);
            }
            catch (Exception e)
            {
                _shadow = null;
                Report(e);
            }
            return release;
        }

        static void UpdatePolling(bool dragging)
        {
            int interval = 250;
            ShojiSnapshot snapshot = _ipc.Latest;
            if (dragging)
            {
                interval = 16;
            }
            else if (snapshot != null && snapshot.HasIdleState && snapshot.DesktopIdle)
            {
                interval = 33; // Resume promptly on input anywhere in the desktop.
            }
            else if (snapshot != null && snapshot.HasWindow && snapshot.HasPointer)
            {
                // Hand holding and drag starts need a fresh pointer near the pet.
                RectInt window = snapshot.Window;
                var near = new RectInt(window.x - 64, window.y - 64, window.width + 128, window.height + 128);
                if (near.Contains(snapshot.Pointer)) interval = 33;
            }
            _ipc.SetInterval(interval);
        }

        static bool CheckDragLifecycle(WindowManager windowManager, bool dragging)
        {
            bool focused = Application.isFocused;
            bool lostFocus = _wasFocused && !focused;
            _wasFocused = focused;
            bool held = Input.GetMouseButton(0);
            if (!held || Input.GetMouseButtonDown(0))
            {
                _releaseForced = false;
                _buttonMismatch = 0;
            }

            if (held && lostFocus)
            {
                _releaseForced = true;
            }
            else if (held && !_releaseForced && !Input.GetMouseButtonDown(0) && Time.unscaledTime >= _nextButtonCheck)
            {
                // Unity still believes the button is down: ask the X server,
                // which saw every event delivered to this client.
                _nextButtonCheck = Time.unscaledTime + 0.1f;
                if (windowManager != null && !windowManager.GetMouseButton(KeyCode.Mouse0))
                {
                    if (++_buttonMismatch >= 2) _releaseForced = true;
                }
                else
                {
                    _buttonMismatch = 0;
                }
            }

            return dragging && (!held || _releaseForced || lostFocus);
        }

        // Replaces Input.GetMouseButtonUp in AvatarAnimatorController.Update.
        // There it only clears mouseHeld, so answering "the button is up now"
        // (a level, not the one-frame edge) cannot start anything, but it
        // ends a grab pose whose release event was missed.
        public static bool GetMouseButtonUp(int button)
        {
            if (Input.GetMouseButtonUp(button)) return true;
            if (button != 0 || !Active) return false;
            if (Input.GetMouseButtonDown(0)) return false;
            return !Input.GetMouseButton(0) || _releaseForced;
        }

        // ---- Settings / Blendshape menu placement ------------------------------

        const float MenuMinScale = 0.75f;
        const float MenuMarginPx = 16f;
        const float NudgeIntervalSeconds = 0.5f;

        static readonly Dictionary<RectTransform, Vector3> _menuBaseScale = new Dictionary<RectTransform, Vector3>();
        static readonly Dictionary<RectTransform, bool> _menuLeftSide = new Dictionary<RectTransform, bool>();
        static readonly Dictionary<RectTransform, List<SettingsMenuPosition.MenuEntry>> _menuGroups =
            new Dictionary<RectTransform, List<SettingsMenuPosition.MenuEntry>>();
        struct MenuPlacement
        {
            public Vector2 DesktopPivot;
            public Vector3 Scale;
        }
        static readonly Dictionary<RectTransform, MenuPlacement> _menuPlacements = new Dictionary<RectTransform, MenuPlacement>();
        static readonly Vector3[] _menuCorners = new Vector3[4];
        static MonoBehaviour _menuAnchor;
        static IntPtr _menuSeatHandle;
        static RectInt _menuAnchorRect, _menuArea;
        static Vector2Int _menuWindowSize, _menuScreenSize;
        static float _nextNudge;

        // SettingsMenuPosition.Update keeps its original X11 logic unless the
        // compositor geometry is available.
        public static bool MenuLayoutActive()
        {
            try
            {
                ShojiSnapshot snapshot = Snapshot();
                return snapshot != null && snapshot.HasWindow && snapshot.Outputs != null && snapshot.Outputs.Length > 0;
            }
            catch (Exception e) { Report(e); return false; }
        }

        // Called from SettingsMenuPosition.LateUpdate: before rendering, so a
        // menu opened this frame is drawn in place on its first frame.
        public static void LayoutMenus(MonoBehaviour owner)
        {
            try
            {
                var settings = owner as SettingsMenuPosition;
                if (settings == null || settings.menus == null || !MenuLayoutActive()) return;
                RectInt window, area;
                if (!MenuGeometry(out window, out area)) return;
                var seat = CurrentSeat();
                MonoBehaviour anchor = seat;
                IntPtr seatHandle = seat != null ? (IntPtr)SeatHandle.GetValue(seat) : IntPtr.Zero;
                RectInt anchorRect = default(RectInt);
                if (seat != null)
                {
                    var support = Surface(seatHandle);
                    if (support != null) anchorRect = support.Rect;
                }
                else if (EdgeOwnsMovement())
                {
                    anchor = _edgeOwner;
                    TryGetEdgeMonitor(out anchorRect);
                }
                bool anchored = anchor != null;
                var windowSize = new Vector2Int(window.width, window.height);
                var screenSize = new Vector2Int(Screen.width, Screen.height);
                var wm = WindowManager.Instance;
                if (!anchored || anchor != _menuAnchor || seatHandle != _menuSeatHandle || !anchorRect.Equals(_menuAnchorRect) ||
                    !area.Equals(_menuArea) || windowSize != _menuWindowSize || screenSize != _menuScreenSize ||
                    (wm != null && wm.IsDragging && Input.GetMouseButton(0))) _menuPlacements.Clear();
                _menuAnchor = anchor;
                _menuSeatHandle = seatHandle;
                _menuAnchorRect = anchorRect;
                _menuArea = area;
                _menuWindowSize = windowSize;
                _menuScreenSize = screenSize;

                foreach (var group in _menuGroups.Values) group.Clear();
                foreach (var entry in settings.menus)
                {
                    RectTransform menu = entry != null ? entry.settingsMenu : null;
                    if (menu == null) continue;
                    if (!menu.gameObject.activeInHierarchy)
                    {
                        if (_menuPlacements.ContainsKey(menu)) _menuPlacements.Clear();
                        continue;
                    }
                    // A canvas root is sized by its canvas, and stretched menus
                    // have no single anchor point to move.
                    if (menu.GetComponent<Canvas>() != null || menu.anchorMin != menu.anchorMax) continue;
                    var parent = menu.parent as RectTransform;
                    if (parent == null) continue;
                    if (!_menuBaseScale.ContainsKey(menu))
                    {
                        Vector3 scale = menu.localScale;
                        if (scale.x < 0.01f || scale.y < 0.01f) continue;
                        _menuBaseScale[menu] = scale;
                    }
                    List<SettingsMenuPosition.MenuEntry> group;
                    if (!_menuGroups.TryGetValue(parent, out group))
                    {
                        group = new List<SettingsMenuPosition.MenuEntry>();
                        _menuGroups[parent] = group;
                    }
                    group.Add(entry);
                }

                // Unity pixels each open menu needs at the minimum scale.
                bool anyOpen = false;
                Vector2 needed = Vector2.zero;
                foreach (var pair in _menuGroups)
                {
                    if (pair.Value.Count == 0) continue;
                    anyOpen = true;
                    needed = Vector2.Max(needed, MinimumPixels(pair.Key, pair.Value));
                }
                if (!anyOpen) { _menuPlacements.Clear(); return; }

                Rect region;
                bool fits = false;
                if (VisibleScreenRegion(window, area, out region))
                {
                    fits = region.width + 0.5f >= needed.x && region.height + 0.5f >= needed.y;
                    foreach (var pair in _menuGroups)
                    {
                        if (pair.Value.Count == 0) continue;
                        if (anchored && RestoreMenuGroup(pair.Key, pair.Value, window, region)) continue;
                        LayoutGroup(pair.Key, pair.Value, region, anchored);
                        if (anchored) RememberMenuGroup(pair.Key, pair.Value, window);
                    }
                }
                // Too little of the window is on the screen, possibly none of
                // it: bring the window in until the menus fit.
                if (!fits) NudgeWindow(window, area, needed);
            }
            catch (Exception e) { Report(e); }
        }

        // The window and the usable area (minus panels) of its monitor.
        static bool MenuGeometry(out RectInt window, out RectInt area)
        {
            window = default(RectInt);
            area = default(RectInt);
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null || !snapshot.HasWindow || Screen.width <= 0 || Screen.height <= 0) return false;
            window = snapshot.Window;
            RectInt anchor = window;
            var seat = CurrentSeat();
            if (seat != null)
            {
                var support = Surface((IntPtr)SeatHandle.GetValue(seat));
                if (support != null) anchor = support.Rect;
            }
            else if (EdgeOwnsMovement())
            {
                RectInt edge;
                if (TryGetEdgeMonitor(out edge)) anchor = edge;
            }
            // Animated pinning can move the transparent client's centre
            // across a monitor seam. Its support owns the menu's monitor.
            ShojiOutput output = OutputFor(snapshot, anchor);
            if (output == null) return false;
            area = output.Usable;
            return true;
        }

        // The monitor under the window centre, else the most overlapped one,
        // else the nearest one when the window is off every monitor.
        static ShojiOutput OutputFor(ShojiSnapshot snapshot, RectInt window)
        {
            if (snapshot.Outputs == null) return null;
            ShojiOutput output = null;
            var center = new Vector2Int(window.x + window.width / 2, window.y + window.height / 2);
            long best = -1;
            foreach (ShojiOutput candidate in snapshot.Outputs)
            {
                if (candidate.Full.Contains(center)) return candidate;
                RectInt overlap;
                long score = Intersect(window, candidate.Full, out overlap)
                    ? (long)overlap.width * overlap.height
                    : -DistanceSquared(candidate.Full, center);
                if (output == null || score > best)
                {
                    best = score;
                    output = candidate;
                }
            }
            return output;
        }

        static long DistanceSquared(RectInt rect, Vector2Int point)
        {
            long dx = Math.Max(Math.Max(rect.xMin - point.x, point.x - rect.xMax), 0);
            long dy = Math.Max(Math.Max(rect.yMin - point.y, point.y - rect.yMax), 0);
            return dx * dx + dy * dy;
        }

        // The part of the window on that usable area, minus margins, in
        // Unity screen pixels; false when nothing usable is left.
        static bool VisibleScreenRegion(RectInt window, RectInt area, out Rect region)
        {
            region = default(Rect);
            RectInt visible;
            if (!Intersect(window, area, out visible)) return false;

            // Window logical pixels -> Unity pixels (equal at output scale 1).
            float kx = Screen.width / (float)window.width;
            float ky = Screen.height / (float)window.height;
            float left = (visible.xMin - window.xMin) * kx + MenuMarginPx;
            float right = (visible.xMax - window.xMin) * kx - MenuMarginPx;
            float top = (visible.yMin - window.yMin) * ky + MenuMarginPx;
            float bottom = (visible.yMax - window.yMin) * ky - MenuMarginPx;
            if (right <= left || bottom <= top) return false;
            // Unity screen space grows upwards.
            region = Rect.MinMaxRect(left, Screen.height - bottom, right, Screen.height - top);
            return true;
        }

        static bool CanvasCamera(RectTransform transform, out Camera camera)
        {
            camera = null;
            Canvas canvas = transform.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            Canvas root = canvas.rootCanvas;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay) camera = root.worldCamera;
            return true;
        }

        // Screen size of one canvas' open menus at the minimum scale.
        static Vector2 MinimumPixels(RectTransform parent, List<SettingsMenuPosition.MenuEntry> members)
        {
            Camera camera;
            if (!CanvasCamera(parent, out camera)) return Vector2.zero;
            Rect union = BaseUnion(members, parent.rect, false);
            Vector2 origin = RectTransformUtility.WorldToScreenPoint(camera, parent.TransformPoint(Vector3.zero));
            float perUnitX = (RectTransformUtility.WorldToScreenPoint(camera, parent.TransformPoint(Vector3.right)) - origin).magnitude;
            float perUnitY = (RectTransformUtility.WorldToScreenPoint(camera, parent.TransformPoint(Vector3.up)) - origin).magnitude;
            return new Vector2(union.width * MenuMinScale * perUnitX, union.height * MenuMinScale * perUnitY);
        }

        // Places one canvas' menus inside the region (Unity screen pixels).
        static void LayoutGroup(RectTransform parent, List<SettingsMenuPosition.MenuEntry> members, Rect screenRegion, bool pinned)
        {
            Camera camera;
            if (!CanvasCamera(parent, out camera)) return;

            Vector2 a, b;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenRegion.min, camera, out a) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenRegion.max, camera, out b))
            {
                return;
            }
            Rect region = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            if (region.width <= 0 || region.height <= 0) return;
            Rect parentRect = parent.rect;

            // Keep the menu beside the pet; switch sides only when the other
            // side shows clearly more of it.
            Rect right = BaseUnion(members, parentRect, false);
            Rect left = BaseUnion(members, parentRect, true);
            float shownRight = VisibleFraction(right, region);
            float shownLeft = VisibleFraction(left, region);
            bool wasLeft;
            _menuLeftSide.TryGetValue(parent, out wasLeft);
            bool useLeft = wasLeft ? !(shownRight > shownLeft + 0.02f) : shownLeft > shownRight + 0.02f;
            _menuLeftSide[parent] = useLeft;

            Rect union = useLeft ? left : right;
            if (union.width <= 0 || union.height <= 0) return;
            float scale = Mathf.Min(1f, region.width / union.width, region.height / union.height);
            // A latched pet cannot be nudged to make room. Fit the menus in
            // its visible viewport instead of retaining a clipping minimum.
            if (!pinned) scale = Mathf.Max(MenuMinScale, scale);
            Vector2 center = union.center;
            Vector2 size = union.size * scale;
            float dx = Shift(center.x - size.x / 2, center.x + size.x / 2, region.xMin, region.xMax, false);
            float dy = Shift(center.y - size.y / 2, center.y + size.y / 2, region.yMin, region.yMax, true);

            foreach (var entry in members)
            {
                RectTransform menu = entry.settingsMenu;
                Vector2 anchor = AnchorPoint(menu, parentRect);
                Vector2 basePivot = anchor + BaseOffset(entry, useLeft);
                Vector2 pivot = center + (basePivot - center) * scale + new Vector2(dx, dy);
                Vector2 anchored = pivot - anchor;
                Vector3 localScale = _menuBaseScale[menu] * scale;
                if ((menu.anchoredPosition - anchored).sqrMagnitude > 0.01f) menu.anchoredPosition = anchored;
                if ((menu.localScale - localScale).sqrMagnitude > 1e-6f) menu.localScale = localScale;
                entry.lastApplied = anchored;
            }
        }

        static void RememberMenuGroup(RectTransform parent, List<SettingsMenuPosition.MenuEntry> members, RectInt window)
        {
            Camera camera;
            if (!CanvasCamera(parent, out camera)) return;
            foreach (var entry in members)
            {
                var menu = entry.settingsMenu;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, menu.position);
                _menuPlacements[menu] = new MenuPlacement {
                    DesktopPivot = new Vector2(window.x + point.x * window.width / Screen.width,
                        window.y + (Screen.height - point.y) * window.height / Screen.height),
                    Scale = menu.localScale
                };
            }
        }

        static bool RestoreMenuGroup(RectTransform parent, List<SettingsMenuPosition.MenuEntry> members, RectInt window, Rect region)
        {
            Camera camera;
            if (!CanvasCamera(parent, out camera)) return false;
            foreach (var entry in members) if (!_menuPlacements.ContainsKey(entry.settingsMenu)) return false;
            foreach (var entry in members)
            {
                var menu = entry.settingsMenu;
                var placement = _menuPlacements[menu];
                Vector2 point = new Vector2((placement.DesktopPivot.x - window.x) * Screen.width / window.width,
                    Screen.height - (placement.DesktopPivot.y - window.y) * Screen.height / window.height);
                Vector2 pivot;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, point, camera, out pivot)) return false;
                Vector2 position = pivot - AnchorPoint(menu, parent.rect);
                if ((menu.anchoredPosition - position).sqrMagnitude > 0.01f) menu.anchoredPosition = position;
                if ((menu.localScale - placement.Scale).sqrMagnitude > 1e-6f) menu.localScale = placement.Scale;
                entry.lastApplied = position;
                // Keep the desktop pivot/scale through small pin corrections.
                // Reflow only if it leaves the actual viewport, consuming the
                // existing margin before reflowing to avoid edge oscillation.
                menu.GetWorldCorners(_menuCorners);
                foreach (var corner in _menuCorners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    if (!Finite(screen.x) || !Finite(screen.y) ||
                        screen.x < region.xMin - MenuMarginPx || screen.x > region.xMax + MenuMarginPx ||
                        screen.y < region.yMin - MenuMarginPx || screen.y > region.yMax + MenuMarginPx) return false;
                }
                if (SeatAuditEnabled && (RectTransformUtility.WorldToScreenPoint(camera, menu.position) - point).sqrMagnitude > 0.25f)
                    throw new InvalidOperationException("[SeatAudit] pinned menu moved from its desktop pivot");
            }
            return true;
        }

        // The menus do not fit even at the minimum scale only when most of the
        // window lies outside the usable area: move the window in just far
        // enough that every element of the menus is on screen.
        static void NudgeWindow(RectInt window, RectInt area, Vector2 neededPixels)
        {
            if (CurrentSeat() != null || EdgeOwnsMovement()) return;
            if (Time.unscaledTime < _nextNudge || !_ipc.Connected) return;
            float kx = Screen.width / (float)window.width;
            float ky = Screen.height / (float)window.height;
            int neededX = Mathf.CeilToInt((neededPixels.x + 2 * MenuMarginPx) / kx);
            int neededY = Mathf.CeilToInt((neededPixels.y + 2 * MenuMarginPx) / ky);
            int dx = NudgeAxis(window.xMin, window.xMax, area.xMin, area.xMax, neededX);
            int dy = NudgeAxis(window.yMin, window.yMax, area.yMin, area.yMax, neededY);
            if (dx == 0 && dy == 0) return;
            _nextNudge = Time.unscaledTime + NudgeIntervalSeconds;
            TrySetWindowPosition(window.position + new Vector2Int(dx, dy));
        }

        // Shift along one axis that makes the window overlap the area by
        // `needed`, but never further than putting that side fully inside.
        static int NudgeAxis(int windowMin, int windowMax, int areaMin, int areaMax, int needed)
        {
            int overlap = Math.Min(windowMax, areaMax) - Math.Max(windowMin, areaMin);
            if (overlap >= needed) return 0;
            bool outBefore = windowMin < areaMin, outAfter = windowMax > areaMax;
            if (outBefore && !outAfter) return Math.Min(areaMin + needed - windowMax, areaMin - windowMin);
            if (outAfter && !outBefore) return -Math.Min(windowMin - (areaMax - needed), windowMax - areaMax);
            return 0;
        }

        static Vector2 BaseOffset(SettingsMenuPosition.MenuEntry entry, bool left)
        {
            return new Vector2(left ? -entry.originalX : entry.originalX, entry.originalY);
        }

        // Point the anchored position is measured from, in the parent's space.
        static Vector2 AnchorPoint(RectTransform menu, Rect parentRect)
        {
            return new Vector2(
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, menu.anchorMin.x),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, menu.anchorMin.y));
        }

        static Rect BaseUnion(List<SettingsMenuPosition.MenuEntry> members, Rect parentRect, bool left)
        {
            bool any = false;
            float xMin = 0, yMin = 0, xMax = 0, yMax = 0;
            foreach (var entry in members)
            {
                RectTransform menu = entry.settingsMenu;
                Vector3 scale = _menuBaseScale[menu];
                Vector2 pivotPosition = AnchorPoint(menu, parentRect) + BaseOffset(entry, left);
                Vector2 size = Vector2.Scale(menu.rect.size, new Vector2(scale.x, scale.y));
                Vector2 min = pivotPosition - Vector2.Scale(menu.pivot, size);
                Vector2 max = min + size;
                if (!any)
                {
                    xMin = min.x; yMin = min.y; xMax = max.x; yMax = max.y;
                    any = true;
                }
                else
                {
                    xMin = Mathf.Min(xMin, min.x); yMin = Mathf.Min(yMin, min.y);
                    xMax = Mathf.Max(xMax, max.x); yMax = Mathf.Max(yMax, max.y);
                }
            }
            return any ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : default(Rect);
        }

        static float VisibleFraction(Rect rect, Rect region)
        {
            float area = rect.width * rect.height;
            if (area <= 0) return 0;
            float w = Mathf.Min(rect.xMax, region.xMax) - Mathf.Max(rect.xMin, region.xMin);
            float h = Mathf.Min(rect.yMax, region.yMax) - Mathf.Max(rect.yMin, region.yMin);
            return w <= 0 || h <= 0 ? 0 : w * h / area;
        }

        // Too big for the region: centre horizontally, keep the top (header)
        // vertically; the next nudge makes room for the rest.
        static float Shift(float min, float max, float regionMin, float regionMax, bool keepMaxWhenTooBig)
        {
            if (max - min > regionMax - regionMin)
            {
                return keepMaxWhenTooBig ? regionMax - max : (regionMin + regionMax - min - max) / 2;
            }
            if (min < regionMin) return regionMin - min;
            if (max > regionMax) return regionMax - max;
            return 0;
        }

        static bool Intersect(RectInt a, RectInt b, out RectInt result)
        {
            int xMin = Math.Max(a.xMin, b.xMin), yMin = Math.Max(a.yMin, b.yMin);
            int xMax = Math.Min(a.xMax, b.xMax), yMax = Math.Min(a.yMax, b.yMax);
            result = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
            return xMax > xMin && yMax > yMin;
        }

        // ---- Big Screen ----------------------------------------------------------

        // Big Screen keeps the window size and zooms the camera onto the head,
        // so the window has to sit on the lower edge of its monitor for the
        // cut-off body to end at the screen edge. AvatarBigScreenHandler places
        // it once, at the end of its opening camera glide, through
        // SetWindowPosition with a formula that ignores the monitor origin, and
        // satellite dropped that move anyway: the window stayed where it was
        // and the picture ended above the screen edge. While Big Screen is on,
        // the bridge keeps the window at the bottom centre of its monitor.

        const float BigScreenAnchorInterval = 0.25f;

        static FieldInfo _bigScreenTransition;
        static bool _bigScreenTransitionResolved;
        static float _nextBigScreenAnchor;
        // Where the window was before Big Screen, and on which monitor.
        static bool _hasNormalRect;
        static RectInt _normalRect;
        static ShojiOutput _normalOutput;

        static AvatarBigScreenHandler ActiveBigScreen()
        {
            List<AvatarBigScreenHandler> handlers = AvatarBigScreenHandler.ActiveHandlers;
            if (handlers == null) return null;
            for (int i = 0; i < handlers.Count; i++)
            {
                AvatarBigScreenHandler handler = handlers[i];
                if (handler != null && handler.IsBigScreenActive) return handler;
            }
            return null;
        }

        // True during the camera glide that opens Big Screen; the handler's
        // own placement comes at its end.
        static bool InDesktopTransition(AvatarBigScreenHandler handler)
        {
            if (!_bigScreenTransitionResolved)
            {
                _bigScreenTransitionResolved = true;
                _bigScreenTransition = typeof(AvatarBigScreenHandler).GetField(
                    "isInDesktopTransition", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return _bigScreenTransition != null && (bool)_bigScreenTransition.GetValue(handler);
        }

        // Bottom centre of the monitor: lower window edge on the lower screen
        // edge. A window taller than the space below the top panel keeps its
        // top at the panel, where the compositor would push it on its next
        // layout anyway; its bottom then lies past the screen edge, still
        // without a gap.
        static RectInt BigScreenTarget(ShojiOutput output, RectInt window)
        {
            RectInt full = output.Full;
            int x = full.x + (full.width - window.width) / 2;
            int y = Math.Max(full.yMax - window.height, output.Usable.yMin);
            return new RectInt(x, y, window.width, window.height);
        }

        static ShojiOutput OutputContaining(ShojiSnapshot snapshot, RectInt rect)
        {
            if (snapshot.Outputs == null) return null;
            foreach (ShojiOutput output in snapshot.Outputs)
            {
                RectInt full = output.Full;
                if (rect.xMin >= full.xMin && rect.yMin >= full.yMin &&
                    rect.xMax <= full.xMax && rect.yMax <= full.yMax)
                {
                    return output;
                }
            }
            return null;
        }

        static void RememberNormalRect(ShojiSnapshot snapshot)
        {
            if (_hasNormalRect) return;
            ShojiOutput output = OutputFor(snapshot, snapshot.Window);
            if (output == null) return;
            _hasNormalRect = true;
            _normalRect = snapshot.Window;
            _normalOutput = output;
        }

        // The position from before Big Screen, as its offset from the corner
        // the monitor had then (_normalOutput is that snapshot's geometry),
        // applied to where the monitor the pet is on now lies in the layout
        // now: the same spot when nothing changed, the matching spot when that
        // monitor moved in the layout or the pet changed monitors. A centre
        // that was on its monitor stays on the new one, so the compositor
        // files the window there.
        static Vector2Int NormalPosition(ShojiSnapshot snapshot)
        {
            Vector2Int position = _normalRect.position;
            ShojiOutput now = OutputFor(snapshot, snapshot.Window);
            if (now == null) return position;
            RectInt from = _normalOutput.Full, to = now.Full;
            int halfWidth = _normalRect.width / 2, halfHeight = _normalRect.height / 2;
            int x = position.x - from.x + to.x;
            int y = position.y - from.y + to.y;
            if (from.Contains(new Vector2Int(position.x + halfWidth, position.y + halfHeight)))
            {
                x = Mathf.Clamp(x + halfWidth, to.xMin, to.xMax - 1) - halfWidth;
                y = Mathf.Clamp(y + halfHeight, to.yMin, to.yMax - 1) - halfHeight;
            }
            return new Vector2Int(x, y);
        }

        // Every move MateEngine asks for while Big Screen is on (the handler's
        // placement, the dance safety zone panning along, "Move To Main
        // Monitor" from the tray) lands on the bottom anchor: of the monitor
        // the requested window fits on, else of the window's current one. The
        // handler's placement always takes the current one, since its formula
        // can point between two monitors. The first move after Big Screen is
        // the handler restoring the position it saved at the end of the glide;
        // the bridge's own record from before Big Screen replaces it (the same
        // in the normal case, and still right if something moved the window
        // during the glide). Returns false when no move is needed.
        static bool BigScreenPlacement(ref Vector2Int position)
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThread) return true;
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null || !snapshot.HasWindow) return true;
            AvatarBigScreenHandler handler = ActiveBigScreen();
            if (handler == null)
            {
                if (_hasNormalRect)
                {
                    _hasNormalRect = false;
                    position = NormalPosition(snapshot);
                }
                return true;
            }
            RememberNormalRect(snapshot);
            RectInt window = snapshot.Window;
            ShojiOutput output = InDesktopTransition(handler)
                ? null
                : OutputContaining(snapshot, new RectInt(position.x, position.y, window.width, window.height));
            if (output == null) output = OutputFor(snapshot, window);
            if (output == null) return true;
            position = BigScreenTarget(output, window).position;
            _nextBigScreenAnchor = Time.unscaledTime + BigScreenAnchorInterval;
            // The compositor ignores moves while the user drags the window.
            return !snapshot.Moving && position != window.position;
        }

        // Per frame: records the normal position as Big Screen starts,
        // re-anchors after anything outside MateEngine moved the window (a
        // Super+drag once released, onto the monitor it was dropped on; a
        // monitor that changed or went away), and puts the window back if Big
        // Screen ended without the handler's restore. Waits during the opening
        // glide, which ends with the handler's placement.
        static void UpdateBigScreen(AvatarBigScreenHandler handler)
        {
            ShojiSnapshot snapshot = Snapshot();
            if (handler == null)
            {
                if (_hasNormalRect)
                {
                    _hasNormalRect = false;
                    if (snapshot != null && snapshot.HasWindow) _ipc.RequestMove(NormalPosition(snapshot));
                }
                return;
            }
            if (snapshot == null || !snapshot.HasWindow) return;
            RememberNormalRect(snapshot);
            if (snapshot.Moving || InDesktopTransition(handler) || Time.unscaledTime < _nextBigScreenAnchor) return;
            ShojiOutput output = OutputFor(snapshot, snapshot.Window);
            if (output == null) return;
            Vector2Int target = BigScreenTarget(output, snapshot.Window).position;
            if (target == snapshot.Window.position) return;
            _nextBigScreenAnchor = Time.unscaledTime + BigScreenAnchorInterval;
            _ipc.RequestMove(target);
        }

        // The radial menu's Big Screen button hides while a dance plays
        // (hideIfAnimatorBool "isCustomDancing"), which left no way out of Big
        // Screen until the dance ended. While Big Screen is on, that condition
        // is dropped from the button templates the menu builds from and checks
        // every frame, so the button stays and can only end Big Screen; the
        // dance goes on. Outside Big Screen it hides during dances as before,
        // so a dance still cannot start Big Screen.
        const string BigScreenButtonId = "bigscreen";
        const string DancingBool = "isCustomDancing";
        const float ExitButtonScanInterval = 2f;

        sealed class ExitButton
        {
            public Xamin.Button Button;
            public string[] Original;
            public string[] DuringBigScreen;
        }

        static readonly List<ExitButton> _exitButtons = new List<ExitButton>();
        static float _nextExitButtonScan;

        static void UpdateBigScreenExitButton(bool bigScreen)
        {
            _exitButtons.RemoveAll(entry => entry.Button == null);
            if (_exitButtons.Count == 0 && Time.unscaledTime >= _nextExitButtonScan)
            {
                _nextExitButtonScan = Time.unscaledTime + ExitButtonScanInterval;
                ScanExitButtons();
            }
            foreach (ExitButton entry in _exitButtons)
            {
                string[] wanted = bigScreen ? entry.DuringBigScreen : entry.Original;
                if (entry.Button.hideIfAnimatorBool != wanted) entry.Button.hideIfAnimatorBool = wanted;
            }
        }

        static void ScanExitButtons()
        {
            foreach (Xamin.CircleSelector selector in UnityEngine.Object.FindObjectsByType<Xamin.CircleSelector>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (selector.Buttons == null) continue;
                foreach (GameObject template in selector.Buttons)
                {
                    Xamin.Button button = template != null ? template.GetComponent<Xamin.Button>() : null;
                    if (button == null || button.id != BigScreenButtonId || button.hideIfAnimatorBool == null) continue;
                    if (Array.IndexOf(button.hideIfAnimatorBool, DancingBool) < 0) continue;
                    if (_exitButtons.Exists(entry => entry.Button == button)) continue;
                    _exitButtons.Add(new ExitButton
                    {
                        Button = button,
                        Original = button.hideIfAnimatorBool,
                        DuringBigScreen = Array.FindAll(button.hideIfAnimatorBool, name => name != DancingBool),
                    });
                }
            }
        }

        // ---- Click-through shadow ----------------------------------------------

        // The "Shadow" wall behind the avatar is drawn by Custom/TransparentShadow
        // as pure black with alpha up to _ShadowColor.a, and the alpha mask
        // made it catch clicks. A disabled helper camera renders the scene
        // without that wall into a small texture; its alpha is everything the
        // window shows except the shadow. A pixel leaves the input mask only
        // if it is outside that coverage (plus a margin), looks like the
        // shadow, lies in the avatar's shadow area and is not under UI that
        // takes clicks. When in doubt it keeps its input.
        sealed class ShadowSpec
        {
            public int Width, Height;
            public int X0, Y0, X1, Y1;
            public int MaxAlpha;
            public int CellsX;
            public bool[] Protected;
        }

        sealed class Coverage
        {
            public int Width, Height;
            public int FrameWidth, FrameHeight;
            public long Timestamp;
            public bool[] Solid;
            public bool[] Dilated;
            // 0 unknown, 1 rows bottom-up, 2 rows top-down, -1 undecided.
            public int Orientation;
        }

        const int ShadowCell = 8;
        const int ShadowMarginPx = 8;
        const int ShadowColorTolerance = 2;
        const int CoverageDownscale = 4;
        const int CoverageDilateCells = 3;
        const byte CoverageAlpha = 8;
        const byte CoverageSolidAlpha = 32;
        const double CoverageMaxAgeSeconds = 0.5;
        const float CoverageIntervalSeconds = 0.25f;
        // float.MaxValue is stripped from the game's mscorlib.
        const float Huge = 3.4e38f;

        static volatile ShadowSpec _shadow;
        static volatile Coverage _coverage;
        static float _nextShadowUpdate;
        static float _nextShadowScan;
        static Renderer _shadowRenderer;
        static Material _shadowMaterial;
        static Light _sun;
        static Canvas[] _canvases;
        static AvatarAnimatorController _avatar;
        static Renderer[] _avatarRenderers;
        static Camera _coverageCamera;
        static RenderTexture _coverageTexture;
        static Texture2D _coverageReadback;
        static float _nextCoverage;
        static readonly Vector3[] _corners = new Vector3[4];

        static void UpdateShadow(WindowManager windowManager)
        {
            float now = Time.unscaledTime;
            if (now < _nextShadowUpdate) return;
            _nextShadowUpdate = now + 0.1f;
            if (now >= _nextShadowScan)
            {
                _nextShadowScan = now + 2f;
                ScanShadowScene();
            }
            ShadowSpec spec = BuildShadowSpec(windowManager);
            _shadow = spec;
            if (spec != null && now >= _nextCoverage)
            {
                _nextCoverage = now + CoverageIntervalSeconds;
                UpdateCoverage();
            }
        }

        static void ScanShadowScene()
        {
            GameObject wall = GameObject.Find("/Shadow");
            _shadowRenderer = wall != null ? wall.GetComponent<MeshRenderer>() : null;
            _shadowMaterial = _shadowRenderer != null ? _shadowRenderer.sharedMaterial : null;
            if (_shadowMaterial == null || _shadowMaterial.shader == null ||
                _shadowMaterial.shader.name != "Custom/TransparentShadow" || !_shadowMaterial.HasProperty("_ShadowColor"))
            {
                _shadowRenderer = null;
                _shadowMaterial = null;
            }

            _sun = null;
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional && light.shadows != LightShadows.None && light.isActiveAndEnabled)
                {
                    _sun = light;
                    break;
                }
            }

            _canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            _avatar = null;
            _avatarRenderers = null;
            foreach (AvatarAnimatorController avatar in UnityEngine.Object.FindObjectsByType<AvatarAnimatorController>(FindObjectsSortMode.None))
            {
                if (!avatar.isActiveAndEnabled) continue;
                _avatar = avatar;
                _avatarRenderers = avatar.GetComponentsInChildren<Renderer>();
                break;
            }
        }

        static ShadowSpec BuildShadowSpec(WindowManager windowManager)
        {
            if (_shadowRenderer == null || _shadowMaterial == null || _sun == null || _avatar == null || _avatarRenderers == null) return null;
            if (!_shadowRenderer.enabled || !_shadowRenderer.gameObject.activeInHierarchy || !_sun.isActiveAndEnabled) return null;
            // Fast motion would outrun the coverage margin: keep the input then.
            if (!_avatar.isActiveAndEnabled || _avatar.isDancing || _avatar.isDragging) return null;
            Color color = _shadowMaterial.GetColor("_ShadowColor");
            // The pixel test below relies on a black shadow.
            if (color.r > 0.01f || color.g > 0.01f || color.b > 0.01f || color.a <= 0f) return null;
            Camera camera = Camera.main;
            int width = Screen.width, height = Screen.height;
            if (camera == null || width <= 0 || height <= 0) return null;

            bool any = false;
            Bounds bounds = default(Bounds);
            foreach (Renderer renderer in _avatarRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!any) { bounds = renderer.bounds; any = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!any) return null;

            // Avatar bounds plus their projection along the light onto the wall.
            Transform wall = _shadowRenderer.transform;
            Vector3 normal = wall.up, origin = wall.position, light = _sun.transform.forward;
            float facing = Vector3.Dot(normal, light);
            float x0 = Huge, y0 = Huge, x1 = -Huge, y1 = -Huge;
            Vector3 min = bounds.min, max = bounds.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Include(camera.WorldToScreenPoint(corner), ref x0, ref y0, ref x1, ref y1);
                if (Mathf.Abs(facing) > 1e-4f)
                {
                    Vector3 onWall = corner + light * (Vector3.Dot(normal, origin - corner) / facing);
                    Include(camera.WorldToScreenPoint(onWall), ref x0, ref y0, ref x1, ref y1);
                }
            }

            // X11 rows run top-down, Unity screen y bottom-up.
            var spec = new ShadowSpec
            {
                Width = width,
                Height = height,
                X0 = Mathf.Clamp(Mathf.FloorToInt(x0) - ShadowMarginPx, 0, width),
                X1 = Mathf.Clamp(Mathf.CeilToInt(x1) + ShadowMarginPx, 0, width),
                Y0 = Mathf.Clamp(height - Mathf.CeilToInt(y1) - ShadowMarginPx, 0, height),
                Y1 = Mathf.Clamp(height - Mathf.FloorToInt(y0) + ShadowMarginPx, 0, height),
                MaxAlpha = Mathf.Min(254, Mathf.CeilToInt(color.a * 255f) + ShadowColorTolerance),
            };
            if (spec.X1 <= spec.X0 || spec.Y1 <= spec.Y0) return null;
            spec.CellsX = (spec.X1 - spec.X0 + ShadowCell - 1) / ShadowCell;
            int cellsY = (spec.Y1 - spec.Y0 + ShadowCell - 1) / ShadowCell;
            spec.Protected = new bool[spec.CellsX * cellsY];
            ProtectUi(spec, cellsY, windowManager);
            return spec;
        }

        static void Include(Vector3 screen, ref float x0, ref float y0, ref float x1, ref float y1)
        {
            if (screen.x < x0) x0 = screen.x;
            if (screen.x > x1) x1 = screen.x;
            if (screen.y < y0) y0 = screen.y;
            if (screen.y > y1) y1 = screen.y;
        }

        // Marks shadow cells covered by UI that receives clicks (menus, chat,
        // the modal dialog backdrop). The full-window drag handle lives on the
        // WindowManager canvas and is skipped.
        static void ProtectUi(ShadowSpec spec, int cellsY, WindowManager windowManager)
        {
            Canvas[] canvases = _canvases;
            if (canvases == null) return;
            GameObject dragCanvas = windowManager != null ? windowManager.gameObject : null;
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled || canvas.gameObject == dragCanvas) continue;
                Canvas root = canvas.rootCanvas;
                Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
                if (camera == null && root.renderMode == RenderMode.WorldSpace) camera = Camera.main;
                IList<Graphic> graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
                for (int i = 0; i < graphics.Count; i++)
                {
                    Graphic graphic = graphics[i];
                    if (graphic == null || !graphic.isActiveAndEnabled || graphic.canvasRenderer.cull) continue;
                    graphic.rectTransform.GetWorldCorners(_corners);
                    float x0 = Huge, y0 = Huge, x1 = -Huge, y1 = -Huge;
                    for (int c = 0; c < 4; c++)
                    {
                        Vector2 p = RectTransformUtility.WorldToScreenPoint(camera, _corners[c]);
                        Include(p, ref x0, ref y0, ref x1, ref y1);
                    }
                    int left = Mathf.FloorToInt(x0) - spec.X0;
                    int right = Mathf.CeilToInt(x1) - spec.X0;
                    int top = spec.Height - Mathf.CeilToInt(y1) - spec.Y0;
                    int bottom = spec.Height - Mathf.FloorToInt(y0) - spec.Y0;
                    if (right <= 0 || bottom <= 0) continue;
                    int cx0 = Mathf.Max(0, left / ShadowCell), cx1 = Mathf.Min(spec.CellsX - 1, (right - 1) / ShadowCell);
                    int cy0 = Mathf.Max(0, top / ShadowCell), cy1 = Mathf.Min(cellsY - 1, (bottom - 1) / ShadowCell);
                    for (int cy = cy0; cy <= cy1; cy++)
                    {
                        for (int cx = cx0; cx <= cx1; cx++) spec.Protected[cy * spec.CellsX + cx] = true;
                    }
                }
            }
        }

        // Renders everything the main camera shows except the shadow wall into
        // a quarter-size texture and reads its alpha back. The game's stripped
        // UnityEngine has no AsyncGPUReadback (nor Camera.CopyFrom or
        // Renderer.forceRenderingOff), so this is a synchronous ReadPixels,
        // run every CoverageIntervalSeconds while the filter is in use.
        static void UpdateCoverage()
        {
            Camera main = Camera.main;
            if (main == null) return;
            int frameWidth = Screen.width, frameHeight = Screen.height;
            int width = Mathf.Max(16, frameWidth / CoverageDownscale);
            int height = Mathf.Max(16, frameHeight / CoverageDownscale);

            if (_coverageTexture == null || _coverageTexture.width != width || _coverageTexture.height != height)
            {
                if (_coverageTexture != null)
                {
                    _coverageTexture.Release();
                    UnityEngine.Object.Destroy(_coverageTexture);
                }
                if (_coverageReadback != null) UnityEngine.Object.Destroy(_coverageReadback);
                _coverageTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _coverageTexture.name = "ShojiBridgeCoverage";
                _coverageTexture.Create();
                _coverageReadback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }
            if (_coverageCamera == null)
            {
                var host = new GameObject("ShojiBridgeCoverageCamera");
                host.hideFlags = HideFlags.HideAndDontSave;
                _coverageCamera = host.AddComponent<Camera>();
                _coverageCamera.enabled = false;
            }

            // Same pose and projection as the main camera. Its culling mask
            // cannot be copied (no setter); rendering every layer only adds
            // coverage, which keeps more input, never less.
            _coverageCamera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            _coverageCamera.projectionMatrix = main.projectionMatrix;
            _coverageCamera.targetTexture = _coverageTexture;
            _coverageCamera.clearFlags = CameraClearFlags.SolidColor;
            _coverageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

            RenderTexture previous = RenderTexture.active;
            bool wallWasOn = _shadowRenderer.enabled;
            _shadowRenderer.enabled = false;
            try
            {
                _coverageCamera.Render();
                RenderTexture.active = _coverageTexture;
                _coverageReadback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            }
            finally
            {
                RenderTexture.active = previous;
                _shadowRenderer.enabled = wallWasOn;
            }

            NativeArray<byte> data = _coverageReadback.GetRawTextureData<byte>();
            int count = width * height;
            if (data.Length < count * 4) return;
            var solid = new bool[count];
            var covered = new bool[count];
            for (int i = 0; i < count; i++)
            {
                byte alpha = data[i * 4 + 3];
                covered[i] = alpha > CoverageAlpha;
                solid[i] = alpha > CoverageSolidAlpha;
            }
            _coverage = new Coverage
            {
                Width = width,
                Height = height,
                FrameWidth = frameWidth,
                FrameHeight = frameHeight,
                Timestamp = Stopwatch.GetTimestamp(),
                Solid = solid,
                Dilated = Dilate(covered, width, height, CoverageDilateCells),
            };
        }

        static bool[] Dilate(bool[] source, int width, int height, int radius)
        {
            var rows = new bool[source.Length];
            for (int y = 0; y < height; y++)
            {
                int run = 0, offset = y * width;
                for (int x = -radius; x < width + radius; x++)
                {
                    int enter = x + radius, leave = x - radius - 1;
                    if (enter < width && source[offset + enter]) run++;
                    if (leave >= 0 && source[offset + leave]) run--;
                    if (x >= 0 && x < width) rows[offset + x] = run > 0;
                }
            }
            var result = new bool[source.Length];
            for (int x = 0; x < width; x++)
            {
                int run = 0;
                for (int y = -radius; y < height + radius; y++)
                {
                    int enter = y + radius, leave = y - radius - 1;
                    if (enter < height && rows[enter * width + x]) run++;
                    if (leave >= 0 && rows[leave * width + x]) run--;
                    if (y >= 0 && y < height) result[y * width + x] = run > 0;
                }
            }
            return result;
        }

        // Which way the readback rows run is checked against the window
        // pixels themselves: the opaque part of the coverage must land on
        // opaque window pixels, clearly better than its mirror image.
        static int DetectOrientation(Coverage coverage, byte[] pixels, int width, int height)
        {
            int total = 0, bottomUp = 0, topDown = 0;
            for (int cy = 0; cy < coverage.Height; cy++)
            {
                int rowDown = (2 * cy + 1) * height / (2 * coverage.Height);
                int rowUp = height - 1 - rowDown;
                for (int cx = 0; cx < coverage.Width; cx++)
                {
                    if (!coverage.Solid[cy * coverage.Width + cx]) continue;
                    int x = (2 * cx + 1) * width / (2 * coverage.Width);
                    total++;
                    if (pixels[(rowUp * width + x) * 4 + 3] > 10) bottomUp++;
                    if (pixels[(rowDown * width + x) * 4 + 3] > 10) topDown++;
                }
            }
            if (total < 64) return -1;
            float up = bottomUp / (float)total, down = topDown / (float)total;
            if (up >= 0.85f && up - down >= 0.15f) return 1;
            if (down >= 0.85f && down - up >= 0.15f) return 2;
            return -1;
        }

        // Runs on WinShapeThread inside UpdateInputMask, on the BGRA copy of
        // the window pixmap just before GenerateRectangles; no Unity API here.
        public static void FilterInputPixels(byte[] pixels, int width, int height)
        {
            if (!_active) return;
            ShadowSpec spec = _shadow;
            Coverage coverage = _coverage;
            if (spec == null || coverage == null || pixels == null) return;
            if (width != spec.Width || height != spec.Height ||
                width != coverage.FrameWidth || height != coverage.FrameHeight) return;
            if (pixels.Length < width * height * 4) return;
            if ((Stopwatch.GetTimestamp() - coverage.Timestamp) / (double)Stopwatch.Frequency > CoverageMaxAgeSeconds) return;

            int orientation = coverage.Orientation;
            if (orientation == 0)
            {
                orientation = DetectOrientation(coverage, pixels, width, height);
                coverage.Orientation = orientation;
            }
            if (orientation < 0) return;
            bool bottomUp = orientation == 1;

            var columnCell = new int[spec.X1 - spec.X0];
            for (int x = spec.X0; x < spec.X1; x++) columnCell[x - spec.X0] = x * coverage.Width / width;

            int maxAlpha = spec.MaxAlpha;
            for (int y = spec.Y0; y < spec.Y1; y++)
            {
                int rowCells = (y - spec.Y0) / ShadowCell * spec.CellsX;
                int coverageRow = (bottomUp ? height - 1 - y : y) * coverage.Height / height;
                int coverageOffset = coverageRow * coverage.Width;
                for (int x = spec.X0; x < spec.X1; x++)
                {
                    int i = (y * width + x) * 4;
                    int alpha = pixels[i + 3];
                    if (alpha <= 10 || alpha > maxAlpha) continue;
                    if (pixels[i] > ShadowColorTolerance || pixels[i + 1] > ShadowColorTolerance ||
                        pixels[i + 2] > ShadowColorTolerance) continue;
                    if (coverage.Dilated[coverageOffset + columnCell[x - spec.X0]]) continue;
                    if (spec.Protected[rowCells + (x - spec.X0) / ShadowCell]) continue;
                    pixels[i + 3] = 0;
                }
            }
        }
    }
}
