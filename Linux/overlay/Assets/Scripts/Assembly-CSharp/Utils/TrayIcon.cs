using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Utils
{
	public static class TrayIcon
	{
		public enum ToolTipIcon
		{
			None = 0,
			Info = 1,
			Warning = 2,
			Error = 3
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct NOTIFYICONDATA
		{
			public uint cbSize;

			public IntPtr hWnd;

			public uint uID;

			public uint uFlags;

			public uint uCallbackMessage;

			public IntPtr hIcon;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szTip;

			public uint dwState;

			public uint dwStateMask;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szInfo;

			public uint uVersion;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string szInfoTitle;

			public uint dwInfoFlags;

			public Guid guidItem;

			public IntPtr hBalloonIcon;
		}

		private struct ICONINFO
		{
			public bool fIcon;

			public uint xHotspot;

			public uint yHotspot;

			public IntPtr hbmMask;

			public IntPtr hbmColor;
		}

		private struct BITMAPINFOHEADER
		{
			public uint biSize;

			public int biWidth;

			public int biHeight;

			public ushort biPlanes;

			public ushort biBitCount;

			public uint biCompression;

			public uint biSizeImage;

			public int biXPelsPerMeter;

			public int biYPelsPerMeter;

			public uint biClrUsed;

			public uint biClrImportant;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct WNDCLASSEX
		{
			public uint cbSize;

			public uint style;

			public IntPtr lpfnWndProc;

			public int cbClsExtra;

			public int cbWndExtra;

			public IntPtr hInstance;

			public IntPtr hIcon;

			public IntPtr hCursor;

			public IntPtr hbrBackground;

			public string lpszMenuName;

			public string lpszClassName;

			public IntPtr hIconSm;
		}

		private struct BITMAPINFO
		{
			public BITMAPINFOHEADER bmiHeader;
		}

		private struct POINT
		{
			public int X;

			public int Y;
		}

		private static class WinAPI
		{
			[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA lpData);

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool DestroyWindow(IntPtr hWnd);

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

			[DllImport("user32.dll")]
			public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool DestroyIcon(IntPtr hIcon);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr CreatePopupMenu();

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

			[DllImport("user32.dll")]
			public static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lpTPMParams);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool DestroyMenu(IntPtr hMenu);

			[DllImport("user32.dll")]
			public static extern bool GetCursorPos(out POINT lpPoint);

			[DllImport("user32.dll")]
			public static extern bool SetForegroundWindow(IntPtr hWnd);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern IntPtr GetModuleHandle(string lpModuleName);

			[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

			[DllImport("gdi32.dll", SetLastError = true)]
			public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

			[DllImport("gdi32.dll")]
			public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, IntPtr lpvBits);

			[DllImport("gdi32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DeleteObject(IntPtr hObject);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr CreateIconIndirect([In] ref ICONINFO piconinfo);
		}

		private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		private const uint NIM_ADD = 0u;

		private const uint NIM_MODIFY = 1u;

		private const uint NIM_DELETE = 2u;

		private const uint NIF_MESSAGE = 1u;

		private const uint NIF_ICON = 2u;

		private const uint NIF_TIP = 4u;

		private const uint NIF_INFO = 16u;

		private const uint WM_COMMAND = 273u;

		private const uint WM_USER = 1024u;

		private const uint WM_APP = 32768u;

		private const uint TRAY_ICON_MESSAGE = 32769u;

		private const uint WM_LBUTTONUP = 514u;

		private const uint WM_RBUTTONUP = 517u;

		private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

		private const uint MF_STRING = 0u;

		private const uint MF_POPUP = 16u;

		private const uint MF_BYPOSITION = 1024u;

		private const uint MF_SEPARATOR = 2048u;

		private const uint TPM_LEFTALIGN = 0u;

		private const uint TPM_LEFTBUTTON = 0u;

		private const uint TPM_BOTTOMALIGN = 32u;

		private const uint NIIF_NONE = 0u;

		private const uint NIIF_INFO = 1u;

		private const uint NIIF_WARNING = 2u;

		private const uint NIIF_ERROR = 3u;

		private const uint NIIF_NOSOUND = 16u;

		public const string LEFT_CLICK = "LeftClick";

		public const string SEPARATOR = "Separator";

		private const uint BI_RGB = 0u;

		private const uint DIB_RGB_COLORS = 0u;

		private static bool _init = false;

		private static string windowClassName;

		private static NOTIFYICONDATA notifyIconData;

		private static IntPtr hIcon;

		private static IntPtr messageWindowHandle;

		private static Dictionary<string, Action> MenuActions;

		private static Dictionary<uint, string> ActionMappings;

		private static Action OnLeftClick;

		private static WndProcDelegate wndProcDelegate;

		public static Func<List<(string, Action)>> OnBuildMenu;

		private static ushort _id = 0;

		private static IntPtr CreateHIconFromTexture2D(ref Texture2D texture)
		{
			int width = texture.width;
			int height = texture.height;
			byte[] array = new byte[width * height * 4];
			GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
			gCHandle.AddrOfPinnedObject();
			try
			{
				Color32[] pixels = texture.GetPixels32();
				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						Color32 color = pixels[i * width + j];
						int num = (i * width + j) * 4;
						array[num] = color.b;
						array[num + 1] = color.g;
						array[num + 2] = color.r;
						array[num + 3] = color.a;
					}
				}
				BITMAPINFO pbmi = new BITMAPINFO
				{
					bmiHeader = 
					{
						biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
						biWidth = width,
						biHeight = height,
						biPlanes = 1,
						biBitCount = 32,
						biCompression = 0u
					}
				};
				IntPtr ppvBits;
				IntPtr intPtr = WinAPI.CreateDIBSection(IntPtr.Zero, ref pbmi, 0u, out ppvBits, IntPtr.Zero, 0u);
				if (intPtr == IntPtr.Zero)
				{
					throw new SystemException($"CreateDIBSection Failed. Error: {Marshal.GetLastWin32Error()}");
				}
				Marshal.Copy(array, 0, ppvBits, array.Length);
				IntPtr intPtr2 = WinAPI.CreateBitmap(width, height, 1u, 1u, IntPtr.Zero);
				if (intPtr2 == IntPtr.Zero)
				{
					WinAPI.DeleteObject(intPtr);
					throw new SystemException($"CreateBitmap Failed. Error: {Marshal.GetLastWin32Error()}");
				}
				ICONINFO piconinfo = new ICONINFO
				{
					fIcon = true,
					hbmMask = intPtr2,
					hbmColor = intPtr
				};
				IntPtr intPtr3 = WinAPI.CreateIconIndirect(ref piconinfo);
				if (intPtr3 == IntPtr.Zero)
				{
					throw new SystemException($"CreateIconIndirect Failed. Error: {Marshal.GetLastWin32Error()}");
				}
				WinAPI.DeleteObject(intPtr);
				WinAPI.DeleteObject(intPtr2);
				return intPtr3;
			}
			catch (Exception arg)
			{
				Debug.LogError($"Exception in CreateHIconFromTexture2D:\n{arg}");
				return IntPtr.Zero;
			}
			finally
			{
				if (gCHandle.IsAllocated)
				{
					gCHandle.Free();
				}
			}
		}

		public static void ShowBalloonTip(string title, string message, ToolTipIcon iconType, bool useSound = true)
		{
			if (!_init || messageWindowHandle == IntPtr.Zero)
			{
				Debug.LogError("TrayIcon is not initialized yet...");
				return;
			}
			notifyIconData.uFlags = 16u;
			notifyIconData.szInfoTitle = TruncateString(title, 64);
			notifyIconData.szInfo = TruncateString(message, 256);
			switch (iconType)
			{
			case ToolTipIcon.None:
				notifyIconData.dwInfoFlags = 0u;
				break;
			case ToolTipIcon.Info:
				notifyIconData.dwInfoFlags = 1u;
				break;
			case ToolTipIcon.Warning:
				notifyIconData.dwInfoFlags = 2u;
				break;
			case ToolTipIcon.Error:
				notifyIconData.dwInfoFlags = 3u;
				break;
			}
			if (!useSound)
			{
				notifyIconData.dwInfoFlags |= 16u;
			}
			if (!WinAPI.Shell_NotifyIcon(1u, ref notifyIconData))
			{
				Debug.LogError($"Shell_NotifyIcon Failed. Error: {Marshal.GetLastWin32Error()}");
			}
			notifyIconData.uFlags &= 4294967279u;
			notifyIconData.dwInfoFlags = 0u;
			notifyIconData.szInfoTitle = "";
			notifyIconData.szInfo = "";
		}

		public static void Init(string appName, string tooltip, Texture2D iconTexture, List<(string, Action)> actions = null)
		{
			if (_init)
			{
				Debug.LogError("Init can only be called once...");
				return;
			}
			if (string.IsNullOrEmpty(appName))
			{
				Debug.LogError("A title for the application is required...");
				return;
			}
			if (string.IsNullOrEmpty(tooltip))
			{
				Debug.LogError("A description when hovered is required...");
				return;
			}
			if (iconTexture == null || !iconTexture.isReadable)
			{
				Debug.LogError("Texture2D with Read/Write permission is required...");
				return;
			}
			windowClassName = appName;
			ProcessMenuActions(actions);
			hIcon = CreateHIconFromTexture2D(ref iconTexture);
			if (hIcon == IntPtr.Zero)
			{
				Debug.LogError("Failed to create icon...");
				return;
			}
			if (!CreateMessageWindow())
			{
				Debug.LogError("Failed to create message window");
				CleanupResources();
				return;
			}
			notifyIconData = new NOTIFYICONDATA
			{
				cbSize = (uint)Marshal.SizeOf(notifyIconData),
				hWnd = messageWindowHandle,
				uID = GetUniqueID(),
				uFlags = 7u,
				uCallbackMessage = 32769u,
				hIcon = hIcon,
				szTip = tooltip
			};
			if (WinAPI.Shell_NotifyIcon(0u, ref notifyIconData))
			{
				_init = true;
				Application.quitting += CleanupResources;
			}
			else
			{
				Debug.LogError($"Failed to add system tray icon. Error: {Marshal.GetLastWin32Error()}");
				CleanupResources();
			}
		}

		private static bool CreateMessageWindow()
		{
			IntPtr moduleHandle = WinAPI.GetModuleHandle(null);
			if (moduleHandle == IntPtr.Zero)
			{
				return false;
			}
			wndProcDelegate = WndProc;
			WNDCLASSEX lpwcx = new WNDCLASSEX
			{
				cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
				lpszClassName = windowClassName,
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
				hInstance = moduleHandle,
				style = 0u,
				hIcon = IntPtr.Zero,
				hIconSm = IntPtr.Zero,
				hCursor = IntPtr.Zero,
				hbrBackground = IntPtr.Zero,
				lpszMenuName = null,
				cbClsExtra = 0,
				cbWndExtra = 0
			};
			if (WinAPI.RegisterClassEx(ref lpwcx) == 0)
			{
				Debug.LogError($"RegisterClassEx Failed. Error: {Marshal.GetLastWin32Error()}");
				return false;
			}
			messageWindowHandle = WinAPI.CreateWindowEx(0u, windowClassName, windowClassName, 0u, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, moduleHandle, IntPtr.Zero);
			if (messageWindowHandle == IntPtr.Zero)
			{
				Debug.LogError($"CreateWindowEx Failed. Error: {Marshal.GetLastWin32Error()}");
				WinAPI.UnregisterClass(windowClassName, moduleHandle);
				return false;
			}
			return true;
		}

		private static void ShowContextMenu()
		{
			if (!WinAPI.GetCursorPos(out var lpPoint))
			{
				return;
			}
			IntPtr intPtr = WinAPI.CreatePopupMenu();
			if (intPtr == IntPtr.Zero)
			{
				return;
			}
			List<(string, Action)> list = ((OnBuildMenu != null) ? OnBuildMenu() : null);
			MenuActions = new Dictionary<string, Action>();
			ActionMappings = new Dictionary<uint, string>();
			uint num = 1000u;
			if (list != null)
			{
				foreach (var item in list)
				{
					if (item.Item1 == "Separator")
					{
						WinAPI.AppendMenu(intPtr, 2048u, 0u, null);
						continue;
					}
					WinAPI.AppendMenu(intPtr, 0u, num, item.Item1);
					MenuActions[item.Item1] = item.Item2;
					ActionMappings[num] = item.Item1;
					num++;
				}
			}
			WinAPI.SetForegroundWindow(messageWindowHandle);
			WinAPI.TrackPopupMenuEx(intPtr, 32u, lpPoint.X, lpPoint.Y, messageWindowHandle, IntPtr.Zero);
			WinAPI.DestroyMenu(intPtr);
		}

		private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			switch (msg)
			{
			case 32769u:
				switch ((uint)(int)lParam)
				{
				case 514u:
					OnLeftClick?.Invoke();
					break;
				case 517u:
					ShowContextMenu();
					break;
				}
				return IntPtr.Zero;
			case 273u:
			{
				uint key = (uint)((int)wParam & 0xFFFF);
				MenuActions[ActionMappings[key]]?.Invoke();
				return IntPtr.Zero;
			}
			default:
				return WinAPI.DefWindowProc(hWnd, msg, wParam, lParam);
			}
		}

		private static void CleanupResources()
		{
			IntPtr moduleHandle = WinAPI.GetModuleHandle(null);
			if (_init && messageWindowHandle != IntPtr.Zero && !WinAPI.Shell_NotifyIcon(2u, ref notifyIconData))
			{
				Debug.LogWarning("Failed to delete notifyIconData");
			}
			if (hIcon != IntPtr.Zero)
			{
				WinAPI.DestroyIcon(hIcon);
				hIcon = IntPtr.Zero;
			}
			if (messageWindowHandle != IntPtr.Zero)
			{
				WinAPI.DestroyWindow(messageWindowHandle);
				messageWindowHandle = IntPtr.Zero;
			}
			if (moduleHandle != IntPtr.Zero)
			{
				WinAPI.UnregisterClass(windowClassName, moduleHandle);
			}
			wndProcDelegate = null;
		}

		private static ushort GetUniqueID()
		{
			if (_id == 0)
			{
				_id = (ushort)(DateTime.UtcNow.Ticks % 65535);
			}
			return ++_id;
		}

		private static void ProcessMenuActions(List<(string, Action)> actions)
		{
			MenuActions = new Dictionary<string, Action>();
			ActionMappings = new Dictionary<uint, string>();
			if (actions == null)
			{
				return;
			}
			foreach (var (text, action) in actions)
			{
				if (text == "LeftClick")
				{
					OnLeftClick = action;
					continue;
				}
				uint uniqueID = GetUniqueID();
				ActionMappings[uniqueID] = text;
				MenuActions[text] = action;
			}
		}

		private static string TruncateString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
			{
				return "";
			}
			if (str.Length >= maxLength)
			{
				return str.Substring(0, maxLength - 1);
			}
			return str;
		}
	}
}
