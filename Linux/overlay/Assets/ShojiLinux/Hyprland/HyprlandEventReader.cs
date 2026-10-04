using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace APIs.Hyprland
{
    public class HyprlandEventArgs : EventArgs
    {
        public HyprlandEventArgs(string eventName, string[] parameters) : base()
        {
            EventName = eventName;
            Parameters = parameters;
        }

        public string EventName { get; set; }
        public string[] Parameters { get; set; }

        public override string ToString() => $"{EventName} : {string.Join(" , ", Parameters)}";
    }

    public delegate void HyprlandEventDelegate(object sender, HyprlandEventArgs args);

    /// <summary>
    /// Reads hyprland events in realtime
    /// </summary>
    public class HyprlandEventReader : HyprlandSocketBase, IDisposable
    {
        public void Dispose()
        {
            _StreamReader?.Dispose();
            _Socket?.Dispose();
        }

        const string _SocketName = ".socket2.sock";
        StreamReader _StreamReader;
        Socket _Socket;

        public bool SocketExists() => SockerExistsImpl(_SocketName);

        public async void Start(IList<string> eventFilter)
        {
            var eventSocketPath = GetSocketPath(_SocketName);

            _Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(eventSocketPath);

            await _Socket.ConnectAsync(endpoint);

            var stream = new NetworkStream(_Socket, ownsSocket: true);
            _StreamReader = new StreamReader(stream);

            while (true)
            {
                // events are terminated by \n
                var output = await _StreamReader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    // format: evetName>>parameter1,paramater2,parameter3
                    var parts = output.Split(">>", StringSplitOptions.RemoveEmptyEntries);

                    if (eventFilter.Any(a => parts[0] == a))
                        continue;

                    var parameters = new string[0];
                    if (parts.Length > 1)
                        parameters = parts[1].Split(",", StringSplitOptions.RemoveEmptyEntries);

                    HyprlandEvent?.Invoke(this, new HyprlandEventArgs(parts[0], parameters));
                }
            }
        }

        public event HyprlandEventDelegate HyprlandEvent;
    }
}