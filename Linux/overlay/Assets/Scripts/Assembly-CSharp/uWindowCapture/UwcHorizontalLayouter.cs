using System.Collections.Generic;
using UnityEngine;

namespace uWindowCapture
{
	[RequireComponent(typeof(UwcWindowTextureManager))]
	public class UwcHorizontalLayouter : MonoBehaviour
	{
		private UwcWindowTextureManager manager_;

		private void Awake()
		{
			manager_ = GetComponent<UwcWindowTextureManager>();
		}

		private void Update()
		{
			Vector3 zero = Vector3.zero;
			foreach (KeyValuePair<int, UwcWindowTexture> window in manager_.windows)
			{
				UwcWindowTexture value = window.Value;
				float x = value.transform.localScale.x;
				zero += new Vector3(x * 0.5f, 0f, 0f);
				value.transform.localPosition = zero;
				zero += new Vector3(x * 0.5f, 0f, 0f);
			}
		}
	}
}
