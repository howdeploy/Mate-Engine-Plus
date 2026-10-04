using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

[DBusInterface("org.freedesktop.Notifications")]
public interface INotifications : IDBusObject
{
    Task<uint> NotifyAsync(string appName, uint replacesId, string appIcon, string summary, string body, string[] actions, IDictionary<string, object> hints, int expireTimeout);
}

public class DBusNotificationHelper : Singleton<DBusNotificationHelper>
{
    private Connection _connection;

    private INotifications _proxy;

    private bool _initialized;

    public void Init(Connection connection)
    {
        _connection = connection;
        _proxy = _connection.CreateProxy<INotifications>(
            "org.freedesktop.Notifications",
            "/org/freedesktop/Notifications"
        );
        _initialized = true;
    }

    public async Task<uint> Send(string title, string message, string icon = "dialog-information", string[] actions = null, int timeout = 5000)
    {
        if (!_initialized) return uint.MaxValue;
        string appName = "Mate Engine";
        uint replacesId = 0;
        actions ??= Array.Empty<string>();
        var hints = new Dictionary<string, object>
        {
            { "desktop-entry", "mateengine" } 
        };

        return await _proxy.NotifyAsync(appName, replacesId, icon, title, message, actions, hints, timeout);
    }
}