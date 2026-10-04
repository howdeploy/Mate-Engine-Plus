using UnityEngine;

namespace uWindowCapture
{
	public class UwcGetPixelExample : MonoBehaviour
	{
		[SerializeField]
		private UwcWindowTexture uwcTexture;

		private Material material_;

		private void Start()
		{
			material_ = GetComponent<Renderer>().material;
		}

		private void Update()
		{
			UwcWindow window = uwcTexture.window;
			if (window != null && UwcManager.cursorWindow == window)
			{
				Point cursorPosition = Lib.GetCursorPosition();
				int x = cursorPosition.x - window.x;
				int y = cursorPosition.y - window.y;
				material_.color = window.GetPixel(x, y);
			}
		}
	}
}
