using UnityEngine;

namespace uWindowCapture
{
	[RequireComponent(typeof(UwcWindowTexture))]
	public class UwcNewWindowExample : MonoBehaviour
	{
		[SerializeField]
		private float delay = 1f;

		private float delayTimer_;

		private Renderer renderer_;

		private UwcWindowTexture texture_;

		private bool isReady => delayTimer_ > delay;

		private void OnEnable()
		{
			UwcManager.onWindowAdded.AddListener(OnWindowAdded);
			UwcManager.onWindowRemoved.AddListener(OnWindowRemoved);
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
			UwcManager.onWindowAdded.RemoveListener(OnWindowAdded);
			UwcManager.onWindowAdded.RemoveListener(OnWindowRemoved);
			if ((bool)texture_)
			{
				texture_.window = null;
				texture_ = null;
			}
		}

		private void Update()
		{
			UpdateRenderer();
			delayTimer_ += Time.deltaTime;
		}

		private void UpdateRenderer()
		{
			if ((bool)renderer_)
			{
				renderer_.enabled = texture_.window != null;
			}
		}

		private void OnWindowAdded(UwcWindow window)
		{
			if (isReady && (bool)texture_)
			{
				texture_.window = window;
			}
		}

		private void OnWindowRemoved(UwcWindow window)
		{
			if (texture_.window == window)
			{
				texture_.window = null;
			}
		}
	}
}
