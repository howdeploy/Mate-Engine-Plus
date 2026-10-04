using System.Collections.Generic;
using UnityEngine;

namespace uWindowCapture
{
	[RequireComponent(typeof(UwcWindowTextureManager))]
	public class UwcDesktopLayouter : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("meter / 1000 pixel")]
		private float scale = 1f;

		[SerializeField]
		[Tooltip("z-margin distance between windows")]
		private float zMargin = 0.1f;

		[SerializeField]
		[Tooltip("Use position filter")]
		private bool usePositionFilter = true;

		[SerializeField]
		[Tooltip("Use scale filter")]
		private bool useScaleFilter;

		[SerializeField]
		[Tooltip("Smoothing filter")]
		private float filter = 0.3f;

		private UwcWindowTextureManager manager_;

		private float basePixel => 1000f / scale;

		private void Awake()
		{
			manager_ = GetComponent<UwcWindowTextureManager>();
			manager_.onWindowTextureAdded.AddListener(InitWindow);
		}

		private void InitWindow(UwcWindowTexture windowTexture)
		{
			MoveWindow(windowTexture, useFilter: false);
			if (useScaleFilter)
			{
				windowTexture.transform.localScale = Vector3.zero;
			}
			else
			{
				ScaleWindow(windowTexture, useFilter: false);
			}
		}

		private void Update()
		{
			foreach (KeyValuePair<int, UwcWindowTexture> window in manager_.windows)
			{
				UwcWindowTexture value = window.Value;
				CheckWindow(value);
				MoveWindow(value, usePositionFilter);
				ScaleWindow(value, useScaleFilter);
			}
		}

		private void CheckWindow(UwcWindowTexture windowTexture)
		{
			windowTexture.enabled = !windowTexture.window.isIconic;
		}

		private void MoveWindow(UwcWindowTexture windowTexture, bool useFilter)
		{
			UwcWindow window = windowTexture.window;
			Vector3 point = UwcWindowUtil.ConvertDesktopCoordToUnityPosition(window, basePixel);
			point.z = (float)window.zOrder * zMargin;
			Vector3 vector = base.transform.localToWorldMatrix.MultiplyPoint3x4(point);
			windowTexture.transform.position = (useFilter ? Vector3.Slerp(windowTexture.transform.position, vector, filter) : vector);
		}

		private void ScaleWindow(UwcWindowTexture windowTexture, bool useFilter)
		{
			windowTexture.scaleControlType = WindowTextureScaleControlType.BaseScale;
			windowTexture.scalePer1000Pixel = scale;
		}
	}
}
