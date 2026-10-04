using System;
using System.IO;

namespace APIs.Hyprland
{
    public abstract class HyprlandSocketBase
    {
        string _HyprLandSignature = Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE");
        string _XDG_Runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");

        protected string GetSocketPath(string socketName)
        {
            if (string.IsNullOrWhiteSpace(_XDG_Runtime))
                throw new NullReferenceException("Missing enviroment variable '$XDG_RUNTIME_DIR'");

            if (string.IsNullOrWhiteSpace(_HyprLandSignature))
                throw new NullReferenceException("Missing enviroment variable '$HYPRLAND_INSTANCE_SIGNATURE'");

            return Path.Combine(_XDG_Runtime, "hypr", _HyprLandSignature, socketName);
        }

        protected bool SockerExistsImpl(string socketName)
        {
            if (string.IsNullOrWhiteSpace(_XDG_Runtime) || string.IsNullOrWhiteSpace(_HyprLandSignature))
                return false;

            var socketPath = Path.Combine(_XDG_Runtime, "hypr", _HyprLandSignature, socketName);

            return File.Exists(socketPath);
        }
    }
}