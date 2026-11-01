using UnityEngine;

namespace APIs.Hyprland
{
    public sealed class HyprlandInputMask
    {
        readonly object sync = new object();
        byte[] pixels;
        int width, height;

        public void Update(byte[] pixels, int width, int height)
        {
            lock (sync)
            {
                this.pixels = pixels;
                this.width = width;
                this.height = height;
            }
        }

        public bool Contains(Vector2Int local, Vector2Int windowSize)
        {
            lock (sync)
            {
                if (pixels == null || width <= 0 || height <= 0 || windowSize.x <= 0 || windowSize.y <= 0 ||
                    local.x < 0 || local.y < 0 || local.x >= windowSize.x || local.y >= windowSize.y) return false;
                int x = (int)((long)local.x * width / windowSize.x);
                int y = (int)((long)local.y * height / windowSize.y);
                return pixels[(y * width + x) * 4 + 3] > 10;
            }
        }
    }
}
