using LLMUnity;
using UnityEngine;

namespace LLMUnitySamples
{
	public class RectTransformResizeHandler : MonoBehaviour
	{
		private EmptyCallback callback;

		public void SetCallBack(EmptyCallback callback)
		{
			this.callback = callback;
		}

		private void OnRectTransformDimensionsChange()
		{
			callback?.Invoke();
		}
	}
}
