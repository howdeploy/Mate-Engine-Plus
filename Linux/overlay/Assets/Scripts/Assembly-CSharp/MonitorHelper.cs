using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class MonitorHelper
{
	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private enum MONITOR_DPI_TYPE
	{
		MDT_EFFECTIVE_DPI = 0,
		MDT_ANGULAR_DPI = 1,
		MDT_RAW_DPI = 2
	}

	public const uint MONITOR_DEFAULTTONEAREST = 2u;

	public static IntPtr MonitorFromWindow(IntPtr window, uint flags) =>
		WindowManager.Instance != null ? WindowManager.Instance.GetMonitorFromWindow(window) : IntPtr.Zero;

	public static Rect GetTaskbarRectForWindow(IntPtr windowHandle)
	{
		var rect = GetTaskbarRectForWindow();
		return new Rect(rect.x, rect.y, rect.width, rect.height);
	}

	public static RectInt GetTaskbarRectForWindow()
	{
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(wm.GetDock, out var rect)) return default;
		return rect;
	}

	public static float GetScaleForWindow(IntPtr windowHandle)
	{
		var wm = WindowManager.Instance;
		return wm != null && wm.GetWindowRect(windowHandle, out var rect) && rect.width > 0
			? (float)Screen.width / rect.width : 1f;
	}
}
