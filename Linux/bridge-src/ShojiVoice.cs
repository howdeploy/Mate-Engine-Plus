using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CustomDancePlayer;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        [Serializable]
        sealed class VoiceRequest
        {
            public int version;
            public string id, command, name, monitor, windowId, appId;
            public double deadlineUtc;
            public bool enabled;
            public float x = 0.5f, y = 0.5f, fraction = 0.5f;
        }

        [Serializable]
        sealed class VoiceResponse
        {
            public int version = 1, pid, instance;
            public string id, error, monitor, currentDance;
            public bool ok, playing, portrait, accepted;
            public bool anchored, seatFlag, seatState, edgeAnchored, edgeState;
            public string edgeSide;
            public float seatPose;
            public string animationClip;
            public string[] dances;
        }

        sealed class VoiceJob
        {
            public VoiceRequest Request;
            public Socket Client;
            public string Response;
            public volatile bool Cancelled;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim();
        }

        static readonly object VoiceGate = new object();
        static readonly HashSet<string> VoiceActions = new HashSet<string> {
            "status", "dances.list", "dance.play", "dance.stop", "dance.next", "dance.previous", "hide.edge",
            "portrait.set", "seat.dock", "seat.window", "move", "quit"
        };
        static volatile Socket _voiceListener;
        static VoiceJob _voiceJob;
        static string _voicePath;
        static bool _voiceAttempted;
        static float _voiceQuitAt;
        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        static extern int VoiceChmod(string path, uint mode);
        [DllImport("libc", EntryPoint = "geteuid")]
        static extern uint VoiceUid();

        static double VoiceUtc() { return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }

        static void StartVoiceControl()
        {
            _voiceAttempted = true;
            string runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (runtime != "/run/user/" + VoiceUid() || !Directory.Exists(runtime))
                throw new InvalidOperationException("Voice control requires the user's private XDG_RUNTIME_DIR");
            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            string path = Path.Combine(runtime, "mateengine-voice-" + pid + ".sock");
            var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            bool bound = false;
            try
            {
                listener.Bind(new UnixDomainSocketEndPoint(path));
                bound = true;
                if (VoiceChmod(path, 384) != 0) throw new IOException("Cannot protect the voice socket");
                listener.Listen(4);
                _voicePath = path;
                _voiceListener = listener;
                Application.quitting += CloseVoiceControl;
                new Thread(VoiceServer) { IsBackground = true, Name = "MateVoiceControl" }.Start();
                Debug.Log("[ShojiBridge] voice control ready: " + path);
            }
            catch
            {
                listener.Close();
                if (bound) File.Delete(path);
                throw;
            }
        }

        static void CloseVoiceControl()
        {
            Socket listener = _voiceListener;
            _voiceListener = null;
            if (listener != null) listener.Close();
            if (_voicePath != null) { File.Delete(_voicePath); _voicePath = null; }
        }

        static void VoiceServer()
        {
            while (_voiceListener != null)
            {
                try
                {
                    using (Socket client = _voiceListener.Accept())
                    {
                        client.ReceiveTimeout = 2000;
                        client.SendTimeout = 1000;
                        var bytes = new List<byte>();
                        var one = new byte[1];
                        while (client.Receive(one) == 1 && one[0] != 10)
                        {
                            if (bytes.Count >= 8192) throw new IOException("Voice request is too large");
                            bytes.Add(one[0]);
                        }
                        VoiceRequest request = JsonUtility.FromJson<VoiceRequest>(Encoding.UTF8.GetString(bytes.ToArray()));
                        if (request == null || request.version != 1 || string.IsNullOrEmpty(request.id) ||
                            request.id.Length > 64 || !VoiceActions.Contains(request.command ?? "") ||
                            double.IsNaN(request.deadlineUtc) || double.IsInfinity(request.deadlineUtc) ||
                            request.deadlineUtc <= VoiceUtc() || request.deadlineUtc > VoiceUtc() + 10 ||
                            (request.name ?? "").Length > 256 || (request.monitor ?? "").Length > 64 ||
                            (request.windowId ?? "").Length > 128 || (request.appId ?? "").Length > 128)
                            throw new ArgumentException("Invalid or expired voice request");
                        var job = new VoiceJob { Request = request, Client = client };
                        lock (VoiceGate) _voiceJob = job;
                        if (!job.Done.Wait(6000))
                        {
                            job.Cancelled = true;
                            throw new TimeoutException("Unity did not accept the voice request in time");
                        }
                        byte[] response = Encoding.UTF8.GetBytes(job.Response + "\n");
                        for (int sent = 0; sent < response.Length;)
                        {
                            int count = client.Send(response, sent, response.Length - sent, SocketFlags.None);
                            if (count == 0) throw new IOException("Voice client disconnected");
                            sent += count;
                        }
                    }
                }
                catch (Exception error)
                {
                    if (_voiceListener != null) Debug.LogWarning("[ShojiBridge] voice request: " + error.Message);
                }
            }
        }

        static T VoiceComponent<T>() where T : Behaviour
        {
            T selected = null;
            foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
            {
                if (!candidate.gameObject.scene.IsValid() || !candidate.isActiveAndEnabled) continue;
                if (selected != null) throw new InvalidOperationException("More than one active " + typeof(T).Name);
                selected = candidate;
            }
            if (selected == null) throw new InvalidOperationException("No active " + typeof(T).Name);
            return selected;
        }

        static ShojiOutput VoiceOutput(ShojiSnapshot snapshot, string name)
        {
            if (string.IsNullOrEmpty(name)) return OutputFor(snapshot, snapshot.Window);
            foreach (ShojiOutput output in snapshot.Outputs) if (output.Name == name) return output;
            throw new ArgumentException("Requested monitor is not connected");
        }

        static void VoiceSeatStatus(VoiceResponse response, AvatarWindowHandler handler)
        {
            var animator = SeatAnimator.GetValue(handler) as Animator;
            response.anchored = (IntPtr)SeatHandle.GetValue(handler) != IntPtr.Zero;
            if (animator == null) return;
            response.seatFlag = animator.GetBool("isWindowSit");
            response.seatState = animator.GetCurrentAnimatorStateInfo(0).IsName("WindowSit");
            response.seatPose = animator.GetFloat(SeatPoseParameter);
            response.animationClip = OriginalSeatClip(handler);
        }

        static void VoiceEdgeStatus(VoiceResponse response)
        {
            response.edgeAnchored = EdgeOwnsMovement();
            if (!response.edgeAnchored) return;
            bool left = Convert.ToInt32(EdgeSide.GetValue(_edgeOwner)) == 1;
            response.edgeSide = left ? "left" : "right";
            var animator = _edgeOwner.GetComponent<Animator>();
            response.edgeState = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(
                animator.GetLayerName(0) + (left ? ".Hide Left" : ".Hide Right"));
        }

        static VoiceResponse ExecuteVoiceControl(VoiceRequest request, WindowManager manager)
        {
            var response = new VoiceResponse { id = request.id, ok = true,
                pid = System.Diagnostics.Process.GetCurrentProcess().Id };
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--instance") int.TryParse(args[i + 1], out response.instance);
            if (request.command == "quit")
            {
                _voiceQuitAt = Time.realtimeSinceStartup + 0.25f;
                response.accepted = true;
                return response;
            }
            if (request.command == "dances.list" || request.command.StartsWith("dance."))
            {
                var dances = VoiceComponent<AvatarDanceHandler>();
                if (request.command == "dances.list") response.dances = dances.GetDanceNamesForBridge();
                else { dances.ExecuteDanceFromBridge(request.command, request.name); response.accepted = true; }
                response.currentDance = dances.GetCurrentStableId();
                response.playing = dances.IsPlaying;
                return response;
            }
            ShojiSnapshot snapshot = Snapshot();
            if (snapshot == null || !_ipc.IsFresh(snapshot) || !snapshot.HasWindow || snapshot.Outputs == null)
                throw new InvalidOperationException("Compositor state is unavailable or stale");
            var portrait = VoiceComponent<AvatarBigScreenHandler>();
            response.portrait = portrait.IsBigScreenActive;
            if (request.command == "status")
            {
                var output = OutputFor(snapshot, snapshot.Window);
                response.monitor = output == null ? null : output.Name;
                VoiceSeatStatus(response, VoiceComponent<AvatarWindowHandler>());
                VoiceEdgeStatus(response);
                return response;
            }
            if (!snapshot.WindowVisible || snapshot.Moving || manager == null || manager.IsDragging)
                throw new InvalidOperationException("The pet is hidden or being dragged");
            // shortcut: compositor geometry targets one pet; add per-window routing before moving multiple pets.
            if (snapshot.Surfaces != null && Array.FindAll(snapshot.Surfaces, surface => surface.Self).Length != 1)
                throw new InvalidOperationException("Visual commands require one pet in the compositor; dance and quit support explicit instances");
            if (request.command == "portrait.set")
            {
                if (!portrait.SetPortraitFromBridge(request.enabled))
                    throw new InvalidOperationException("Portrait transition is already in progress");
                response.accepted = true;
                return response;
            }
            if (portrait.IsBigScreenActive) throw new InvalidOperationException("Leave portrait mode before moving or sitting");
            ShojiOutput target = VoiceOutput(snapshot, request.monitor);
            if (target == null) throw new InvalidOperationException("Cannot identify the pet's monitor");
            response.monitor = target.Name;
            if (request.command == "hide.edge")
            {
                if (!snapshot.MotionSupported) throw new InvalidOperationException("Edge hiding is unavailable");
                var handler = VoiceComponent<AvatarHideHandler>();
                VoiceComponent<AvatarWindowHandler>().ForceExitWindowSitting();
                ReleaseEdge();
                _edgeOwner = handler;
                _edgeOutput = target.Name;
                if (!handler.HideFromBridge(target.Full))
                {
                    ReleaseEdge();
                    throw new InvalidOperationException("Cannot hide at an external edge of this monitor");
                }
                VoiceEdgeStatus(response);
                VoiceSeatStatus(response, VoiceComponent<AvatarWindowHandler>());
            }
            else if (request.command == "move")
            {
                if (!VoiceUnit(request.x) || !VoiceUnit(request.y)) throw new ArgumentException("Position must be in 0..1");
                VoiceComponent<AvatarWindowHandler>().ForceExitWindowSitting();
                ReleaseEdge();
                RectInt area = target.Usable;
                _ipc.RequestMove(new Vector2Int(area.x + Mathf.RoundToInt(Math.Max(0, area.width - snapshot.Window.width) * request.x),
                    area.y + Mathf.RoundToInt(Math.Max(0, area.height - snapshot.Window.height) * request.y)), "output:" + target.Name);
            }
            else
            {
                if (!VoiceUnit(request.fraction) || snapshot.Surfaces == null || !snapshot.SeatingSupported)
                    throw new ArgumentException("Seating state or fraction is invalid");
                ShojiSurface selected = null;
                foreach (ShojiSurface surface in snapshot.Surfaces)
                {
                    bool matches = request.command == "seat.dock" ? surface.Dock && surface.Id == "dock:" + target.Name :
                        !surface.Dock && surface.Visible && !surface.Self &&
                        (!string.IsNullOrEmpty(request.windowId) ? surface.Id == request.windowId :
                            !string.IsNullOrEmpty(request.appId) && string.Equals(surface.ClassName, request.appId, StringComparison.OrdinalIgnoreCase));
                    if (!matches || surface.Self || surface.Maximized || surface.Fullscreen ||
                        !target.Full.Contains(new Vector2Int(surface.Rect.x + surface.Rect.width / 2, surface.Rect.y))) continue;
                    if (selected != null) throw new InvalidOperationException("More than one matching seating surface");
                    selected = surface;
                }
                if (selected == null) throw new ArgumentException("No matching seating surface on the requested monitor");
                string previous = _seatTargetId;
                _seatTargetId = selected.Id;
                _ipc.SetSeating(selected.Id, null);
                bool seated = false;
                try
                {
                    ReleaseEdge();
                    List<IntPtr> handles;
                    if (!TryGetStackingList(manager.UnityWindow, out handles)) throw new InvalidOperationException("Cannot enumerate seating handles");
                    IntPtr handle;
                    lock (_surfaceHandles) handle = _surfaceHandles[selected.Id];
                    var handler = VoiceComponent<AvatarWindowHandler>();
                    seated = handler.SeatFromBridge(handle, request.fraction);
                    if (!seated) throw new InvalidOperationException("Native seating was blocked by avatar state or settings");
                    VoiceSeatStatus(response, handler);
                }
                finally
                {
                    if (!seated) { _seatTargetId = previous; _ipc.SetSeating(previous, null); }
                }
            }
            response.accepted = true;
            return response;
        }

        static bool VoiceUnit(float value) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1; }

        static void UpdateVoiceControl(WindowManager manager)
        {
            if (!_voiceAttempted) StartVoiceControl();
            if (_voiceQuitAt > 0 && Time.realtimeSinceStartup >= _voiceQuitAt) { Application.Quit(); return; }
            VoiceJob job;
            lock (VoiceGate) { job = _voiceJob; _voiceJob = null; }
            if (job == null) return;
            try
            {
                if (job.Cancelled || job.Request.deadlineUtc <= VoiceUtc() ||
                    (job.Client.Poll(0, SelectMode.SelectRead) && job.Client.Available == 0))
                    throw new TimeoutException("Voice request expired or its client disconnected");
                job.Response = JsonUtility.ToJson(ExecuteVoiceControl(job.Request, manager));
            }
            catch (Exception error)
            {
                job.Response = JsonUtility.ToJson(new VoiceResponse { id = job.Request.id, error = error.Message });
            }
            finally { job.Done.Set(); }
        }
    }
}
