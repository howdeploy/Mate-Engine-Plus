using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class RemoveTaskbarApp : MonoBehaviour
{
	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int SW_RESTORE = 9;

	private static IntPtr unityHWND = IntPtr.Zero;

	private bool _isHidden = true;

	public bool IsHidden => _isHidden;

	private void Start()
	{
		unityHWND = GetUnityWindow();
		if (unityHWND != IntPtr.Zero)
		{
			WindowManager.Instance.HideFromTaskbar(true);
			_isHidden = true;
		}
	}

	public void ToggleAppMode()
	{
		unityHWND = GetUnityWindow();
		if (!(unityHWND == IntPtr.Zero))
		{
			_isHidden = !_isHidden;
			WindowManager.Instance.HideFromTaskbar(_isHidden);
		}
	}

	private IntPtr GetUnityWindow()
	{
		return WindowManager.Instance != null ? WindowManager.Instance.UnityWindow : IntPtr.Zero;
	}
}
