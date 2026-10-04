using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class AvatarHideHandler : MonoBehaviour
{
	private enum Side
	{
		None = 0,
		Left = 1,
		Right = 2
	}

	private struct MonitorData
	{
		public IntPtr hmon;

		public RECT rect;
	}

	private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct POINT
	{
		public int x;

		public int y;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public int dwFlags;
	}

	public int snapThresholdPx = 12;

	public int unsnapThresholdPx = 24;

	public int edgeInsetPx;

	public int adjacencyTolerancePx = 6;

	public int adjacencyMinVerticalOverlapPx = 32;

	public bool enableSmoothing = true;

	[Range(0.01f, 0.5f)]
	public float smoothingTime = 0.1f;

	public float smoothingMaxSpeed = 6000f;

	public bool keepTopmostWhileSnapped = true;

	public float unsnapGraceTime = 0.12f;

	public float unsnapCooldownSeconds = 0.3f;

	private Animator animator;

	private AvatarAnimatorController controller;

	private IntPtr unityHWND;

	private Transform leftHand;

	private Transform rightHand;

	private Transform anchorRoot;

	private Vector3 localAnchorOffset;

	private Camera cam;

	private Side snappedSide;

	public bool IsAnchored => snappedSide != Side.None;

	private int cursorOffsetY;

	private float velX;

	private float velY;

	private bool smoothingActive;

	private bool wasDragging;

	private float snappedAt;

	private float unsnapCooldownUntil;

	private int dragBaseW;

	private int dragBaseH;

	private IntPtr snappedHmon = IntPtr.Zero;

	private int lockedAnchorDeskX;

	private bool hasLock;

	private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

	private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private const int SM_CXVIRTUALSCREEN = 78;

	private const int SM_CYVIRTUALSCREEN = 79;

	private const int SM_XVIRTUALSCREEN = 76;

	private const int SM_YVIRTUALSCREEN = 77;

	private void Start()
	{
		unityHWND = WindowManager.Instance != null ? WindowManager.Instance.UnityWindow : IntPtr.Zero;
		animator = GetComponent<Animator>();
		controller = GetComponent<AvatarAnimatorController>();
		if (animator != null && animator.isHuman && animator.avatar != null)
		{
			leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
			rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
		}
		anchorRoot = base.transform;
		cam = Camera.main;
		if (cam == null)
		{
			cam = UnityEngine.Object.FindObjectOfType<Camera>();
		}
		unsnapCooldownUntil = -1f;
		dragBaseW = 0;
		dragBaseH = 0;
		lockedAnchorDeskX = 0;
		hasLock = false;
	}

	private void OnDisable()
	{
		SetHide(left: false, right: false);
		snappedSide = Side.None;
		snappedHmon = IntPtr.Zero;
		unsnapCooldownUntil = -1f;
		SetTopMost(on: false);
		hasLock = false;
	}

	private void Update()
	{
		if (unityHWND == IntPtr.Zero || animator == null || controller == null)
		{
			return;
		}
		if (controller.isDragging && !wasDragging && GetWindowRect(unityHWND, out var lpRect) && GetCursorPos(out var lpPoint))
		{
			dragBaseW = Math.Max(1, lpRect.Right - lpRect.Left);
			dragBaseH = Math.Max(1, lpRect.Bottom - lpRect.Top);
			cursorOffsetY = lpPoint.y - lpRect.Top;
			smoothingActive = false;
			velX = 0f;
			velY = 0f;
		}
		EnsureSaneWindowSize();
		if (controller.isDragging)
		{
			if (!GetCursorPos(out var lpPoint2))
			{
				wasDragging = controller.isDragging;
				return;
			}
			if (!GetWindowRect(unityHWND, out var lpRect2))
			{
				wasDragging = controller.isDragging;
				return;
			}
			RECT monitorRectFromHandle = GetMonitorFromWindow(unityHWND);
			IntPtr hmon = IntPtr.Zero;
			foreach (var monitor in GetAllMonitors())
				if (monitor.rect.Left == monitorRectFromHandle.Left && monitor.rect.Top == monitorRectFromHandle.Top &&
					monitor.rect.Right == monitorRectFromHandle.Right && monitor.rect.Bottom == monitorRectFromHandle.Bottom)
					{ hmon = monitor.hmon; break; }
			GetAllowedEdgesForMonitor(hmon, out var allowLeft, out var allowRight);
			int handDeskX;
			Transform clingHand = GetClingHand(Side.Left, out handDeskX);
			int handDeskX2;
			Transform clingHand2 = GetClingHand(Side.Right, out handDeskX2);
			int num = lpRect2.Left + Math.Max(1, (lpRect2.Right - lpRect2.Left) / 2);
			if (clingHand == null)
			{
				handDeskX = num;
			}
			if (clingHand2 == null)
			{
				handDeskX2 = num;
			}
			int num2 = Math.Max(1, snapThresholdPx);
			int num3 = monitorRectFromHandle.Left + edgeInsetPx;
			int num4 = monitorRectFromHandle.Right - 1 - edgeInsetPx;
			bool flag = allowLeft && (handDeskX <= num3 + num2 || lpPoint2.x <= num3 + num2);
			bool flag2 = allowRight && (handDeskX2 >= num4 - num2 || lpPoint2.x >= num4 - num2);
			if (snappedSide == Side.None)
			{
				if (Time.unscaledTime >= unsnapCooldownUntil)
				{
					if (flag)
					{
						SnapTo(Side.Left, lpPoint2, hmon, monitorRectFromHandle);
					}
					else if (flag2)
					{
						SnapTo(Side.Right, lpPoint2, hmon, monitorRectFromHandle);
					}
				}
			}
			else if (Time.unscaledTime >= snappedAt + unsnapGraceTime)
			{
				RECT snappedMonitorRect = GetSnappedMonitorRect();
				int baseDesiredEdgeX = GetBaseDesiredEdgeX(snappedMonitorRect, snappedSide);
				int num5 = Math.Max(1, unsnapThresholdPx);
				if (Mathf.Abs(lpPoint2.x - baseDesiredEdgeX) > num5)
				{
					Unsnap();
				}
			}
			if (snappedSide != Side.None)
			{
				if (!GetWindowRect(unityHWND, out var lpRect3))
				{
					wasDragging = controller.isDragging;
					return;
				}
				int targetX = ComputeHoldLeft(lpRect3.Left);
				int targetY = lpPoint2.y - cursorOffsetY;
				MoveSmooth(lpRect3.Left, lpRect3.Top, targetX, targetY);
				if (keepTopmostWhileSnapped)
				{
					SetTopMost(on: true);
				}
			}
		}
		else if (snappedSide != Side.None)
		{
			if (!GetWindowRect(unityHWND, out var lpRect4))
			{
				return;
			}
			int targetX2 = ComputeHoldLeft(lpRect4.Left);
			int top = lpRect4.Top;
			MoveSmooth(lpRect4.Left, lpRect4.Top, targetX2, top);
			if (keepTopmostWhileSnapped)
			{
				SetTopMost(on: true);
			}
		}
		wasDragging = controller.isDragging;
	}

	private int GetBaseDesiredEdgeX(RECT mon, Side side)
	{
		return side switch
		{
			Side.Left => mon.Left + edgeInsetPx, 
			Side.Right => mon.Right - 1 - edgeInsetPx, 
			_ => 0, 
		};
	}

	private int ComputeHoldLeft(int currentLeft)
	{
		if (!hasLock)
		{
			return currentLeft;
		}
		if (!TryGetAnchorDesktopX(out var x))
		{
			return currentLeft;
		}
		return currentLeft + (lockedAnchorDeskX - x);
	}

	private void GetAllowedEdgesForMonitor(IntPtr hmon, out bool allowLeft, out bool allowRight)
	{
		List<MonitorData> allMonitors = GetAllMonitors();
		if (allMonitors.Count == 0)
		{
			allowLeft = false;
			allowRight = false;
			return;
		}
		if (allMonitors.Count == 1)
		{
			allowLeft = true;
			allowRight = true;
			return;
		}
		RECT monitorRectFromHandle = GetMonitorRectFromHandle(hmon);
		bool flag = false;
		bool flag2 = false;
		int num = Mathf.Max(0, adjacencyTolerancePx);
		int num2 = Mathf.Max(1, adjacencyMinVerticalOverlapPx);
		for (int i = 0; i < allMonitors.Count; i++)
		{
			RECT rect = allMonitors[i].rect;
			if (VerticalOverlap(monitorRectFromHandle, rect) >= num2)
			{
				if (Mathf.Abs(rect.Right - monitorRectFromHandle.Left) <= num)
				{
					flag = true;
				}
				if (Mathf.Abs(rect.Left - monitorRectFromHandle.Right) <= num)
				{
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
		}
		allowLeft = !flag;
		allowRight = !flag2;
	}

	private int VerticalOverlap(RECT a, RECT b)
	{
		int num = Math.Max(a.Top, b.Top);
		int num2 = Math.Min(a.Bottom, b.Bottom);
		return Math.Max(0, num2 - num);
	}

	private List<MonitorData> GetAllMonitors()
	{
		List<MonitorData> list = new List<MonitorData>();
		GCHandle value = GCHandle.Alloc(list);
		EnumDisplayMonitors(dwData: GCHandle.ToIntPtr(value), lpfnEnum: delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
		{
			List<MonitorData> list2 = (List<MonitorData>)GCHandle.FromIntPtr(dwData).Target;
			MONITORINFO lpmi = new MONITORINFO
			{
				cbSize = Marshal.SizeOf(typeof(MONITORINFO))
			};
			if (GetMonitorInfo(hMonitor, ref lpmi))
			{
				MonitorData item = default(MonitorData);
				item.hmon = hMonitor;
				item.rect = lpmi.rcMonitor;
				list2.Add(item);
			}
			return true;
		}, hdc: IntPtr.Zero, lprcClip: IntPtr.Zero);
		value.Free();
		return list;
	}

	private Transform GetClingHand(Side side, out int handDeskX)
	{
		handDeskX = 0;
		int x;
		bool flag = TryGetWorldDesktopX(leftHand, out x);
		int x2;
		bool flag2 = TryGetWorldDesktopX(rightHand, out x2);
		if (!flag && !flag2)
		{
			return null;
		}
		if (flag && !flag2)
		{
			handDeskX = x;
			return leftHand;
		}
		if (flag2 && !flag)
		{
			handDeskX = x2;
			return rightHand;
		}
		if (side == Side.Left)
		{
			// Keep the hand facing the visible desktop at the outer edge.
			if (x >= x2)
			{
				handDeskX = x;
				return leftHand;
			}
			handDeskX = x2;
			return rightHand;
		}
		if (x2 <= x)
		{
			handDeskX = x2;
			return rightHand;
		}
		handDeskX = x;
		return leftHand;
	}

	private bool TryGetAnchorDesktopX(out int x)
	{
		x = 0;
		if (snappedSide != Side.None && GetClingHand(snappedSide, out x) != null) return true;
		if (anchorRoot == null)
		{
			return false;
		}
		return TryGetWorldDesktopX(anchorRoot.TransformPoint(localAnchorOffset), out x);
	}

	private bool TryGetWorldDesktopX(Transform t, out int x)
	{
		x = 0;
		if (t == null)
		{
			return false;
		}
		return TryGetWorldDesktopX(t.position, out x);
	}

	private bool TryGetWorldDesktopX(Vector3 world, out int x)
	{
		x = 0;
		if (cam == null)
		{
			return false;
		}
		if (!GetUnityClientRect(out var r))
		{
			return false;
		}
		Vector3 vector = cam.WorldToScreenPoint(world);
		if (vector.z < 0.01f)
		{
			return false;
		}
		float num = Mathf.Max(1f, r.Right - r.Left);
		float num2 = Mathf.Max(1, cam.pixelWidth);
		float f = vector.x * (num / num2);
		x = r.Left + Mathf.RoundToInt(f);
		return true;
	}

	private void SnapTo(Side side, POINT cp, IntPtr hmon, RECT mon)
	{
		if (GetWindowRect(unityHWND, out var lpRect))
		{
			int num = Math.Max(1, lpRect.Right - lpRect.Left);
			int num2 = Math.Max(1, lpRect.Bottom - lpRect.Top);
			if (dragBaseW <= 0)
			{
				dragBaseW = num;
			}
			if (dragBaseH <= 0)
			{
				dragBaseH = num2;
			}
			cursorOffsetY = cp.y - lpRect.Top;
			snappedSide = side;
			snappedHmon = hmon;
			SetHide(side == Side.Left, side == Side.Right);
			int baseDesiredEdgeX = GetBaseDesiredEdgeX(mon, side);
			int handDeskX;
			Transform clingHand = GetClingHand(side, out handDeskX);
			int num3;
			if (clingHand != null && anchorRoot != null)
			{
				localAnchorOffset = anchorRoot.InverseTransformPoint(clingHand.position);
				num3 = handDeskX - lpRect.Left;
			}
			else
			{
				localAnchorOffset = Vector3.zero;
				num3 = Math.Max(1, num / 2);
			}
			int x = baseDesiredEdgeX - num3;
			int y = cp.y - cursorOffsetY;
			MoveOnly(x, y);
			// IPC moves are asynchronous; the old WM snapshot is not an acknowledgement.
			lockedAnchorDeskX = baseDesiredEdgeX;
			hasLock = true;
			smoothingActive = enableSmoothing;
			velX = 0f;
			velY = 0f;
			snappedAt = Time.unscaledTime;
			if (keepTopmostWhileSnapped)
			{
				SetTopMost(on: true);
			}
		}
	}

	private void Unsnap()
	{
		snappedSide = Side.None;
		snappedHmon = IntPtr.Zero;
		SetHide(left: false, right: false);
		smoothingActive = false;
		velX = 0f;
		velY = 0f;
		SetTopMost(on: false);
		hasLock = false;
		if (controller != null && controller.isDragging)
		{
			unsnapCooldownUntil = Time.unscaledTime + Mathf.Max(0f, unsnapCooldownSeconds);
		}
	}

	private void SetHide(bool left, bool right)
	{
		if (animator == null) return;
		animator.SetBool("HideLeft", left);
		animator.SetBool("HideRight", right);
	}

	private void MoveSmooth(int curX, int curY, int targetX, int targetY)
	{
		if (!enableSmoothing || !smoothingActive)
		{
			if (curX != targetX || curY != targetY)
			{
				MoveOnly(targetX, targetY);
			}
			return;
		}
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		float f = Mathf.SmoothDamp(curX, targetX, ref velX, smoothingTime, smoothingMaxSpeed, unscaledDeltaTime);
		float f2 = Mathf.SmoothDamp(curY, targetY, ref velY, smoothingTime, smoothingMaxSpeed, unscaledDeltaTime);
		int num = Mathf.RoundToInt(f);
		int num2 = Mathf.RoundToInt(f2);
		if (Mathf.Abs(targetX - num) <= 1 && Mathf.Abs(targetY - num2) <= 1)
		{
			num = targetX;
			num2 = targetY;
			smoothingActive = false;
			velX = 0f;
			velY = 0f;
		}
		if (num != curX || num2 != curY)
		{
			MoveOnly(num, num2);
		}
	}

	private void MoveOnly(int x, int y)
	{
		SetWindowPos(unityHWND, IntPtr.Zero, x, y, 0, 0, 21u);
	}

	private void EnsureSaneWindowSize()
	{
		if (GetWindowRect(unityHWND, out var lpRect))
		{
			RECT virtualScreenRect = GetVirtualScreenRect();
			int num = Math.Max(1, lpRect.Right - lpRect.Left);
			int num2 = Math.Max(1, lpRect.Bottom - lpRect.Top);
			int num3 = Math.Max(1, virtualScreenRect.Right - virtualScreenRect.Left);
			int num4 = Math.Max(1, virtualScreenRect.Bottom - virtualScreenRect.Top);
			if (num > num3 || num2 > num4)
			{
				int cx = ((dragBaseW > 0) ? Mathf.Clamp(dragBaseW, 1, num3) : Mathf.Clamp(num, 1, num3));
				int cy = ((dragBaseH > 0) ? Mathf.Clamp(dragBaseH, 1, num4) : Mathf.Clamp(num2, 1, num4));
				SetWindowPos(unityHWND, IntPtr.Zero, 0, 0, cx, cy, 22u);
			}
		}
	}

	private RECT GetSnappedMonitorRect()
	{
		if (snappedHmon != IntPtr.Zero)
		{
			return GetMonitorRectFromHandle(snappedHmon);
		}
		return GetMonitorFromWindow(unityHWND);
	}

	private RECT GetMonitorRectFromHandle(IntPtr hmon)
	{
		RECT virtualScreenRect = GetVirtualScreenRect();
		if (hmon == IntPtr.Zero)
		{
			return virtualScreenRect;
		}
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = Marshal.SizeOf(typeof(MONITORINFO))
		};
		if (!GetMonitorInfo(hmon, ref lpmi))
		{
			return virtualScreenRect;
		}
		return lpmi.rcMonitor;
	}

	private RECT GetMonitorFromWindow(IntPtr hwnd)
	{
		if (WindowManager.Instance == null) return GetVirtualScreenRect();
		return ToRect(GetCurrentMonitorRect(WindowManager.Instance.GetMousePosition()));
	}

	private RectInt GetCurrentMonitorRect(Vector2Int pointer)
	{
		return WindowManager.Instance.GetMonitorRectFromPoint(pointer);
	}

	private RECT GetVirtualScreenRect()
	{
		RECT result = default(RECT);
		result.Left = GetSystemMetrics(76);
		result.Top = GetSystemMetrics(77);
		result.Right = result.Left + GetSystemMetrics(78);
		result.Bottom = result.Top + GetSystemMetrics(79);
		return result;
	}

	private bool GetUnityClientRect(out RECT r)
	{
		r = default(RECT);
		if (!GetClientRect(unityHWND, out var lpRect))
		{
			return false;
		}
		POINT lpPoint = default(POINT);
		lpPoint.x = 0;
		lpPoint.y = 0;
		if (!ClientToScreen(unityHWND, ref lpPoint))
		{
			return false;
		}
		r.Left = lpPoint.x;
		r.Top = lpPoint.y;
		r.Right = lpPoint.x + lpRect.Right;
		r.Bottom = lpPoint.y + lpRect.Bottom;
		return true;
	}

	private void SetTopMost(bool on)
	{
		SetWindowPos(unityHWND, on ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, 19u);
	}

	private static RECT ToRect(RectInt rect) => new RECT { Left = rect.xMin, Top = rect.yMin, Right = rect.xMax, Bottom = rect.yMax };

	private static bool GetCursorPos(out POINT point)
	{
		point = default;
		if (WindowManager.Instance == null || !WindowManager.Instance.GetMousePosition(out var position)) return false;
		point.x = position.x;
		point.y = position.y;
		return true;
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		if (WindowManager.Instance == null || !WindowManager.Instance.GetWindowRect(window, out var value)) return false;
		rect = ToRect(value);
		return true;
	}

	private static int GetSystemMetrics(int index)
	{
		if (WindowManager.Instance == null) return 0;
		bool first = true;
		int left = 0, top = 0, right = 0, bottom = 0;
		foreach (var rect in WindowManager.Instance.GetAllMonitors().Values)
		{
			left = first ? rect.xMin : Math.Min(left, rect.xMin);
			top = first ? rect.yMin : Math.Min(top, rect.yMin);
			right = first ? rect.xMax : Math.Max(right, rect.xMax);
			bottom = first ? rect.yMax : Math.Max(bottom, rect.yMax);
			first = false;
		}
		return index == 76 ? left : index == 77 ? top : index == 78 ? right - left : index == 79 ? bottom - top : 0;
	}

	private static IntPtr MonitorFromWindow(IntPtr window, uint flags) => WindowManager.Instance != null ? WindowManager.Instance.GetMonitorFromWindow(window) : IntPtr.Zero;

	private static bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info)
	{
		if (WindowManager.Instance == null) return false;
		var rect = WindowManager.Instance.GetMonitorFromHandle(monitor);
		if (rect.width <= 0 || rect.height <= 0) return false;
		info.rcMonitor = info.rcWork = ToRect(rect);
		return true;
	}

	private static bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags)
	{
		var wm = WindowManager.Instance;
		if (wm == null || window != wm.UnityWindow) return false;
		if ((flags & SWP_NOMOVE) == 0) wm.SetWindowPosition(x, y);
		if ((flags & SWP_NOSIZE) == 0) wm.SetWindowSize(width, height);
		if ((flags & SWP_NOZORDER) == 0 && (insertAfter == HWND_TOPMOST || insertAfter == HWND_NOTOPMOST)) wm.SetTopmost(insertAfter == HWND_TOPMOST);
		return true;
	}

	private static bool GetClientRect(IntPtr window, out RECT rect)
	{
		if (!GetWindowRect(window, out rect)) return false;
		rect.Right -= rect.Left;
		rect.Bottom -= rect.Top;
		rect.Left = rect.Top = 0;
		return true;
	}

	private static bool ClientToScreen(IntPtr window, ref POINT point)
	{
		if (!GetWindowRect(window, out var rect)) return false;
		point.x += rect.Left;
		point.y += rect.Top;
		return true;
	}

	private static bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData)
	{
		if (WindowManager.Instance == null) return false;
		foreach (var monitor in WindowManager.Instance.GetAllMonitors())
		{
			var rect = ToRect(monitor.Value);
			if (!lpfnEnum(monitor.Key, IntPtr.Zero, ref rect, dwData)) break;
		}
		return true;
	}
}
