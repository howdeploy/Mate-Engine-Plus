using System;
using System.Reflection;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        static readonly FieldInfo SeatFraction = typeof(AvatarWindowHandler).GetField("snapFraction", SeatFields);
        static readonly FieldInfo SeatCursorY = typeof(AvatarWindowHandler).GetField("_snapCursorY", SeatFields);
        static readonly FieldInfo SeatVelocityX = typeof(AvatarWindowHandler).GetField("_snapVelX", SeatFields);
        static readonly FieldInfo SeatVelocityY = typeof(AvatarWindowHandler).GetField("_snapVelY", SeatFields);
        static readonly MethodInfo SeatWorldCurrent = typeof(AvatarWindowHandler).GetMethod("GetSeatWorldCurrent", SeatFields);
        static readonly FieldInfo DragMouseOrigin = typeof(WindowManager).GetField("_initialMousePos", SeatFields);
        static readonly FieldInfo DragWindowOrigin = typeof(WindowManager).GetField("_initialWindowPos", SeatFields);
        static readonly FieldInfo EdgeSide = typeof(AvatarHideHandler).GetField("snappedSide", SeatFields);
        static readonly MethodInfo EdgeUnsnap = typeof(AvatarHideHandler).GetMethod("Unsnap", SeatFields);
        static AvatarHideHandler _edgeOwner;
        static string _edgeOutput;
        static ShojiSnapshot _lastSeatMove;
        static bool _seatSmoothing;
        static Vector2 _seatSmoothPos;
        static Vector2Int _seatSmoothSent;

        static AvatarWindowHandler CurrentSeat()
        {
            var snapshot = Snapshot();
            if (snapshot == null || !snapshot.MotionSupported || _seatOwner == null || !_seatOwner.isActiveAndEnabled)
                return null;
            return (IntPtr)SeatHandle.GetValue(_seatOwner) == IntPtr.Zero ? null : _seatOwner;
        }

        public static void MoveDraggedWindow(MonoBehaviour owner, Vector2Int position)
        {
            var wm = owner as WindowManager;
            if (wm == null) return;
            // Only this call site is the plain cursor-driven drag. Seating,
            // hiding, Big Screen and menu moves retain their own entry points.
            if (CurrentSeat() != null || EdgeOwnsMovement()) return;
            wm.SetWindowPosition(position);
        }

        static bool EdgeOwnsMovement()
        {
            var snapshot = Snapshot();
            return snapshot != null && snapshot.MotionSupported && _edgeOwner != null &&
                _edgeOwner.isActiveAndEnabled && Convert.ToInt32(EdgeSide.GetValue(_edgeOwner)) != 0;
        }

        static void ReleaseEdge()
        {
            if (_edgeOwner != null && Convert.ToInt32(EdgeSide.GetValue(_edgeOwner)) != 0)
                EdgeUnsnap.Invoke(_edgeOwner, null);
            _edgeOutput = null;
        }

        public static bool BeforeEdgeUpdate(MonoBehaviour owner)
        {
            var snapshot = Snapshot();
            var handler = owner as AvatarHideHandler;
            if (handler == null || snapshot == null || !snapshot.MotionSupported) return false;
            _edgeOwner = handler;
            if (snapshot.Outputs == null || snapshot.Outputs.Length == 0)
            {
                ReleaseEdge();
                return true;
            }
            // A workspace switch hides the surface, not the edge anchor.
            // Clearing the pose here strands the normal avatar off-screen.
            if (!snapshot.WindowVisible)
            {
                _ipc.CancelPendingMove();
                return true;
            }
            if (snapshot.Moving || CurrentSeat() != null || ActiveBigScreen() != null)
            {
                ReleaseEdge();
                return true;
            }
            var controller = handler.GetComponent<AvatarAnimatorController>();
            bool dragging = controller != null && controller.isDragging;
            if (dragging)
            {
                ShojiOutput output = null;
                if (snapshot.HasPointer) foreach (var candidate in snapshot.Outputs)
                    if (candidate.Full.Contains(snapshot.Pointer)) { output = candidate; break; }
                if (output == null) { ReleaseEdge(); return true; }
                bool latched = Convert.ToInt32(EdgeSide.GetValue(handler)) != 0;
                bool left = snapshot.Pointer.x - output.Full.xMin <= 64;
                bool right = output.Full.xMax - snapshot.Pointer.x <= 64;
                bool internalEdge = false;
                foreach (var neighbour in snapshot.Outputs)
                {
                    if (neighbour.Name == output.Name || snapshot.Pointer.y < neighbour.Full.yMin ||
                        snapshot.Pointer.y >= neighbour.Full.yMax) continue;
                    internalEdge |= (left && neighbour.Full.xMax == output.Full.xMin) ||
                        (right && neighbour.Full.xMin == output.Full.xMax);
                }
                // A shared output edge is a transfer, not an off-screen hide.
                // Never carry a side anchor's local maths onto another output.
                if (internalEdge || (latched && _edgeOutput != output.Name))
                {
                    if (latched) { ReleaseEdge(); RebaseDrag(snapshot); }
                    return true;
                }
                // A hand outside a monitor is not a request to hide. The
                // cursor must actually approach a side before acquiring it.
                if (!latched && !left && !right) return true;
                _edgeOutput = output.Name;
            }
            else
            {
                if (!EdgeOwnsMovement()) return true;
                bool found = false;
                foreach (var output in snapshot.Outputs) if (output.Name == _edgeOutput) found = true;
                if (!found) { ReleaseEdge(); return true; }
            }
            return false;
        }

        public static bool TryGetEdgeMonitor(out RectInt rect)
        {
            rect = default(RectInt);
            var snapshot = Snapshot();
            if (snapshot == null || !snapshot.MotionSupported || snapshot.Outputs == null) return false;
            foreach (var output in snapshot.Outputs)
                if (output.Name == _edgeOutput) { rect = output.Full; return true; }
            // Removed monitors release their anchor; never send a zero-sized
            // monitor to the original side-positioning arithmetic.
            ReleaseEdge();
            return false;
        }

        static Vector2Int DesiredDragPosition(ShojiSnapshot snapshot)
        {
            var wm = WindowManager.Instance;
            if (wm == null || !snapshot.HasPointer) return snapshot.Window.position;
            return (Vector2Int)DragWindowOrigin.GetValue(wm) + snapshot.Pointer -
                (Vector2Int)DragMouseOrigin.GetValue(wm);
        }

        static void RebaseDrag(ShojiSnapshot snapshot)
        {
            _ipc.CancelPendingMove();
            _lastSeatMove = null;
            var wm = WindowManager.Instance;
            if (wm == null || !snapshot.HasPointer) return;
            DragWindowOrigin.SetValue(wm, snapshot.Window.position);
            DragMouseOrigin.SetValue(wm, snapshot.Pointer);
        }

        public static void EdgeReleased()
        {
            var snapshot = Snapshot();
            if (snapshot == null || !snapshot.MotionSupported) return;
            _edgeOutput = null;
            RebaseDrag(snapshot);
        }

        public static void SeatReleased(MonoBehaviour owner)
        {
            _seatSmoothing = false;
            if (SeatAuditEnabled) EndOriginalSeatAudit(owner as AvatarWindowHandler);
            var snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.MotionSupported ||
                (IntPtr)SeatHandle.GetValue(handler) == IntPtr.Zero) return;
            RebaseDrag(snapshot);
            _ipc.SetSeating(null, null);
        }

        static Vector3 SeatScreenPoint(AvatarWindowHandler handler)
        {
            return handler.targetCamera.WorldToScreenPoint((Vector3)SeatWorldCurrent.Invoke(handler, null));
        }

        public static bool TryKeepSeat(MonoBehaviour owner, out bool keep)
        {
            keep = false;
            var snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.MotionSupported || !snapshot.HasWindow) return false;
            var support = Surface((IntPtr)SeatHandle.GetValue(handler));
            if (support == null || (!support.Visible && !support.Dock)) { RebaseDrag(snapshot); return true; }
            var camera = handler.targetCamera;
            if (camera == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0) return false;
            Vector3 point = SeatScreenPoint(handler);
            if (point.z <= 0f || !Finite(point.x) || !Finite(point.y) || !Finite(point.z))
            {
                RebaseDrag(snapshot);
                return true;
            }
            float contactX = DesiredDragPosition(snapshot).x + point.x * snapshot.Window.width / camera.pixelWidth;
            keep = snapshot.HasPointer &&
                Math.Abs(snapshot.Pointer.y - (int)SeatCursorY.GetValue(handler)) <= 48 &&
                contactX >= support.Rect.xMin - 24 && contactX <= support.Rect.xMax + 24;
            if (!keep) RebaseDrag(snapshot);
            return true;
        }

        public static bool TryPinSeat(MonoBehaviour owner)
        {
            var snapshot = Snapshot();
            var handler = owner as AvatarWindowHandler;
            if (handler == null || snapshot == null || !snapshot.MotionSupported || !snapshot.HasWindow) return false;
            if (!snapshot.WindowVisible || snapshot.Moving) return true;
            var support = Surface((IntPtr)SeatHandle.GetValue(handler));
            if (support == null || (!support.Visible && !support.Dock))
            {
                _seatSmoothing = false;
                return true;
            }
            var camera = handler.targetCamera;
            if (camera == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0) return false;
            if (SeatAuditEnabled) AuditOriginalSeat(handler, snapshot, support);
            ReleaseEdge();
            Vector3 point = SeatScreenPoint(handler);
            if (point.z <= 0 || !Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return true;
            float localX = point.x * snapshot.Window.width / camera.pixelWidth;
            float localY = (camera.pixelHeight - point.y) * snapshot.Window.height / camera.pixelHeight;
            var controller = SeatController.GetValue(handler) as AvatarAnimatorController;
            float fraction = (float)SeatFraction.GetValue(handler);
            if (!Finite(fraction) || !Finite(localX) || !Finite(localY)) return true;
            if (controller != null && controller.isDragging)
            {
                fraction = Mathf.Clamp01((DesiredDragPosition(snapshot).x + localX - support.Rect.x) /
                    Mathf.Max(1, support.Rect.width));
                SeatFraction.SetValue(handler, fraction);
            }
            Vector2Int desired = new Vector2Int(
                Mathf.RoundToInt(support.Rect.x + fraction * support.Rect.width - localX),
                Mathf.RoundToInt(support.Rect.y + handler.seatOffsetPx - localY));
            if (handler.enableSnapSmoothing && (bool)SeatSmoothing.GetValue(handler))
            {
                if (!_seatSmoothing)
                {
                    _seatSmoothPos = snapshot.Window.position;
                    _seatSmoothSent = snapshot.Window.position;
                    _seatSmoothing = true;
                }
                // Integrate from our commanded position; the asynchronous WM
                // can report the same older position for several Unity frames.
                float velocityX = (float)SeatVelocityX.GetValue(handler);
                float velocityY = (float)SeatVelocityY.GetValue(handler);
                float dt = Time.unscaledDeltaTime;
                float x = Mathf.SmoothDamp(_seatSmoothPos.x, desired.x, ref velocityX,
                    handler.snapSmoothingTime, handler.snapSmoothingMaxSpeed, dt);
                float y = Mathf.SmoothDamp(_seatSmoothPos.y, desired.y, ref velocityY,
                    handler.snapSmoothingTime, handler.snapSmoothingMaxSpeed, dt);
                if (controller != null && controller.isDragging && y > desired.y)
                    y -= Mathf.Min(handler.snapSmoothingMaxSpeed * dt, Mathf.Max(0f, y - desired.y - 1f));
                var position = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
                if (Mathf.Abs(desired.x - position.x) <= 1 && Mathf.Abs(desired.y - position.y) <= 1)
                {
                    position = desired;
                    x = desired.x;
                    y = desired.y;
                    SeatSmoothing.SetValue(handler, false);
                    velocityX = velocityY = 0f;
                    _seatSmoothing = false;
                    if (SeatAuditEnabled) LogOriginalSeat("smooth-settled", $"position={position}");
                }
                SeatVelocityX.SetValue(handler, velocityX);
                SeatVelocityY.SetValue(handler, velocityY);
                _seatSmoothPos = new Vector2(x, y);
                if (position != _seatSmoothSent)
                {
                    _seatSmoothSent = position;
                    _ipc.RequestMove(position, support.Id);
                    if (SeatAuditEnabled) LogOriginalSeat("move", $"mode=smooth from={snapshot.Window.position} to={position} desired={desired} support={support.Rect} smoothing={_seatSmoothing}");
                }
                return true;
            }
            _seatSmoothing = false;
            // Settled seats retain absolute geometry, once per real WM reply.
            if (_lastSeatMove != snapshot && desired != snapshot.Window.position)
            {
                _lastSeatMove = snapshot;
                _ipc.RequestMove(desired, support.Id);
                if (SeatAuditEnabled) LogOriginalSeat("move", $"mode=absolute from={snapshot.Window.position} to={desired} desired={desired} support={support.Rect} smoothing={_seatSmoothing}");
            }
            return true;
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
