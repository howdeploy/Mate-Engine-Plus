using UnityEngine;

namespace uWindowCapture
{
	public class UwcGetPixelsExample : MonoBehaviour
	{
		[SerializeField]
		private UwcWindowTexture uwcTexture;

		[SerializeField]
		private int x = 100;

		[SerializeField]
		private int y = 100;

		[SerializeField]
		private int w = 64;

		[SerializeField]
		private int h = 32;

		public Texture2D texture;

		private Color32[] colors;

		private void CreateTextureIfNeeded()
		{
			if (!texture || texture.width != w || texture.height != h)
			{
				colors = new Color32[w * h];
				texture = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
				GetComponent<Renderer>().material.mainTexture = texture;
			}
		}

		private void Start()
		{
			CreateTextureIfNeeded();
		}

		private void Update()
		{
			CreateTextureIfNeeded();
			UwcWindow window = uwcTexture.window;
			if (window != null && window.width != 0 && window.GetPixels(colors, x, y, w, h))
			{
				texture.SetPixels32(colors);
				texture.Apply();
			}
		}
	}
}
