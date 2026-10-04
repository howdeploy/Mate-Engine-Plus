using System;

namespace APIs.Hyprland
{
    public static class HyprlandUtils
    {
        public static IntPtr AddressToIntPtr(string address) => new IntPtr(Convert.ToInt64(address, 16));

        public static string IntPtrToHex(IntPtr ptr) => ptr.ToString("X8");
    }
}