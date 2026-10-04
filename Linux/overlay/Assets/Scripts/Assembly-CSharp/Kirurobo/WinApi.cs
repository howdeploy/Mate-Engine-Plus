using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Kirurobo
{
	public class WinApi
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct RECT
		{
			public int left;

			public int top;

			public int right;

			public int bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct COLORREF
		{
			public uint color;

			public COLORREF(uint color)
			{
				this.color = color;
			}

			public COLORREF(byte r, byte g, byte b)
			{
				color = (uint)(b * 65536 + g * 256 + r);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CHANGEFILTERSTRUCT
		{
			public int cbSize;

			public uint extStatus;

			public CHANGEFILTERSTRUCT(uint msgFltInfo)
			{
				extStatus = msgFltInfo;
				cbSize = Marshal.SizeOf(typeof(CHANGEFILTERSTRUCT));
			}

			public override string ToString()
			{
				return $"ExtStatus:{extStatus}";
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct POINT
		{
			public int x;

			public int y;

			public override string ToString()
			{
				return "(" + x + "," + y + ")";
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CURSORINFO
		{
			public int cbSize;

			public int flags;

			public IntPtr hCursor;

			public POINT ptScreenPos;

			public override string ToString()
			{
				return $"Flags:{flags}, HCursor:{hCursor}, Point:{ptScreenPos.ToString()}";
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CWPSTRUCT
		{
			public ulong lParam;

			public ulong wParam;

			public uint message;

			public IntPtr hwnd;
		}

		public struct MSG
		{
			public IntPtr hwnd;

			public uint message;

			public IntPtr wParam;

			public IntPtr lParam;

			public ushort time;

			public POINT pt;
		}

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate IntPtr HookProc(int code, IntPtr wParam, ref MSG lParam);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public class OpenFileName
		{
			public int structSize;

			public IntPtr dlgOwner = IntPtr.Zero;

			public IntPtr instance = IntPtr.Zero;

			public string filter;

			public string customFilter;

			public int maxCustFilter;

			public int filterIndex;

			public string file;

			public int maxFile;

			public string fileTitle;

			public int maxFileTitle;

			public string initialDir;

			public string title;

			public int flags;

			public short fileOffset;

			public short fileExtension;

			public string defExt;

			public IntPtr custData = IntPtr.Zero;

			public IntPtr hook = IntPtr.Zero;

			public string templateName;

			public IntPtr reservedPtr = IntPtr.Zero;

			public int reservedInt;

			public int flagsEx;

			public static readonly int OFN_ALLOWMULTISELECT = 512;

			public static readonly int OFN_CREATEPROMPT = 512;

			public static readonly int OFN_DONTADDTORECENT = 33554432;

			public static readonly int OFN_ENABLEHOOK = 32;

			public static readonly int OFN_ENABLEINCLUDENOTIFY = 4194304;

			public static readonly int OFN_ENABLESIZING = 8388608;

			public static readonly int OFN_ENABLETEMPLATE = 64;

			public static readonly int OFN_ENABLETEMPLATEHANDLE = 128;

			public static readonly int OFN_EXPLORER = 524288;

			public static readonly int OFN_EXTENSIONDIFFERENT = 1024;

			public static readonly int OFN_FILEMUSTEXIST = 4096;

			public static readonly int OFN_FORCESHOWHIDDEN = 268435456;

			public static readonly int OFN_HIDEREADONLY = 4;

			public static readonly int OFN_LONGNAMES = 2097152;

			public static readonly int OFN_NOCHANGEDIR = 8;

			public static readonly int OFN_NODEREFERENCELINKS = 1048576;

			public static readonly int OFN_NOLONGNAMES = 262144;

			public static readonly int OFN_NONETWORKBUTTON = 131072;

			public static readonly int OFN_NOREADONLYRETURN = 32768;

			public static readonly int OFN_NOTESTFILECREATE = 65536;

			public static readonly int OFN_NOVALIDATE = 256;

			public static readonly int OFN_OVERWRITEPROMPT = 2;

			public static readonly int OFN_PATHMUSTEXIST = 2048;

			public static readonly int OFN_READONLY = 1;

			public static readonly int OFN_SHAREAWARE = 16384;

			public static readonly int OFN_SHOWHELP = 16;

			public OpenFileName()
			{
				structSize = Marshal.SizeOf(this);
				file = new string('\0', 4096);
				maxFile = file.Length;
				fileTitle = new string('\0', 256);
				maxFileTitle = fileTitle.Length;
				title = "Open";
				flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
			}
		}

		public static readonly int GWL_STYLE = -16;

		public static readonly int SW_HIDE = 0;

		public static readonly int SW_MAXIMIZE = 3;

		public static readonly int SW_MINIMIZE = 6;

		public static readonly int SW_RESTORE = 9;

		public static readonly int SW_SHOW = 5;

		public static readonly uint SWP_REFRESH = 567u;

		public static readonly uint SWP_NOSIZE = 1u;

		public static readonly uint SWP_NOMOVE = 2u;

		public static readonly uint SWP_NOZORDER = 4u;

		public static readonly uint SWP_NOACTIVATE = 16u;

		public static readonly uint SWP_FRAMECHANGED = 32u;

		public static readonly uint SWP_SHOWWINDOW = 64u;

		public static readonly uint SWP_NOCOPYBITS = 256u;

		public static readonly uint SWP_NOOWNERZORDER = 512u;

		public static readonly uint SWP_NOREPOSITION = 512u;

		public static readonly uint SWP_NOSENDCHANGING = 1024u;

		public static readonly uint SWP_ASYNCWINDOWPOS = 16384u;

		public static readonly ulong WS_BORDER = 8388608uL;

		public static readonly ulong WS_VISIBLE = 268435456uL;

		public static readonly ulong WS_OVERLAPPED = 0uL;

		public static readonly ulong WS_CAPTION = 12582912uL;

		public static readonly ulong WS_SYSMENU = 524288uL;

		public static readonly ulong WS_THICKFRAME = 262144uL;

		public static readonly ulong WS_ICONIC = 536870912uL;

		public static readonly ulong WS_MINIMIZE = 536870912uL;

		public static readonly ulong WS_MAXIMIZE = 16777216uL;

		public static readonly ulong WS_MINIMIZEBOX = 131072uL;

		public static readonly ulong WS_MAXIMIZEBOX = 65536uL;

		public static readonly ulong WS_POPUP = 2147483648uL;

		public static readonly ulong WS_OVERLAPPEDWINDOW = 13565952uL;

		public static readonly ulong WS_EX_TRANSPARENT = 32uL;

		public static readonly ulong WS_EX_LAYERED = 524288uL;

		public static readonly ulong WS_EX_TOPMOST = 8uL;

		public static readonly ulong WS_EX_OVERLAPPEDWINDOW = 768uL;

		public static readonly ulong WS_EX_ACCEPTFILES = 16uL;

		public static readonly IntPtr HWND_TOP = new IntPtr(0);

		public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

		public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

		public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

		public static readonly uint GA_PARENT = 1u;

		public static readonly uint GA_ROOT = 2u;

		public static readonly uint GA_ROOTOWNER = 3u;

		public static readonly uint GW_HWNDFIRST = 0u;

		public static readonly uint GW_HWNDLAST = 1u;

		public static readonly uint GW_HWNDNEXT = 2u;

		public static readonly uint GW_HWNDPREV = 3u;

		public static readonly uint GW_OWNER = 4u;

		public static readonly uint GW_CHILD = 5u;

		public static readonly uint WM_IME_CHAR = 646u;

		public static readonly uint WM_SETTEXT = 12u;

		public static readonly uint WM_NCDESTROY = 130u;

		public static readonly uint WM_WINDOWPOSCHANGING = 70u;

		public static readonly uint WM_DROPFILES = 563u;

		public static readonly uint WM_COPYDATA = 74u;

		public static readonly uint WM_COPYGLOBALDATA = 73u;

		public static readonly uint MSGFLT_ALLOW = 1u;

		public static readonly uint MSGFLT_DISALLOW = 2u;

		public static readonly uint MSGFLT_RESET = 0u;

		public static readonly uint MSGFLTINFO_NONE = 0u;

		public static readonly uint MSGFLTINFO_ALLOWED_HIGHER = 3u;

		public static readonly uint MSGFLTINFO_ALREADYALLOWED_FORWND = 1u;

		public static readonly uint MSGFLTINFO_ALREADYDISALLOWED_FORWND = 2u;

		public static readonly uint ULW_COLORKEY = 1u;

		public static readonly uint ULW_ALPHA = 2u;

		public static readonly uint ULW_OPAQUE = 4u;

		public static readonly uint LWA_COLORKEY = 1u;

		public static readonly uint LWA_ALPHA = 2u;

		public static readonly ulong MOUSEEVENTF_ABSOLUTE = 32768uL;

		public static readonly ulong MOUSEEVENTF_LEFTDOWN = 2uL;

		public static readonly ulong MOUSEEVENTF_LEFTUP = 4uL;

		public static readonly ulong MOUSEEVENTF_MIDDLEDOWN = 32uL;

		public static readonly ulong MOUSEEVENTF_MIDDLEUP = 64uL;

		public static readonly ulong MOUSEEVENTF_MOVE = 1uL;

		public static readonly ulong MOUSEEVENTF_RIGHTDOWN = 8uL;

		public static readonly ulong MOUSEEVENTF_RIGHTUP = 16uL;

		public static readonly ulong MOUSEEVENTF_XDOWN = 128uL;

		public static readonly ulong MOUSEEVENTF_XUP = 256uL;

		public static readonly ulong MOUSEEVENTF_WHEEL = 2048uL;

		public static readonly ulong MOUSEEVENTF_HWHEEL = 4096uL;

		public static readonly ulong XBUTTON1 = 1uL;

		public static readonly ulong XBUTTON2 = 2uL;

		public static readonly int GWL_EXSTYLE = -20;

		public static readonly int GWLP_HINSTANCE = -6;

		public static readonly int GWLP_ID = -12;

		public static readonly int GWLP_STYLE = -16;

		public static readonly int GWLP_USERDATA = -21;

		public static readonly int GWLP_WNDPROC = -4;

		public static readonly int WH_CALLWNDPROC = 4;

		public static readonly int WH_CALLWNDPROCRET = 12;

		public static readonly int WH_CBT = 5;

		public static readonly int WH_DEBUG = 9;

		public static readonly int WH_FOREGROUNDIDLE = 11;

		public static readonly int WH_GETMESSAGE = 3;

		public static readonly int WH_JOURNALPLAYBACK = 1;

		public static readonly int WH_JOURNALRECORD = 0;

		public static readonly int WH_KEYBOARD = 2;

		public static readonly int WH_KEYBOARD_LL = 13;

		public static readonly int WH_MOUSE = 7;

		public static readonly int WH_MOUSE_LL = 14;

		public static readonly int WH_MSGFILTER = -1;

		public static readonly int WH_SHELL = 10;

		public static readonly int WH_SYSMSGFILTER = 6;

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumChildWindows(IntPtr hWnd, EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Ansi)]
		public static extern int GetWindowText(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Ansi)]
		public static extern int GetClassName(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out ulong lpdwProcessId);

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpszClass, string lpszTitle);

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(IntPtr lpszClass, string lpszTitle);

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpszClass, IntPtr lpszTitle);

		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndNewParent);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.U1)] bool bEnable);

		[DllImport("user32.dll")]
		public static extern IntPtr SetFocus(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsZoomed(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern ulong SetWindowLong(IntPtr hWnd, int nIndex, ulong value);

		[DllImport("user32.dll")]
		public static extern ulong GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		public static extern int SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewPtr);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PostMessage(IntPtr hWnd, uint msg, ulong wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, IntPtr pptDst, IntPtr psize, IntPtr hdcSrc, IntPtr pptSrc, COLORREF crKey, IntPtr pblend, uint dwFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, COLORREF crKey, byte bAlpha, uint dwFlags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeWindowMessageFilter(uint msg, uint dwFlag);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, out CHANGEFILTERSTRUCT pChangeFilterStruct);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out POINT point);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorInfo(ref CURSORINFO pcursorinfo);

		[DllImport("user32.dll")]
		public static extern uint mouse_event(ulong dwFlags, int dx, int dy, ulong dwData, IntPtr dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("shell32.dll")]
		public static extern void DragAcceptFiles(IntPtr hWnd, bool bAccept);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszFile, uint cch);

		[DllImport("shell32.dll")]
		public static extern void DragFinish(IntPtr hDrop);

		public static IntPtr SetWindowProcedure(IntPtr hWnd, IntPtr wndProcPtr)
		{
			if (IntPtr.Size == 8)
			{
				return SetWindowLongPtr(hWnd, GWLP_WNDPROC, wndProcPtr);
			}
			return new IntPtr(SetWindowLongPtr32(hWnd, GWLP_WNDPROC, wndProcPtr.ToInt32()));
		}

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll")]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref MSG lParam);

		[DllImport("kernel32.dll")]
		public static extern ulong GetLastError();

		[DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true, ThrowOnUnmappableChar = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetOpenFileName([In][Out] OpenFileName lpofn);
	}
}
