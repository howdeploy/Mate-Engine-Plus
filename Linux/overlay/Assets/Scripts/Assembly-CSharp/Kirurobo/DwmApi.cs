using System;
using System.Runtime.InteropServices;

namespace Kirurobo
{
	internal class DwmApi
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
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

		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
		public struct MARGINS
		{
			public int cxLeftWidth;

			public int cxRightWidth;

			public int cyTopHeight;

			public int cyBottomHeight;

			public MARGINS(int left, int top, int right, int bottom)
			{
				cxLeftWidth = left;
				cyTopHeight = top;
				cxRightWidth = right;
				cyBottomHeight = bottom;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DWM_BLURBEHIND
		{
			public uint dwFlags;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fEnable;

			public RECT? hRgnBlur;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fTransitionOnMaximized;

			public const uint DWM_BB_ENABLE = 1u;

			public const uint DWM_BB_BLURREGION = 2u;

			public const uint DWM_BB_TRANSITIONONMAXIMIZED = 4u;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DWM_THUMBNAIL_PROPERTIES
		{
			public uint dwFlags;

			public RECT rcDestination;

			public RECT rcSource;

			public byte opacity;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fVisible;

			[MarshalAs(UnmanagedType.Bool)]
			public bool fSourceClientAreaOnly;

			public const uint DWM_TNP_RECTDESTINATION = 1u;

			public const uint DWM_TNP_RECTSOURCE = 2u;

			public const uint DWM_TNP_OPACITY = 4u;

			public const uint DWM_TNP_VISIBLE = 8u;

			public const uint DWM_TNP_SOURCECLIENTAREAONLY = 16u;
		}

		public enum DWMWINDOWATTRIBUTE
		{
			DWMWA_NCRENDERING_ENABLED = 1,
			DWMWA_NCRENDERING_POLICY = 2,
			DWMWA_TRANSITIONS_FORCEDISABLED = 3,
			DWMWA_ALLOW_NCPAINT = 4,
			DWMWA_CAPTION_BUTTON_BOUNDS = 5,
			DWMWA_NONCLIENT_RTL_LAYOUT = 6,
			DWMWA_FORCE_ICONIC_REPRESENTATION = 7,
			DWMWA_FLIP3D_POLICY = 8,
			DWMWA_EXTENDED_FRAME_BOUNDS = 9,
			DWMWA_HAS_ICONIC_BITMAP = 10,
			DWMWA_DISALLOW_PEEK = 11,
			DWMWA_EXCLUDED_FROM_PEEK = 12,
			DWMWA_CLOAK = 13,
			DWMWA_CLOAKED = 14,
			DWMWA_FREEZE_REPRESENTATION = 15,
			DWMWA_PASSIVE_UPDATE_MODE = 16,
			DWMWA_LAST = 17
		}

		public enum DWMNCRENDERINGPOLICY
		{
			DWMNCRP_USEWINDOWSTYLE = 0,
			DWMNCRP_DISABLED = 1,
			DWMNCRP_ENABLED = 2,
			DWMNCRP_LAST = 3
		}

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmEnableBlurBehindWindow(IntPtr hWnd, DWM_BLURBEHIND pBlurBehind);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DwmIsCompositionEnabled();

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmEnableComposition(bool bEnable);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmGetColorizationColor(out int pcrColorization, [MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern IntPtr DwmRegisterThumbnail(IntPtr dest, IntPtr source);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmUnregisterThumbnail(IntPtr hThumbnail);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmUpdateThumbnailProperties(IntPtr hThumbnail, DWM_THUMBNAIL_PROPERTIES props);

		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);

		public static void DwmExtendIntoClientAll(IntPtr hWnd)
		{
			MARGINS pMargins = new MARGINS(-1, -1, -1, -1);
			DwmExtendFrameIntoClientArea(hWnd, ref pMargins);
		}
	}
}
