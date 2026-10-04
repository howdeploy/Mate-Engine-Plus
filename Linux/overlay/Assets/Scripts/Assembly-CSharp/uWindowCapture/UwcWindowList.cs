using System.Collections.Generic;
using UnityEngine;

namespace uWindowCapture
{
	public class UwcWindowList : MonoBehaviour
	{
		[SerializeField]
		private GameObject windowListItem;

		[SerializeField]
		private Transform listRoot;

		public UwcWindowTextureManager windowTextureManager;

		private Dictionary<int, UwcWindowListItem> items_ = new Dictionary<int, UwcWindowListItem>();

		private void Start()
		{
			UwcManager.onWindowAdded.AddListener(OnWindowAdded);
			UwcManager.onWindowRemoved.AddListener(OnWindowRemoved);
			foreach (KeyValuePair<int, UwcWindow> window in UwcManager.windows)
			{
				OnWindowAdded(window.Value);
			}
		}

		private void OnWindowAdded(UwcWindow window)
		{
			if (window.isAltTabWindow && !window.isBackground)
			{
				UwcWindowListItem component = Object.Instantiate(windowListItem, listRoot, worldPositionStays: false).GetComponent<UwcWindowListItem>();
				component.window = window;
				component.list = this;
				items_.Add(window.id, component);
				window.RequestCaptureIcon();
				window.RequestCapture(CapturePriority.Low);
			}
		}

		private void OnWindowRemoved(UwcWindow window)
		{
			items_.TryGetValue(window.id, out var value);
			if ((bool)value)
			{
				value.RemoveWindow();
				Object.Destroy(value.gameObject);
			}
		}
	}
}
