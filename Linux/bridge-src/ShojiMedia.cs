using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CustomDancePlayer;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        [Serializable]
        sealed class MediaState
        {
            public int pid;
            public bool playing;
            public string title = "", artist = "", artwork = "";
        }

        static readonly FieldInfo MediaEntry = typeof(AvatarDanceHandler).GetField("loadedEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        static float _nextMedia;
        static string _mediaPath;
        [DllImport("libc", EntryPoint = "rename", SetLastError = true)]
        static extern int RenameMedia(string source, string destination);

        static string EntryText(object entry, string name)
        {
            return entry == null ? "" : entry.GetType().GetField(name).GetValue(entry) as string ?? "";
        }

        static void UpdateMedia()
        {
            if (Time.unscaledTime < _nextMedia) return;
            _nextMedia = Time.unscaledTime + 0.5f;
            string runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (string.IsNullOrEmpty(runtime) || !Directory.Exists(runtime)) return;
            int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            if (_mediaPath == null) _mediaPath = Path.Combine(runtime, "mateengine-now-playing-" + pid + ".json");
            var state = new MediaState { pid = pid };
            foreach (var player in Resources.FindObjectsOfTypeAll<AvatarDanceHandler>())
            {
                if (!player.gameObject.scene.IsValid() || !player.IsPlaying || player.audioSource == null ||
                    !player.audioSource.isPlaying || player.audioSource.clip == null) continue;
                object entry = MediaEntry.GetValue(player);
                state.playing = true;
                state.title = EntryText(entry, "id");
                if (string.IsNullOrEmpty(state.title)) state.title = player.audioSource.clip.name;
                state.artist = EntryText(entry, "author");
                string thumbnail = Path.Combine(Application.persistentDataPath, "Thumbnails", Path.GetFileName(state.title) + "_thumb.png");
                if (!File.Exists(thumbnail))
                {
                    string bundle = EntryText(entry, "bundlePath");
                    if (!string.IsNullOrEmpty(bundle)) thumbnail = Path.Combine(Path.GetDirectoryName(bundle), Path.GetFileNameWithoutExtension(bundle) + "_thumb.png");
                }
                if (File.Exists(thumbnail)) state.artwork = new Uri(Path.GetFullPath(thumbnail)).AbsoluteUri;
                break;
            }
            string temporary = _mediaPath + ".new";
            File.WriteAllText(temporary, JsonUtility.ToJson(state));
            if (RenameMedia(temporary, _mediaPath) != 0)
                throw new IOException("Cannot publish now-playing metadata: " + Marshal.GetLastWin32Error());
        }
    }
}
