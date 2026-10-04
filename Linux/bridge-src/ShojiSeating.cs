using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        const BindingFlags SeatFields = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly FieldInfo SeatHandle = typeof(AvatarWindowHandler).GetField("snappedHWND", SeatFields);
        static readonly FieldInfo SeatAnimator = typeof(AvatarWindowHandler).GetField("animator", SeatFields);
        static readonly FieldInfo SeatController = typeof(AvatarWindowHandler).GetField("controller", SeatFields);
        static readonly FieldInfo SeatHips = typeof(AvatarWindowHandler).GetField("boneHips", SeatFields);
        static readonly FieldInfo SeatClient = typeof(AvatarWindowHandler).GetField("_lastUnityCli", SeatFields);
        static readonly FieldInfo SeatHasClient = typeof(AvatarWindowHandler).GetField("_haveUnityCli", SeatFields);
        static readonly FieldInfo SeatMaterial = typeof(AvatarWindowHandler).GetField("_occluderSharedMat", SeatFields);
        static readonly FieldInfo SeatTargetQuad = typeof(AvatarWindowHandler).GetField("targetQuadGO", SeatFields);
        static readonly FieldInfo SeatOtherQuads = typeof(AvatarWindowHandler).GetField("otherQuadGOs", SeatFields);
        static readonly FieldInfo SeatOtherOccluders = typeof(AvatarWindowHandler).GetField("activeOccluders", SeatFields);
        static readonly FieldInfo SeatNextEnumeration = typeof(AvatarWindowHandler).GetField("_nextEnumTime", SeatFields);
        static readonly FieldInfo SeatSmoothing = typeof(AvatarWindowHandler).GetField("_snapSmoothingActive", SeatFields);
        static readonly FieldInfo SeatCalibrated = typeof(AvatarWindowHandler).GetField("seatCalibrated", SeatFields);
        static readonly FieldInfo SeatLocal = typeof(AvatarWindowHandler).GetField("seatLocalAtSnap", SeatFields);
        static readonly FieldInfo SeatBoundsMinimum = typeof(AvatarWindowHandler).GetField("boundsMinSnapLocal", SeatFields);
        static readonly FieldInfo SeatBoundsSize = typeof(AvatarWindowHandler).GetField("boundsSizeSnapLocal", SeatFields);
        static readonly FieldInfo SeatNormalizedY = typeof(AvatarWindowHandler).GetField("seatNormY", SeatFields);
        static readonly MethodInfo SeatWorldGuess = typeof(AvatarWindowHandler).GetMethod("SeatWorldGuess", SeatFields);
        static readonly MethodInfo SeatWorldBounds = typeof(AvatarWindowHandler).GetMethod("GetCombinedWorldBounds", SeatFields);
        static readonly MethodInfo SeatLocalBounds = typeof(AvatarWindowHandler).GetMethod("WorldBoundsToRootLocal", SeatFields);
        static readonly MethodInfo SeatZoneProjection = typeof(AvatarWindowHandler).GetMethod("ComputeZoneDesktop", SeatFields);
        static AvatarWindowHandler _seatOwner;
        static ShojiSnapshot _seatSnapshot;
        static string _seatTargetId;
        static string _dockSnapIntent;
        static string _dockCandidate;
        static float _dockCandidateSince;
        static bool _dockSnapReady;
        static Vector2Int _dockSnapProbe;
        static AvatarWindowHandler _contactOwner;
        static Transform _contactHips;
        static Vector3 _contactHipsLocal;
        static readonly int SeatPoseParameter = Animator.StringToHash("WindowSitIndex");
        static AvatarWindowHandler _poseOwner;
        static IntPtr _poseHandle;
        static float _poseIndex;
        static float _recalibrateAt = -1f;

        public static bool BeforeSeatingUpdate(MonoBehaviour owner)
        {
            ShojiSnapshot snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.SeatingSupported || !snapshot.HasWindow) return false;
            try
            {
                if (SeatHandle == null || SeatAnimator == null || SeatController == null || SeatHips == null ||
                    SeatClient == null || SeatHasClient == null || SeatMaterial == null || SeatNextEnumeration == null ||
                    SeatZoneProjection == null || SeatOtherOccluders == null)
                    throw new InvalidOperationException("AvatarWindowHandler fields do not match the reviewed build");

                IntPtr handle = (IntPtr)SeatHandle.GetValue(handler);
                lock (_surfaceHandles) _surfaceKeys.TryGetValue(handle, out _seatTargetId);
                var controller = SeatController.GetValue(handler) as AvatarAnimatorController;
                bool dragging = controller != null && controller.isDragging && Input.GetMouseButton(0) && !_releaseForced;
                Vector2Int probe;
                Vector2Int? draggedProbe = dragging &&
                    ProjectSeatProbe(handler, snapshot, out probe) ? (Vector2Int?)probe : null;
                _dockSnapIntent = null;
                _dockSnapReady = false;
                if (snapshot.MotionSupported && handle == IntPtr.Zero && draggedProbe.HasValue && !snapshot.Moving)
                {
                    _dockSnapReady = FindDockSnapIntent(snapshot, draggedProbe.Value, out _dockSnapProbe);
                    if (_dockSnapIntent != null) draggedProbe = _dockSnapProbe;
                }
                else _dockCandidate = null;
                _ipc.SetSeating(_seatTargetId, draggedProbe);

                // A hidden desktop is not a destroyed supporting window. Leave
                // the original anchor/pose intact until this desktop returns.
                if (!snapshot.WindowVisible) return true;
                if (_seatOwner != handler || _seatSnapshot != snapshot)
                {
                    _seatOwner = handler;
                    _seatSnapshot = snapshot;
                    SeatNextEnumeration.SetValue(handler, 0f);
                }
                handler.minDragHoldSecondsToSit = Mathf.Min(handler.minDragHoldSecondsToSit, 0.15f);
                if (snapshot.MotionSupported && snapshot.Moving)
                {
                    _ipc.CancelPendingMove();
                    handler.ForceExitWindowSitting();
                    _ipc.SetSeating(null, null);
                    return true;
                }
                TrackSeatPose(handler, handle);
                RefreshSeatClient(handler, snapshot);
                PrepareSeatingOcclusion(owner);
            }
            catch (Exception error) { Report(error); }
            return false;
        }

        static bool ProjectSeatProbe(AvatarWindowHandler handler, ShojiSnapshot snapshot, out Vector2Int probe)
        {
            probe = default(Vector2Int);
            Camera camera = handler.targetCamera;
            if (camera == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0) return false;
            var hips = SeatHips.GetValue(handler) as Transform;
            Vector3 point = hips != null ? hips.position : handler.transform.position;
            point += handler.transform.up * (handler.probeZoneYOffsetLocal * handler.transform.lossyScale.y);
            Vector3 screen = camera.WorldToScreenPoint(point);
            if (screen.z <= 0) return false;
            probe = new Vector2Int(
                Mathf.RoundToInt(snapshot.Window.x + screen.x * snapshot.Window.width / camera.pixelWidth),
                Mathf.RoundToInt(snapshot.Window.y + (camera.pixelHeight - screen.y) * snapshot.Window.height / camera.pixelHeight));
            return true;
        }

        static void RefreshSeatClient(AvatarWindowHandler handler, ShojiSnapshot snapshot)
        {
            var rect = snapshot.Window;
            SeatClient.SetValue(handler, new AvatarWindowHandler.RECT {
                Left = rect.xMin, Top = rect.yMin, Right = rect.xMax, Bottom = rect.yMax
            });
            SeatHasClient.SetValue(handler, true);
        }

        static bool FindDockSnapIntent(ShojiSnapshot snapshot, Vector2Int pelvis, out Vector2Int probe)
        {
            probe = pelvis;
            if (!snapshot.WindowVisible || !snapshot.HasPointer || !snapshot.HasSurfaces) return false;
            float nearest = 0f;
            bool found = false;
            bool heldPointSelected = false;
            ShojiSurface selected = null;
            foreach (var surface in snapshot.Surfaces)
            {
                if (!surface.Dock) continue;
                var rect = surface.Rect;
                bool pelvisNear = pelvis.x >= rect.xMin - 24 && pelvis.x <= rect.xMax + 24 &&
                    Math.Abs(pelvis.y - rect.yMin) <= 96;
                bool heldPointOverDock = snapshot.Pointer.x >= rect.xMin - 16 && snapshot.Pointer.x <= rect.xMax + 16 &&
                    snapshot.Pointer.y >= rect.yMin - 24 && snapshot.Pointer.y <= rect.yMax + 24;
                // Do not snap a pelvis on the other monitor to the cursor's
                // dock. Clamping that probe teleports the original grip.
                if (pelvis.x < rect.xMin - 24 || pelvis.x > rect.xMax + 24) continue;
                if (!pelvisNear && !heldPointOverDock) continue;
                float distance = Math.Abs((heldPointOverDock ? snapshot.Pointer.y : pelvis.y) - rect.yMin);
                if (found && ((heldPointSelected && !heldPointOverDock) ||
                    (heldPointSelected == heldPointOverDock && distance >= nearest))) continue;
                found = true;
                heldPointSelected = heldPointOverDock;
                nearest = distance;
                selected = surface;
                probe = new Vector2Int(pelvis.x, rect.yMin);
            }
            if (!found) { _dockCandidate = null; return false; }
            if (_dockCandidate != selected.Id)
            {
                _dockCandidate = selected.Id;
                _dockCandidateSince = Time.unscaledTime;
            }
            // Reveal promptly, acquire only after a short deliberate hold.
            // Passing the dock during a monitor transfer must remain a drag.
            _dockSnapIntent = selected.Id;
            return Time.unscaledTime - _dockCandidateSince >= 0.15f;
        }

        // Only TrySnap's main acquisition probe is replaced. Its cooldown,
        // anti-resnap guard and ordinary window probes retain their semantics.
        public static bool ComputeSnapProbe(MonoBehaviour owner, out float x, out float y)
        {
            x = y = 0f;
            var handler = owner as AvatarWindowHandler;
            if (handler == null) return false;
            try
            {
                var arguments = new object[] { 0f, 0f };
                bool valid = (bool)SeatZoneProjection.Invoke(handler, arguments);
                x = (float)arguments[0];
                y = (float)arguments[1];
                var snapshot = Snapshot();
                var controller = SeatController.GetValue(handler) as AvatarAnimatorController;
                if (!valid || snapshot == null || !snapshot.MotionSupported) return valid;
                if (controller == null || !controller.isDragging || !Input.GetMouseButton(0) ||
                    _releaseForced || snapshot.Moving) return false;
                if ((IntPtr)SeatHandle.GetValue(handler) != IntPtr.Zero) return valid;
                if (_dockSnapIntent != null)
                {
                    if (!_dockSnapReady) return false;
                    x = _dockSnapProbe.x;
                    y = _dockSnapProbe.y;
                }
                return valid;
            }
            catch (Exception error) { Report(error); return false; }
        }

        static void TrackSeatPose(AvatarWindowHandler handler, IntPtr handle)
        {
            var animator = SeatAnimator.GetValue(handler) as Animator;
            if (animator == null) return;
            float index = animator.GetFloat(SeatPoseParameter);
            if (_poseOwner != handler || _poseHandle != handle || _poseIndex != index)
            {
                _poseOwner = handler;
                _poseHandle = handle;
                _poseIndex = index;
                _recalibrateAt = handle == IntPtr.Zero ? -1f : Time.unscaledTime + 0.35f;
            }
            // The port chooses its random sitting pose one Update after the
            // initial snap and recalibrates after only two frames. Wait for
            // the actual pose instead of retaining that transition's anchor.
            if (handle != IntPtr.Zero && animator.IsInTransition(0))
                _recalibrateAt = Time.unscaledTime + 0.15f;
        }

        static void RecalibrateSettledPose(AvatarWindowHandler handler)
        {
            ShojiSnapshot snapshot = Snapshot();
            if (_poseOwner != handler || _poseHandle == IntPtr.Zero || _recalibrateAt < 0f ||
                Time.unscaledTime < _recalibrateAt || snapshot == null || !snapshot.WindowVisible) return;
            var animator = SeatAnimator.GetValue(handler) as Animator;
            if (animator == null || animator.IsInTransition(0)) return;
            if (TryCalibrateSeat(handler))
            {
                SeatSmoothing.SetValue(handler, false);
                _recalibrateAt = -1f;
            }
        }

        public static void PrepareSeatingOcclusion(MonoBehaviour owner)
        {
            ShojiSnapshot snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.SeatingSupported || !snapshot.HasWindow) return;
            try
            {
                RefreshSeatClient(handler, snapshot);
                RecalibrateSettledPose(handler);
                if (snapshot.MotionSupported)
                {
                    // Neighbour windows are already composited over/under the
                    // pet with their actual animated alpha. Unity's opaque
                    // rectangle copies cut it out before those effects finish.
                    // Keep the target quad: it provides the ledge's 3D depth.
                    var occluders = SeatOtherOccluders.GetValue(handler);
                    occluders.GetType().GetMethod("Clear").Invoke(occluders, null);
                }
                if (SeatMaterial.GetValue(handler) == null)
                {
                    // This shader is present in this player's "unity default
                    // resources". Zero RGBA preserves the framebuffer; the
                    // earlier render queue writes only depth before the avatar.
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    if (shader == null) throw new InvalidOperationException("depth occluder shader is unavailable");
                    var material = new Material(shader);
                    material.color = Color.clear;
                    material.SetInt("_SrcBlend", 1); // One
                    material.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
                    material.SetInt("_Cull", 0);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_ZTest", 4); // LessEqual
                    material.renderQueue = 1990;
                    SeatMaterial.SetValue(handler, material); // Original OnDestroy owns cleanup.
                    // Start precreates these meshes even when its material
                    // is null; setting the shared field alone cannot fix them.
                    AssignSeatMaterial(SeatTargetQuad.GetValue(handler) as GameObject, material);
                    var quads = SeatOtherQuads.GetValue(handler) as List<GameObject>;
                    if (quads != null) foreach (GameObject quad in quads) AssignSeatMaterial(quad, material);
                }
                var hips = SeatHips.GetValue(handler) as Transform;
                Camera camera = handler.targetCamera;
                if (hips == null || camera == null) return;
                // The support plane is at the pelvis, not a fixed camera
                // distance. Foreground legs survive; rear geometry is hidden.
                float depth = camera.WorldToScreenPoint(hips.position).z;
                handler.autoScaleTargetZ = false;
                handler.targetQuadZOffset = Mathf.Max(0.001f,
                    depth - camera.nearClipPlane + 0.01f * Mathf.Abs(handler.transform.lossyScale.y));
            }
            catch (Exception error) { Report(error); }
        }

        static void AssignSeatMaterial(GameObject quad, Material material)
        {
            if (quad == null) return;
            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        public static float SeatScreenCoordinate(float value, float minimum, float maximum)
        {
            ShojiSnapshot snapshot = Snapshot();
            return snapshot != null && snapshot.SeatingSupported ? value : Mathf.Clamp(value, minimum, maximum);
        }

        public static bool TryCalibrateSeat(MonoBehaviour owner)
        {
            ShojiSnapshot snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.SeatingSupported || !snapshot.HasWindow) return false;
            try
            {
                // Anchor the actual seated model. Searching for an arbitrary
                // local point already aligned with targetY leaves the body
                // floating: PinToTarget sees no error and never moves it.
                Vector3 world = (Vector3)SeatWorldGuess.Invoke(handler, null);
                if (snapshot.MotionSupported) world = SeatSkinContact(handler, world);
                Vector3 local = handler.transform.worldToLocalMatrix.MultiplyPoint3x4(world);
                Bounds bounds = (Bounds)SeatLocalBounds.Invoke(handler,
                    new object[] { (Bounds)SeatWorldBounds.Invoke(handler, null) });
                SeatLocal.SetValue(handler, local);
                SeatBoundsMinimum.SetValue(handler, bounds.min);
                SeatBoundsSize.SetValue(handler, bounds.size);
                SeatNormalizedY.SetValue(handler, Mathf.Clamp01((local.y - bounds.min.y) / Mathf.Max(0.0001f, bounds.size.y)));
                SeatCalibrated.SetValue(handler, true);
                _contactOwner = handler;
                _contactHips = SeatHips.GetValue(handler) as Transform;
                if (_contactHips != null) _contactHipsLocal = handler.transform.InverseTransformPoint(_contactHips.position);
                return true;
            }
            catch (Exception error) { Report(error); return false; }
        }

        public static bool TrySeatWorldCurrent(MonoBehaviour owner, out Vector3 world)
        {
            world = default(Vector3);
            var snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (snapshot == null || !snapshot.MotionSupported || handler == null ||
                handler != _contactOwner || _contactHips == null || !(bool)SeatCalibrated.GetValue(handler) ||
                (IntPtr)SeatHandle.GetValue(handler) == IntPtr.Zero) return false;
            Vector3 local = (Vector3)SeatLocal.GetValue(handler);
            Vector3 minimum = (Vector3)SeatBoundsMinimum.GetValue(handler);
            Vector3 size = (Vector3)SeatBoundsSize.GetValue(handler);
            local.y = minimum.y + Mathf.Clamp((float)SeatNormalizedY.GetValue(handler) +
                handler.windowSitYOffset, -0.5f, 1.5f) * size.y;
            // The original point is frozen in root space. Seated idle motion
            // moves the pelvis relative to that root, making it float/sink.
            local.y += handler.transform.InverseTransformPoint(_contactHips.position).y - _contactHipsLocal.y;
            world = handler.transform.TransformPoint(local);
            return Finite(world.x) && Finite(world.y) && Finite(world.z);
        }

        public static bool TrySetSeatingTransient(IntPtr parent)
        {
            ShojiSnapshot snapshot = Snapshot();
            // Virtual handles are not XIDs. ShojiWM already owns the pet's
            // stacking policy; never submit these handles to Xlib.
            return snapshot != null && snapshot.SeatingSupported && (parent == IntPtr.Zero || IsVirtualHandle(parent));
        }
    }
}
