using System.Linq;
using UnityEngine;

public class MoveToPrimaryScreen : MonoBehaviour
{
	public void MoveToPrimary()
	{
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(out var rect)) return;
		var monitors = wm.GetAllMonitors();
		if (monitors.Count == 0) return;
		var bounds = monitors.Values.First();
		wm.SetWindowPosition(bounds.x + (bounds.width - rect.width) / 2,
			bounds.y + (bounds.height - rect.height) / 2);
	}
}
