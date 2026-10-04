using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class SettingsMenuPosition : MonoBehaviour
{
	[Serializable]
	public class MenuEntry
	{
		public RectTransform settingsMenu;

		[HideInInspector]
		public float originalX;

		[HideInInspector]
		public float originalY;

		[HideInInspector]
		public Vector2 lastApplied;
	}

	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

	[Header("Menus to track")]
	public List<MenuEntry> menus = new List<MenuEntry>();

	[Header("Edge margin in Pixels")]
	public float edgeMargin = 50f;

	[Header("Checks per second")]
	public float checkFPS = 20f;

	[Header("Monitor refresh (sec)")]
	public float monitorRefreshInterval = 2f;

	private IntPtr unityHWND;

	private readonly List<RECT> monitorRects = new List<RECT>();

	private MonitorEnumProc enumProc;

	private float checkTimer;

	private float monitorTimer;

	private bool lastAtRightEdge;

	private bool initedEdge;

	private static bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc callback, IntPtr data)
	{
		var wm = WindowManager.Instance;
		if (wm == null) return false;
		foreach (var monitor in wm.GetAllMonitors())
		{
			var bounds = monitor.Value;
			var rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
			if (!callback(monitor.Key, IntPtr.Zero, ref rect, data)) break;
		}
		return true;
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
		return true;
	}

	private void Start()
	{
		unityHWND = WindowManager.Instance.UnityWindow;
		enumProc = EnumProc;
		RefreshMonitors();
		foreach (MenuEntry menu in menus)
		{
			if ((bool)menu.settingsMenu)
			{
				menu.originalX = menu.settingsMenu.anchoredPosition.x;
				menu.originalY = menu.settingsMenu.anchoredPosition.y;
				menu.lastApplied = menu.settingsMenu.anchoredPosition;
			}
		}
	}

	private void Update()
	{
		if (unityHWND == IntPtr.Zero)
		{
			return;
		}
		monitorTimer += Time.unscaledDeltaTime;
		if (monitorTimer >= Mathf.Max(0.1f, monitorRefreshInterval))
		{
			monitorTimer = 0f;
			RefreshMonitors();
		}
		checkTimer += Time.unscaledDeltaTime;
		float num = 1f / Mathf.Max(1f, checkFPS);
		if (checkTimer < num)
		{
			return;
		}
		checkTimer = 0f;
		if (!GetWindowRect(unityHWND, out var lpRect))
		{
			return;
		}
		RECT rECT = ((monitorRects.Count > 0) ? GetBestMonitor(lpRect) : new RECT
		{
			left = 0,
			top = 0,
			right = Screen.currentResolution.width,
			bottom = Screen.currentResolution.height
		});
		bool flag = (float)lpRect.right >= (float)rECT.right - edgeMargin;
		if (!initedEdge)
		{
			lastAtRightEdge = flag;
			initedEdge = true;
		}
		if (flag == lastAtRightEdge)
		{
			return;
		}
		lastAtRightEdge = flag;
		for (int i = 0; i < menus.Count; i++)
		{
			MenuEntry menuEntry = menus[i];
			if ((bool)menuEntry.settingsMenu)
			{
				Vector2 vector = new Vector2(flag ? (0f - menuEntry.originalX) : menuEntry.originalX, menuEntry.originalY);
				if (menuEntry.lastApplied != vector)
				{
					menuEntry.settingsMenu.anchoredPosition = vector;
					menuEntry.lastApplied = vector;
				}
			}
		}
	}

	private bool EnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprc, IntPtr data)
	{
		monitorRects.Add(lprc);
		return true;
	}

	private void RefreshMonitors()
	{
		monitorRects.Clear();
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, enumProc, IntPtr.Zero);
	}

	private RECT GetBestMonitor(RECT win)
	{
		int index = 0;
		int num = 0;
		for (int i = 0; i < monitorRects.Count; i++)
		{
			int num2 = OverlapArea(win, monitorRects[i]);
			if (num2 > num)
			{
				num = num2;
				index = i;
			}
		}
		return monitorRects[index];
	}

	private int OverlapArea(RECT a, RECT b)
	{
		int num = Math.Max(a.left, b.left);
		int num2 = Math.Min(a.right, b.right);
		int num3 = Math.Max(a.top, b.top);
		int num4 = Math.Min(a.bottom, b.bottom);
		int num5 = num2 - num;
		int num6 = num4 - num3;
		if (num5 <= 0 || num6 <= 0)
		{
			return 0;
		}
		return num5 * num6;
	}
}
