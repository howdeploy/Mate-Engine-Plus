using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace uWindowCapture
{
	public class UwcGetBufferExample : MonoBehaviour
	{
		[SerializeField]
		private UwcWindowTexture uwcTexture;

		private Texture2D texture_;

		private Color32[] pixels_;

		private GCHandle handle_;

		private IntPtr ptr_ = IntPtr.Zero;

		private bool isValid
		{
			get
			{
				if (!uwcTexture)
				{
					return false;
				}
				UwcWindow window = uwcTexture.window;
				if (window != null)
				{
					return window.buffer != IntPtr.Zero;
				}
				return false;
			}
		}

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

		private void OnDestroy()
		{
			if (ptr_ != IntPtr.Zero)
			{
				handle_.Free();
			}
		}

		private void Update()
		{
			if (isValid)
			{
				UwcWindow window = uwcTexture.window;
				int rawWidth = window.rawWidth;
				int rawHeight = window.rawHeight;
				if (texture_ == null || rawWidth != texture_.width || rawHeight != texture_.height)
				{
					texture_ = new Texture2D(rawWidth, rawHeight, TextureFormat.RGBA32, mipChain: false);
					texture_.filterMode = FilterMode.Bilinear;
					pixels_ = texture_.GetPixels32();
					handle_ = GCHandle.Alloc(pixels_, GCHandleType.Pinned);
					ptr_ = handle_.AddrOfPinnedObject();
					GetComponent<Renderer>().material.mainTexture = texture_;
				}
				IntPtr buffer = window.buffer;
				memcpy(ptr_, buffer, rawWidth * rawHeight * 4);
				texture_.SetPixels32(pixels_);
				texture_.Apply();
			}
		}
	}
}
