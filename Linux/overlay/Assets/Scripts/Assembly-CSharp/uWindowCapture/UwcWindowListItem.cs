using UnityEngine;
using UnityEngine.UI;

namespace uWindowCapture
{
	[RequireComponent(typeof(Image))]
	public class UwcWindowListItem : MonoBehaviour
	{
		private Image image_;

		[SerializeField]
		private Color selected;

		[SerializeField]
		private Color notSelected;

		[SerializeField]
		private RawImage icon;

		[SerializeField]
		private Text title;

		[SerializeField]
		private Text x;

		[SerializeField]
		private Text y;

		[SerializeField]
		private Text z;

		[SerializeField]
		private Text width;

		[SerializeField]
		private Text height;

		[SerializeField]
		private Text status;

		public UwcWindow window { get; set; }

		public UwcWindowList list { get; set; }

		public UwcWindowTexture windowTexture { get; set; }

		private void Awake()
		{
			image_ = GetComponent<Image>();
			image_.color = notSelected;
		}

		private void Update()
		{
			if (window != null)
			{
				if (!window.hasIconTexture && !window.isIconic)
				{
					icon.texture = window.texture;
				}
				else
				{
					icon.texture = window.iconTexture;
				}
				string text = window.title;
				title.text = (string.IsNullOrEmpty(text) ? "-No Name-" : text);
				x.text = (window.isMinimized ? "-" : window.x.ToString());
				y.text = (window.isMinimized ? "-" : window.y.ToString());
				z.text = window.zOrder.ToString();
				width.text = window.width.ToString();
				height.text = window.height.ToString();
				status.text = (window.isIconic ? "Iconic" : (window.isZoomed ? "Zoomed" : "-"));
			}
		}

		public void OnClick()
		{
			if (windowTexture == null)
			{
				AddWindow();
			}
			else
			{
				RemoveWindow();
			}
		}

		private void AddWindow()
		{
			UwcWindowTextureManager windowTextureManager = list.windowTextureManager;
			windowTexture = windowTextureManager.AddWindowTexture(window);
			image_.color = selected;
		}

		public void RemoveWindow()
		{
			list.windowTextureManager.RemoveWindowTexture(window);
			windowTexture = null;
			image_.color = notSelected;
		}
	}
}
