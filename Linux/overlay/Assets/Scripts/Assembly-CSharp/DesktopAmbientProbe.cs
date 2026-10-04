using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class DesktopAmbientProbe : MonoBehaviour
{
	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}


	public Light topLight;

	public Light bottomLight;

	public Light leftLight;

	public Light rightLight;

	public bool enabledAuto = true;

	public bool driveIntensity = true;

	[Range(1f, 60f)]
	public float captureHz = 10f;

	public int captureWidth = 160;

	public int captureHeight = 90;

	public int bandThicknessPx = 120;

	public int excludeMarginPx = 12;

	[Range(0f, 1f)]
	public float smoothing = 0.85f;

	public string saveKey = "auto_ambient";

	[Range(0f, 4f)]
	public float minGrayIntensity = 0.3f;

	[Range(0f, 4f)]
	public float maxColorIntensity = 0.8f;

	[Range(0.5f, 3f)]
	public float saturationGamma = 1.3f;

	private Task<byte[]> capture;
	private readonly object captureLock = new object();
	private Process captureProcess;
	private int captureGeneration;

	private int virtX;

	private int virtY;

	private int virtW;

	private int virtH;

	private byte[] pixelBytes;

	private float nextTick;

	private Vector3 hsvTop;

	private Vector3 hsvBot;

	private Vector3 hsvLeft;

	private Vector3 hsvRight;

	private Vector3 hsvTopTarget;

	private Vector3 hsvBotTarget;

	private Vector3 hsvLeftTarget;

	private Vector3 hsvRightTarget;

	private bool inited;

	private bool hasSample;


	private void Start()
	{
		TryLoadToggle();
		InitCapture();
		inited = true;
	}

	private void OnDestroy()
	{
		ReleaseCapture();
	}

	private void OnDisable() => ReleaseCapture();

	private void TryLoadToggle()
	{
		SaveLoadHandler instance = SaveLoadHandler.Instance;
		if (instance != null && instance.data != null && instance.data.groupToggles != null && instance.data.groupToggles.TryGetValue(saveKey, out var value))
		{
			enabledAuto = value;
		}
	}

	public void SetEnabled(bool v)
	{
		enabledAuto = v;
		if (!v) ReleaseCapture();
		SaveLoadHandler instance = SaveLoadHandler.Instance;
		if (instance != null && instance.data != null)
		{
			instance.data.groupToggles[saveKey] = v;
			instance.SaveToDisk();
		}
	}

	private void LateUpdate()
	{
		if (!inited || !enabledAuto)
		{
			return;
		}
		if (Time.unscaledTime >= nextTick)
		{
			nextTick = Time.unscaledTime + 1f / Mathf.Max(1f, captureHz);
			if (EnsureCaptureValid())
			{
				CaptureAndAnalyze();
			}
		}
		SmoothTowardsTargets(Time.unscaledDeltaTime);
		ApplyToLights();
	}

	private bool EnsureCaptureValid()
	{
		var wm = WindowManager.Instance;
		if (wm == null || !File.Exists("/usr/bin/grim")) return false;
		int x = int.MaxValue, y = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
		foreach (var rect in wm.GetAllMonitors().Values)
		{
			x = Math.Min(x, rect.xMin); y = Math.Min(y, rect.yMin);
			right = Math.Max(right, rect.xMax); bottom = Math.Max(bottom, rect.yMax);
		}
		if (x == int.MaxValue || right <= x || bottom <= y) return false;
		if (x != virtX || y != virtY || right - x != virtW || bottom - y != virtH)
		{
			ReleaseCapture();
			virtX = x; virtY = y; virtW = right - x; virtH = bottom - y;
		}
		return captureWidth > 0 && captureHeight > 0;
	}

	private void InitCapture()
	{
		ReleaseCapture();
		EnsureCaptureValid();
	}

	private void ReleaseCapture()
	{
		lock (captureLock)
		{
			captureGeneration++;
			try { if (captureProcess != null && !captureProcess.HasExited) captureProcess.Kill(); }
			catch (InvalidOperationException) { }
		}
	}

	private IntPtr GetUnityHwnd()
	{
		return WindowManager.Instance == null ? IntPtr.Zero : WindowManager.Instance.UnityWindow;
	}

	private static bool GetWindowRect(IntPtr hwnd, out RECT rect)
	{
		rect = default;
		if (WindowManager.Instance == null || !WindowManager.Instance.GetWindowRect(hwnd, out var value)) return false;
		rect = new RECT { left = value.xMin, top = value.yMin, right = value.xMax, bottom = value.yMax };
		return true;
	}

	private void CaptureAndAnalyze()
	{
		if (capture != null && !capture.IsCompleted) return;
		pixelBytes = capture == null ? null : capture.Result;
		int generation = captureGeneration, width = captureWidth, height = captureHeight;
		string geometry = string.Format(CultureInfo.InvariantCulture, "{0},{1} {2}x{3}", virtX, virtY, virtW, virtH);
		string scale = Math.Max((double)width / virtW, (double)height / virtH).ToString(CultureInfo.InvariantCulture);
		// One bounded readback at a time; Unity objects are only used on this thread.
		capture = Task.Run(() => ReadDesktop(geometry, scale, width, height, generation));
		if (pixelBytes == null || pixelBytes.Length != width * height * 4) return;
		RECT lpRect = default(RECT);
		IntPtr unityHwnd = GetUnityHwnd();
		bool num = unityHwnd != IntPtr.Zero && GetWindowRect(unityHwnd, out lpRect);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		if (num)
		{
			num2 = Mathf.RoundToInt((float)(lpRect.left - virtX) / (float)virtW * (float)captureWidth);
			num3 = Mathf.RoundToInt((float)(lpRect.top - virtY) / (float)virtH * (float)captureHeight);
			num4 = Mathf.RoundToInt((float)(lpRect.right - virtX) / (float)virtW * (float)captureWidth);
			num5 = Mathf.RoundToInt((float)(lpRect.bottom - virtY) / (float)virtH * (float)captureHeight);
		}
		int num6 = Mathf.Max(1, Mathf.RoundToInt((float)bandThicknessPx * ((float)captureHeight / (float)Mathf.Max(1, virtH))));
		int num7 = Mathf.Max(0, Mathf.RoundToInt((float)excludeMarginPx * ((float)captureHeight / (float)Mathf.Max(1, virtH))));
		RectInt r = new RectInt(0, Mathf.Max(0, num3 - num6), captureWidth, Mathf.Clamp(num6, 1, captureHeight));
		RectInt r2 = new RectInt(0, Mathf.Min(captureHeight - num6, num5), captureWidth, Mathf.Clamp(num6, 1, captureHeight));
		RectInt r3 = new RectInt(Mathf.Max(0, num2 - num6), Mathf.Clamp(num3, 0, captureHeight - 1), Mathf.Clamp(num6, 1, captureWidth), Mathf.Clamp(num5 - num3, 1, captureHeight));
		RectInt r4 = new RectInt(Mathf.Min(captureWidth - num6, num4), Mathf.Clamp(num3, 0, captureHeight - 1), Mathf.Clamp(num6, 1, captureWidth), Mathf.Clamp(num5 - num3, 1, captureHeight));
		if (num)
		{
			r = ClampRect(r, captureWidth, captureHeight);
			r2 = ClampRect(r2, captureWidth, captureHeight);
			r3 = ClampRect(r3, captureWidth, captureHeight);
			r4 = ClampRect(r4, captureWidth, captureHeight);
			RectInt inside = new RectInt(Mathf.Clamp(num2 - num7, 0, captureWidth - 1), Mathf.Clamp(num3 - num7, 0, captureHeight - 1), Mathf.Clamp(num4 - num2 + 2 * num7, 1, captureWidth), Mathf.Clamp(num5 - num3 + 2 * num7, 1, captureHeight));
			Exclude(ref r, inside);
			Exclude(ref r2, inside);
			Exclude(ref r3, inside);
			Exclude(ref r4, inside);
		}
		else
		{
			int num8 = Mathf.Max(1, captureHeight / 5);
			int num9 = Mathf.Max(1, captureWidth / 8);
			r = new RectInt(0, num8, captureWidth, num8);
			r2 = new RectInt(0, captureHeight - num8 * 2, captureWidth, num8);
			r3 = new RectInt(num9, num8, num9, captureHeight - 2 * num8);
			r4 = new RectInt(captureWidth - num9 * 2, num8, num9, captureHeight - 2 * num8);
		}
		Color rgbColor = AvgColor(r);
		Color rgbColor2 = AvgColor(r2);
		Color rgbColor3 = AvgColor(r3);
		Color rgbColor4 = AvgColor(r4);
		Color.RGBToHSV(rgbColor, out var H, out var S, out var V);
		Color.RGBToHSV(rgbColor2, out var H2, out var S2, out var V2);
		Color.RGBToHSV(rgbColor3, out var H3, out var S3, out var V3);
		Color.RGBToHSV(rgbColor4, out var H4, out var S4, out var V4);
		Vector3 vector = new Vector3(H, S, V);
		Vector3 vector2 = new Vector3(H2, S2, V2);
		Vector3 vector3 = new Vector3(H3, S3, V3);
		Vector3 vector4 = new Vector3(H4, S4, V4);
		if (!hasSample)
		{
			hsvTop = vector;
			hsvBot = vector2;
			hsvLeft = vector3;
			hsvRight = vector4;
			hsvTopTarget = vector;
			hsvBotTarget = vector2;
			hsvLeftTarget = vector3;
			hsvRightTarget = vector4;
			hasSample = true;
		}
		else
		{
			hsvTopTarget = vector;
			hsvBotTarget = vector2;
			hsvLeftTarget = vector3;
			hsvRightTarget = vector4;
		}
	}

	private byte[] ReadDesktop(string geometry, string scale, int width, int height, int generation)
	{
		using (var process = new Process())
		using (var output = new MemoryStream())
		{
			try
			{
				process.StartInfo = new ProcessStartInfo("/usr/bin/grim", "-t ppm -g \"" + geometry + "\" -s " + scale + " -")
				{ UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
				lock (captureLock)
				{
					if (generation != captureGeneration) return null;
					process.Start();
					captureProcess = process;
				}
				var read = process.StandardOutput.BaseStream.CopyToAsync(output);
				var errors = process.StandardError.ReadToEndAsync();
				if (!process.WaitForExit(2000))
				{
					process.Kill();
					process.WaitForExit(500);
					return null;
				}
				if (!read.Wait(500) || !errors.Wait(500) || process.ExitCode != 0) return null;
				lock (captureLock) if (generation != captureGeneration) return null;
				var bytes = output.ToArray();
				int offset = 0;
				if (ReadPpmToken(bytes, ref offset) != "P6") return null;
				int sourceWidth = int.Parse(ReadPpmToken(bytes, ref offset), CultureInfo.InvariantCulture);
				int sourceHeight = int.Parse(ReadPpmToken(bytes, ref offset), CultureInfo.InvariantCulture);
				if (ReadPpmToken(bytes, ref offset) != "255" || sourceWidth <= 0 || sourceHeight <= 0 ||
					(long)sourceWidth * sourceHeight * 3 != bytes.Length - offset) return null;
				var pixels = new byte[checked(width * height * 4)];
				for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					int from = offset + ((y * sourceHeight / height) * sourceWidth + x * sourceWidth / width) * 3;
					int to = (y * width + x) * 4;
					pixels[to] = bytes[from + 2]; pixels[to + 1] = bytes[from + 1];
					pixels[to + 2] = bytes[from]; pixels[to + 3] = 255;
				}
				return pixels;
			}
			catch (Exception) { return null; }
			finally
			{
				lock (captureLock) if (captureProcess == process) captureProcess = null;
			}
		}
	}

	private static string ReadPpmToken(byte[] bytes, ref int offset)
	{
		while (offset < bytes.Length)
		{
			if (bytes[offset] == '#') { while (offset < bytes.Length && bytes[offset] != '\n') offset++; }
			else if (bytes[offset] <= 32) offset++;
			else break;
		}
		int start = offset;
		while (offset < bytes.Length && bytes[offset] > 32) offset++;
		string token = System.Text.Encoding.ASCII.GetString(bytes, start, offset - start);
		if (offset < bytes.Length && bytes[offset++] == '\r' && offset < bytes.Length && bytes[offset] == '\n') offset++;
		return token;
	}

	private RectInt ClampRect(RectInt r, int w, int h)
	{
		int num = Mathf.Clamp(r.x, 0, w - 1);
		int num2 = Mathf.Clamp(r.y, 0, h - 1);
		int width = Mathf.Clamp(r.width, 1, w - num);
		int height = Mathf.Clamp(r.height, 1, h - num2);
		return new RectInt(num, num2, width, height);
	}

	private void Exclude(ref RectInt r, RectInt inside)
	{
		if (r.Overlaps(inside))
		{
			int num = Mathf.Max(r.x, inside.x);
			int num2 = Mathf.Min(r.x + r.width, inside.x + inside.width);
			int num3 = Mathf.Max(r.y, inside.y);
			int num4 = Mathf.Min(r.y + r.height, inside.y + inside.height);
			RectInt rectInt = new RectInt(r.x, r.y, r.width, Mathf.Max(0, num3 - r.y));
			RectInt rectInt2 = new RectInt(r.x, num4, r.width, Mathf.Max(0, r.y + r.height - num4));
			RectInt rectInt3 = new RectInt(r.x, num3, Mathf.Max(0, num - r.x), Mathf.Max(0, num4 - num3));
			RectInt rectInt4 = new RectInt(num2, num3, Mathf.Max(0, r.x + r.width - num2), Mathf.Max(0, num4 - num3));
			RectInt rectInt5 = rectInt;
			if (rectInt2.width * rectInt2.height > rectInt5.width * rectInt5.height)
			{
				rectInt5 = rectInt2;
			}
			if (rectInt3.width * rectInt3.height > rectInt5.width * rectInt5.height)
			{
				rectInt5 = rectInt3;
			}
			if (rectInt4.width * rectInt4.height > rectInt5.width * rectInt5.height)
			{
				rectInt5 = rectInt4;
			}
			r = ((rectInt5.width > 0 && rectInt5.height > 0) ? rectInt5 : new RectInt(r.x, r.y, 1, 1));
		}
	}

	private Color AvgColor(RectInt r)
	{
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		int num4 = 0;
		int num5 = captureWidth * 4;
		int x = r.x;
		int num6 = r.x + r.width;
		int y = r.y;
		int num7 = r.y + r.height;
		for (int i = y; i < num7; i++)
		{
			int num8 = i * num5;
			for (int j = x; j < num6; j++)
			{
				int num9 = num8 + j * 4;
				byte b = pixelBytes[num9];
				byte b2 = pixelBytes[num9 + 1];
				byte num10 = pixelBytes[num9 + 3];
				byte b3 = pixelBytes[num9 + 2];
				if (num10 != 0)
				{
					num += b3;
					num2 += b2;
					num3 += b;
					num4++;
				}
			}
		}
		if (num4 == 0)
		{
			return Color.black;
		}
		float r2 = (float)num / (255f * (float)num4);
		float g = (float)num2 / (255f * (float)num4);
		float b4 = (float)num3 / (255f * (float)num4);
		return new Color(r2, g, b4, 1f);
	}

	private void SmoothTowardsTargets(float dt)
	{
		if (hasSample)
		{
			float b = 0.05f + 1.5f * Mathf.Clamp01(smoothing);
			float a = 1f - Mathf.Exp((0f - dt) / Mathf.Max(0.0001f, b));
			hsvTop = DampHSV(hsvTop, hsvTopTarget, a);
			hsvBot = DampHSV(hsvBot, hsvBotTarget, a);
			hsvLeft = DampHSV(hsvLeft, hsvLeftTarget, a);
			hsvRight = DampHSV(hsvRight, hsvRightTarget, a);
		}
	}

	private Vector3 DampHSV(Vector3 cur, Vector3 target, float a)
	{
		float num = Mathf.DeltaAngle(cur.x * 360f, target.x * 360f) / 360f;
		float x = Mathf.Repeat(cur.x + a * num, 1f);
		float y = Mathf.Lerp(cur.y, target.y, a);
		float z = Mathf.Lerp(cur.z, target.z, a);
		return new Vector3(x, y, z);
	}

	private void ApplyToLights()
	{
		ApplyLight(topLight, hsvTop);
		ApplyLight(bottomLight, hsvBot);
		ApplyLight(leftLight, hsvLeft);
		ApplyLight(rightLight, hsvRight);
	}

	private void ApplyLight(Light L, Vector3 hsv)
	{
		if (!(L == null))
		{
			Color color = Color.HSVToRGB(hsv.x, hsv.y, 1f);
			L.color = color;
			if (driveIntensity)
			{
				float value = Mathf.Lerp(minGrayIntensity, maxColorIntensity, Mathf.Pow(Mathf.Clamp01(hsv.y), saturationGamma));
				L.intensity = Mathf.Clamp(value, 0f, 4f);
			}
		}
	}
}
