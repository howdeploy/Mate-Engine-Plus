using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MateEngine.Shoji
{
    internal sealed class ShojiOutput
    {
        public string Name;
        public RectInt Full;
        public RectInt Usable;
    }

    // One reply of the compositor, immutable once published: window,
    // pointer and outputs always describe the same moment. Window and
    // pointer keep their last known value when the compositor temporarily
    // has none, so callers never fall back to a different coordinate space
    // or to (0,0).
    internal sealed class ShojiSnapshot
    {
        public bool HasWindow;
        public RectInt Window;
        public bool HasPointer;
        public Vector2Int Pointer;
        public ShojiOutput[] Outputs;
        // The user is dragging the window in the compositor (Super+drag).
        public bool Moving;
        public bool HasSurfaces;
        public bool SeatingSupported;
        public bool MotionSupported;
        public bool WindowVisible = true;
        public bool Covered;
        public bool HasIdleState, DesktopIdle, IdleEnabled;
        public int IdleTimeout;
        public long ReceivedAt;
        public int ConnectionEpoch;
        public ShojiSurface[] Surfaces;
    }

    internal sealed class ShojiSurface
    {
        public string Id;
        public string ClassName;
        public RectInt Rect;
        public bool Self, Dock, Maximized, Fullscreen;
        public bool Visible = true;
    }

    // NDJSON client of the ShojiWM config IPC socket. Socket work happens on
    // two background threads; the Unity thread only swaps references and
    // sets a wake event. At most one state request is in flight and only the
    // latest move is kept, so nothing queues up.
    internal sealed class ShojiIpc
    {
        const int MaxLineBytes = 1 << 20;
        const int RequestTimeoutMs = 2000;
        const int UnsupportedRetryMs = 5000;
        const int StaleMoveMs = 1000;

        readonly string _path;
        readonly AutoResetEvent _wake = new AutoResetEvent(false);
        readonly object _moveGate = new object();
        readonly Stopwatch _clock = Stopwatch.StartNew();

        volatile ShojiSnapshot _latest;
        volatile int _intervalMs = 250;
        volatile bool _unsupported;
        volatile bool _connected;
        volatile int _connectionEpoch;

        bool _moveDirty;
        Vector2Int _move;
        string _moveAnchor;
        long _moveAt;
        bool _dragging;
        bool _dragDirty;
        long _lastDragSent = -100000;
        long _dragObservedAt = -100000;
        string _seatTarget;
        Vector2Int? _seatProbe;
        long _seatObservedAt = -100000;
        int _idleTimeout = 30;
        bool _idleEnabled;
        long _idleObservedAt = -100000;

        Socket _socket;
        volatile bool _readerDied;
        volatile bool _inFlight;
        volatile int _inFlightId;
        long _inFlightSince;
        long _lastRequestAt = -100000;
        bool _confirmMove;
        int _nextId;
        string _lastLog;

        public ShojiIpc(string path)
        {
            _path = path;
        }

        public static string DefaultSocketPath()
        {
            // Same derivation as defaultSocketPath() in shoji_wm/ipc.
            string dir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            string display = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            if (string.IsNullOrEmpty(dir)) dir = "/tmp";
            if (string.IsNullOrEmpty(display)) display = "wayland-0";
            return Path.Combine(dir, "shojiwm-" + display + ".sock");
        }

        public ShojiSnapshot Latest { get { return _latest; } }

        // The config does not know mateengine.* (older config loaded).
        public bool Unsupported { get { return _unsupported; } }

        public bool Connected { get { return _connected; } }

        public bool IsFresh(ShojiSnapshot snapshot)
        {
            return _connected && snapshot != null && snapshot.ConnectionEpoch == _connectionEpoch &&
                _clock.ElapsedMilliseconds - snapshot.ReceivedAt < 1500;
        }

        public void SetScreensaver(int timeout, bool enabled)
        {
            bool changed;
            lock (_moveGate)
            {
                changed = _idleTimeout != timeout || _idleEnabled != enabled;
                _idleTimeout = timeout;
                _idleEnabled = enabled;
                _idleObservedAt = _clock.ElapsedMilliseconds;
            }
            if (changed) _wake.Set();
        }

        public void Start()
        {
            new Thread(Run) { IsBackground = true, Name = "ShojiBridgeIpc" }.Start();
        }

        public void SetInterval(int milliseconds)
        {
            int previous = _intervalMs;
            _intervalMs = milliseconds;
            if (milliseconds < previous) _wake.Set();
        }

        // A move requested while disconnected is dropped, never replayed
        // later; the window simply stays where the compositor has it.
        public void RequestMove(Vector2Int position, string anchor = null)
        {
            lock (_moveGate)
            {
                if (!_connected) return;
                _move = position;
                _moveAnchor = anchor;
                _moveAt = _clock.ElapsedMilliseconds;
                _moveDirty = true;
            }
            _wake.Set();
        }

        public void SetDragging(bool dragging)
        {
            lock (_moveGate)
            {
                if (dragging) _dragObservedAt = _clock.ElapsedMilliseconds;
                if (_dragging == dragging) return;
                _dragging = dragging;
                _dragDirty = true;
                if (!dragging) _moveDirty = false;
            }
            _wake.Set();
        }

        public void SetSeating(string target, Vector2Int? probe)
        {
            lock (_moveGate)
            {
                _seatTarget = target;
                _seatProbe = probe;
                _seatObservedAt = _clock.ElapsedMilliseconds;
            }
        }

        public void CancelPendingMove()
        {
            lock (_moveGate) _moveDirty = false;
        }

        void Run()
        {
            int backoff = 500;
            while (true)
            {
                if (_socket == null)
                {
                    if (!Connect())
                    {
                        _wake.WaitOne(backoff);
                        backoff = Math.Min(backoff * 2, 5000);
                        continue;
                    }
                    backoff = 500;
                }
                if (_readerDied)
                {
                    Drop("connection closed");
                    continue;
                }
                long now = _clock.ElapsedMilliseconds;
                int interval = _unsupported ? UnsupportedRetryMs : _intervalMs;
                try
                {
                    SendDragState(now);
                    SendPendingMove(now);
                    if (_inFlight && now - _inFlightSince > RequestTimeoutMs)
                    {
                        Drop("state request timed out");
                        continue;
                    }
                    // Right after a move, ask for the result instead of
                    // waiting for the next poll, so readers see it soon.
                    if (!_inFlight && (_confirmMove || now - _lastRequestAt >= interval))
                    {
                        _confirmMove = false;
                        SendStateRequest(now);
                    }
                }
                catch (Exception e)
                {
                    Drop(e.Message);
                    continue;
                }
                long wait = _inFlight ? 100 : _lastRequestAt + interval - _clock.ElapsedMilliseconds;
                _wake.WaitOne((int)Math.Max(5, Math.Min(wait, 1000)));
            }
        }

        bool Connect()
        {
            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                // (The game's stripped System.dll has no SendTimeout property.)
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
                socket.Connect(new UnixDomainSocketEndPoint(_path));
            }
            catch (Exception e)
            {
                if (socket != null) socket.Close();
                Log("cannot connect to " + _path + ": " + e.Message);
                return false;
            }
            _socket = socket;
            _readerDied = false;
            _inFlight = false;
            _confirmMove = false;
            _unsupported = false;
            _connectionEpoch++;
            _connected = true;
            lock (_moveGate) { _dragDirty = true; }
            new Thread(() => ReadLoop(socket)) { IsBackground = true, Name = "ShojiBridgeRead" }.Start();
            Log("connected to " + _path);
            return true;
        }

        void Drop(string reason)
        {
            Socket socket = _socket;
            _socket = null;
            _inFlight = false;
            lock (_moveGate)
            {
                _connected = false;
                _moveDirty = false;
                _dragging = false;
                _dragDirty = true;
            }
            if (socket != null)
            {
                try { socket.Shutdown(SocketShutdown.Both); } catch (Exception) { }
                socket.Close();
            }
            Log("disconnected: " + reason);
        }

        void SendPendingMove(long now)
        {
            Vector2Int position;
            string anchor;
            bool dragging;
            lock (_moveGate)
            {
                if (!_moveDirty) return;
                _moveDirty = false;
                // A move that waited too long would teleport the pet to an
                // old drag position.
                if (now - _moveAt > StaleMoveMs) return;
                position = _move;
                anchor = _moveAnchor;
                dragging = _dragging;
            }
            Send("{\"method\":\"mateengine.move\",\"params\":{\"x\":"
                + position.x.ToString(CultureInfo.InvariantCulture) + ",\"y\":"
                + position.y.ToString(CultureInfo.InvariantCulture)
                + ",\"anchor\":" + JsonString(anchor)
                + ",\"dragging\":" + (dragging ? "true" : "false") + "}}\n");
            _confirmMove = true;
        }

        void SendDragState(long now)
        {
            bool dragging;
            lock (_moveGate)
            {
                // Heartbeats require a live Unity main thread, not merely a
                // live socket thread (Unity may be stuck waiting for the GPU).
                if (_dragging && now - _dragObservedAt > StaleMoveMs)
                {
                    _dragging = false;
                    _dragDirty = true;
                    _moveDirty = false;
                }
                if (!_dragDirty && (!_dragging || now - _lastDragSent < 250)) return;
                dragging = _dragging;
                _dragDirty = false;
            }
            Send("{\"method\":\"mateengine.drag\",\"params\":{\"dragging\":"
                + (dragging ? "true" : "false") + "}}\n");
            _lastDragSent = now;
        }

        void SendStateRequest(long now)
        {
            int id = ++_nextId;
            _inFlightId = id;
            _inFlightSince = now;
            _lastRequestAt = now;
            _inFlight = true;
            string target;
            Vector2Int? probe;
            int timeout;
            bool idleEnabled;
            lock (_moveGate)
            {
                bool fresh = now - _seatObservedAt < 1000;
                target = fresh ? _seatTarget : null;
                probe = fresh ? _seatProbe : null;
                timeout = _idleTimeout;
                idleEnabled = _idleEnabled && now - _idleObservedAt < 1000;
            }
            string point = probe.HasValue ? "{\"x\":" + probe.Value.x.ToString(CultureInfo.InvariantCulture) +
                ",\"y\":" + probe.Value.y.ToString(CultureInfo.InvariantCulture) + "}" : "null";
            Send("{\"id\":" + id.ToString(CultureInfo.InvariantCulture) +
                ",\"method\":\"mateengine.state\",\"params\":{\"seat\":" + JsonString(target) +
                ",\"probe\":" + point + ",\"screensaver\":{\"timeout\":" + timeout.ToString(CultureInfo.InvariantCulture) +
                ",\"enabled\":" + (idleEnabled ? "true" : "false") + "}}}\n");
        }

        static string JsonString(string value)
        {
            if (value == null) return "null";
            var text = new StringBuilder("\"");
            foreach (char character in value)
            {
                if (character == '\\' || character == '"') text.Append('\\').Append(character);
                else if (character < 32) text.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                else text.Append(character);
            }
            return text.Append('"').ToString();
        }

        void Send(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            int offset = 0;
            while (offset < bytes.Length)
            {
                int sent = _socket.Send(bytes, offset, bytes.Length - offset, SocketFlags.None);
                if (sent <= 0) throw new IOException("send failed");
                offset += sent;
            }
        }

        void ReadLoop(Socket socket)
        {
            var buffer = new byte[16384];
            var line = new MemoryStream();
            bool overflow = false;
            try
            {
                while (true)
                {
                    int read = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (read <= 0) break;
                    for (int i = 0; i < read; i++)
                    {
                        byte b = buffer[i];
                        if (b == (byte)'\n')
                        {
                            if (!overflow) HandleLine(Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length));
                            line.SetLength(0);
                            overflow = false;
                        }
                        else if (line.Length < MaxLineBytes)
                        {
                            line.WriteByte(b);
                        }
                        else
                        {
                            overflow = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Closed by Drop() or by the compositor; the writer reconnects.
            }
            if (socket == _socket)
            {
                _readerDied = true;
                _wake.Set();
            }
        }

        void HandleLine(string text)
        {
            // Broadcasts ({"event":...}) of the shared socket are not ours.
            if (!text.StartsWith("{\"id\":", StringComparison.Ordinal)) return;
            Dictionary<string, object> message;
            try
            {
                message = MiniJson.Parse(text) as Dictionary<string, object>;
            }
            catch (Exception e)
            {
                Log("bad reply: " + e.Message);
                return;
            }
            if (message == null || !(Get(message, "id") is double) || (int)(double)Get(message, "id") != _inFlightId) return;

            object error = Get(message, "error");
            if (error != null)
            {
                string reason = error as string ?? "error";
                if (reason.IndexOf("unknown method", StringComparison.Ordinal) >= 0)
                {
                    if (!_unsupported) Log("config has no mateengine.* IPC; using X11 geometry");
                    _unsupported = true;
                }
                else
                {
                    Log("state request failed: " + reason);
                }
            }
            else
            {
                var result = Get(message, "result") as Dictionary<string, object>;
                if (result != null)
                {
                    _unsupported = false;
                    var snapshot = ParseState(result, _latest);
                    snapshot.ReceivedAt = _clock.ElapsedMilliseconds;
                    snapshot.ConnectionEpoch = _connectionEpoch;
                    _latest = snapshot;
                }
            }
            _inFlight = false;
            _wake.Set();
        }

        static ShojiSnapshot ParseState(Dictionary<string, object> result, ShojiSnapshot previous)
        {
            var snapshot = new ShojiSnapshot();
            object seating = Get(result, "seatingVersion");
            snapshot.SeatingSupported = seating is double && (double)seating >= 2;
            snapshot.MotionSupported = seating is double && (double)seating >= 3;
            object windowVisible = Get(result, "windowVisible");
            snapshot.WindowVisible = !(windowVisible is bool) || (bool)windowVisible;
            var idle = Get(result, "screensaver") as Dictionary<string, object>;
            double timeout;
            object idleEnabled = Get(idle, "enabled"), desktopIdle = Get(idle, "idle");
            if (TryNumber(idle, "timeout", out timeout) && timeout >= 30 && timeout <= 10800 &&
                timeout == Math.Floor(timeout) && idleEnabled is bool && desktopIdle is bool)
            {
                snapshot.HasIdleState = true;
                snapshot.IdleTimeout = (int)timeout;
                snapshot.IdleEnabled = (bool)idleEnabled;
                snapshot.DesktopIdle = (bool)desktopIdle;
            }

            RectInt window;
            if (TryRect(Get(result, "window") as Dictionary<string, object>, out window))
            {
                snapshot.HasWindow = true;
                snapshot.Window = window;
            }
            else if (previous != null && previous.HasWindow)
            {
                snapshot.HasWindow = true;
                snapshot.Window = previous.Window;
            }
            object moving = Get(result, "moving");
            snapshot.Moving = moving is bool && (bool)moving;

            var pointer = Get(result, "pointer") as Dictionary<string, object>;
            double px, py;
            if (TryNumber(pointer, "x", out px) && TryNumber(pointer, "y", out py))
            {
                snapshot.HasPointer = true;
                // ShojiWM confines trailing edges half a pixel inside the
                // output. Rounding would move that point outside RectInt.
                snapshot.Pointer = new Vector2Int((int)Math.Floor(px), (int)Math.Floor(py));
            }
            else if (previous != null && previous.HasPointer)
            {
                snapshot.HasPointer = true;
                snapshot.Pointer = previous.Pointer;
            }

            var outputs = new List<ShojiOutput>();
            var list = Get(result, "outputs") as List<object>;
            if (list != null)
            {
                foreach (object item in list)
                {
                    var entry = item as Dictionary<string, object>;
                    RectInt full;
                    if (!TryRect(entry, out full)) continue;
                    RectInt usable;
                    if (!TryRect(Get(entry, "usable") as Dictionary<string, object>, out usable)) usable = full;
                    outputs.Add(new ShojiOutput { Name = Get(entry, "name") as string, Full = full, Usable = usable });
                }
            }
            snapshot.Outputs = outputs.Count > 0 || previous == null ? outputs.ToArray() : previous.Outputs;
            var surfaces = Get(result, "surfaces") as List<object>;
            snapshot.HasSurfaces = surfaces != null;
            snapshot.Covered = Get(result, "covered") is bool && (bool)Get(result, "covered");
            var parsed = new List<ShojiSurface>();
            if (surfaces != null) foreach (object item in surfaces)
            {
                var entry = item as Dictionary<string, object>;
                RectInt rect;
                string id = Get(entry, "id") as string;
                if (id == null || !TryRect(Get(entry, "rect") as Dictionary<string, object>, out rect)) continue;
                parsed.Add(new ShojiSurface {
                    Id = id, Rect = rect, ClassName = Get(entry, "appId") as string ?? "shoji-window",
                    Self = Flag(entry, "self"), Dock = Flag(entry, "dock"),
                    Maximized = Flag(entry, "maximized"), Fullscreen = Flag(entry, "fullscreen"),
                    Visible = !(Get(entry, "visible") is bool) || Flag(entry, "visible"),
                });
            }
            snapshot.Surfaces = parsed.ToArray();
            return snapshot;
        }

        static bool Flag(Dictionary<string, object> entry, string key)
        {
            object value = Get(entry, key);
            return value is bool && (bool)value;
        }

        static bool TryRect(Dictionary<string, object> source, out RectInt rect)
        {
            rect = default(RectInt);
            double x, y, width, height;
            if (!TryNumber(source, "x", out x) || !TryNumber(source, "y", out y) ||
                !TryNumber(source, "width", out width) || !TryNumber(source, "height", out height))
            {
                return false;
            }
            if (width < 1 || height < 1) return false;
            rect = new RectInt(Round(x), Round(y), Round(width), Round(height));
            return true;
        }

        static bool TryNumber(Dictionary<string, object> source, string key, out double value)
        {
            value = 0;
            object raw = Get(source, key);
            if (!(raw is double)) return false;
            value = (double)raw;
            return !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) < 1e7;
        }

        static object Get(Dictionary<string, object> source, string key)
        {
            object value;
            return source != null && source.TryGetValue(key, out value) ? value : null;
        }

        static int Round(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        void Log(string message)
        {
            if (message == _lastLog) return;
            _lastLog = message;
            UnityEngine.Debug.Log("[ShojiBridge] " + message);
        }
    }
}
