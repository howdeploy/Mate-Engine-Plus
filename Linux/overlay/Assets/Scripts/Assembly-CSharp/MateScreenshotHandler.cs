using System;
using System.IO;
using UnityEngine;

public class MateScreenshotHandler : MonoBehaviour
{
	public Camera targetCamera;

	public int msaa = 4;

	public void TakeScreenshot()
	{
		Camera camera = ((targetCamera != null) ? targetCamera : ((Camera.main != null) ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>()));
		if (!(camera == null))
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "MateScreenshots");
			Directory.CreateDirectory(text);
			int num = Mathf.Max(1, Screen.width * 2);
			int num2 = Mathf.Max(1, Screen.height * 2);
			int antiAliasing = Mathf.Clamp(msaa, 1, 8);
			RenderTexture renderTexture = new RenderTexture(num, num2, 24, RenderTextureFormat.ARGB32);
			renderTexture.antiAliasing = antiAliasing;
			RenderTexture active = RenderTexture.active;
			RenderTexture targetTexture = camera.targetTexture;
			CameraClearFlags clearFlags = camera.clearFlags;
			Color backgroundColor = camera.backgroundColor;
			camera.targetTexture = renderTexture;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			RenderTexture.active = renderTexture;
			camera.Render();
			Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
			texture2D.Apply();
			File.WriteAllBytes(Path.Combine(text, "MateScreenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"), texture2D.EncodeToPNG());
			camera.targetTexture = targetTexture;
			camera.clearFlags = clearFlags;
			camera.backgroundColor = backgroundColor;
			RenderTexture.active = active;
			renderTexture.Release();
			UnityEngine.Object.Destroy(renderTexture);
			UnityEngine.Object.Destroy(texture2D);
		}
	}
}
