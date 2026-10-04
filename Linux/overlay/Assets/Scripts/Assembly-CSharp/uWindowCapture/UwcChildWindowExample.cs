using UnityEngine;

namespace uWindowCapture
{
	[RequireComponent(typeof(UwcWindowTexture))]
	public class UwcChildWindowExample : MonoBehaviour
	{
		[SerializeField]
		private string partialWindowName = "Unity";

		private Renderer renderer_;

		private UwcWindowTexture texture_;

		private UwcWindow window_;

		private void OnEnable()
		{
			texture_ = GetComponent<UwcWindowTexture>();
			texture_.window = null;
			texture_.searchTiming = WindowSearchTiming.Manual;
			texture_.updateTitle = false;
			texture_.createChildWindows = false;
			texture_.updateScaleForcely = true;
			renderer_ = GetComponent<Renderer>();
		}

		private void OnDisable()
		{
			if (window_ != null)
			{
				window_.onChildAdded.RemoveListener(OnChildAdded);
				window_.onChildRemoved.RemoveListener(OnChildRemoved);
				window_ = null;
			}
			if ((bool)texture_)
			{
				texture_.window = null;
				texture_ = null;
			}
		}

		private void Update()
		{
			UpdateRenderer();
			UpdateWindow();
		}

		private void UpdateRenderer()
		{
			if ((bool)renderer_)
			{
				renderer_.enabled = texture_.window != null;
			}
		}

		private void UpdateWindow()
		{
			if (window_ == null)
			{
				UwcWindow uwcWindow = UwcManager.Find(partialWindowName, isAltTabWindow: false);
				if (uwcWindow != null)
				{
					window_ = uwcWindow;
					window_.onChildAdded.AddListener(OnChildAdded);
					window_.onChildRemoved.AddListener(OnChildRemoved);
				}
			}
		}

		private void OnChildAdded(UwcWindow childWindow)
		{
			if ((bool)texture_)
			{
				texture_.window = childWindow;
			}
		}

		private void OnChildRemoved(UwcWindow childWindow)
		{
			if (texture_.window == childWindow)
			{
				texture_.window = null;
			}
		}
	}
}
