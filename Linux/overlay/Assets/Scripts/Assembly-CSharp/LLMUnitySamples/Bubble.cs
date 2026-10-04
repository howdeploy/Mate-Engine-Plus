using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	internal class Bubble
	{
		protected GameObject bubbleObject;

		protected GameObject imageObject;

		public BubbleUI bubbleUI;

		public Bubble(Transform parent, BubbleUI ui, string name, string message)
		{
			bubbleUI = ui;
			bubbleObject = CreateTextObject(parent, name, message, bubbleUI.bubbleWidth == -1f, bubbleUI.bubbleHeight == -1f);
			imageObject = CreateImageObject(bubbleObject.transform, "Image");
			SetBubblePosition(bubbleObject.GetComponent<RectTransform>(), imageObject.GetComponent<RectTransform>(), bubbleUI);
			SetSortingOrder(bubbleObject, imageObject);
		}

		public void SyncParentRectTransform(RectTransform rectTransform)
		{
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
		}

		protected GameObject CreateTextObject(Transform parent, string name, string message, bool horizontalStretch = true, bool verticalStretch = false)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Canvas));
			gameObject.transform.SetParent(parent);
			Text component = gameObject.GetComponent<Text>();
			if (verticalStretch || horizontalStretch)
			{
				ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
				if (verticalStretch)
				{
					contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				}
				if (horizontalStretch)
				{
					contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				}
			}
			component.text = message;
			if (bubbleUI.font != null)
			{
				component.font = bubbleUI.font;
			}
			component.fontSize = bubbleUI.fontSize;
			component.color = bubbleUI.fontColor;
			return gameObject;
		}

		protected GameObject CreateImageObject(Transform parent, string name)
		{
			GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Canvas));
			gameObject.transform.SetParent(parent);
			gameObject.GetComponent<RectTransform>();
			Image component = gameObject.GetComponent<Image>();
			component.type = Image.Type.Sliced;
			component.sprite = bubbleUI.sprite;
			component.color = bubbleUI.bubbleColor;
			return gameObject;
		}

		private void SetBubblePosition(RectTransform bubbleRectTransform, RectTransform imageRectTransform, BubbleUI bubbleUI)
		{
			bubbleRectTransform.pivot = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
			bubbleRectTransform.anchorMin = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
			bubbleRectTransform.anchorMax = new Vector2(bubbleUI.leftPosition, bubbleUI.bottomPosition);
			bubbleRectTransform.localScale = Vector3.one;
			Vector2 anchoredPosition = new Vector2(bubbleUI.bubbleOffset + bubbleUI.textPadding, bubbleUI.bubbleOffset + bubbleUI.textPadding);
			if (bubbleUI.leftPosition == 1f)
			{
				anchoredPosition.x *= -1f;
			}
			if (bubbleUI.bottomPosition == 1f)
			{
				anchoredPosition.y *= -1f;
			}
			bubbleRectTransform.anchoredPosition = anchoredPosition;
			float num = ((bubbleUI.bubbleWidth == -1f) ? bubbleRectTransform.sizeDelta.x : bubbleUI.bubbleWidth);
			float num2 = ((bubbleUI.bubbleHeight == -1f) ? bubbleRectTransform.sizeDelta.y : bubbleUI.bubbleHeight);
			bubbleRectTransform.sizeDelta = new Vector2(num - 2f * bubbleUI.textPadding, num2 - 2f * bubbleUI.textPadding);
			SyncParentRectTransform(imageRectTransform);
			imageRectTransform.offsetMin = new Vector2(0f - bubbleUI.textPadding, 0f - bubbleUI.textPadding);
			imageRectTransform.offsetMax = new Vector2(bubbleUI.textPadding, bubbleUI.textPadding);
		}

		private void SetSortingOrder(GameObject bubbleObject, GameObject imageObject)
		{
			Canvas component = bubbleObject.GetComponent<Canvas>();
			component.overrideSorting = true;
			component.sortingOrder = 2;
			Canvas component2 = imageObject.GetComponent<Canvas>();
			component2.overrideSorting = true;
			component2.sortingOrder = 1;
		}

		public void OnResize(EmptyCallback callback)
		{
			bubbleObject.AddComponent<RectTransformResizeHandler>().SetCallBack(callback);
		}

		public RectTransform GetRectTransform()
		{
			return bubbleObject.GetComponent<RectTransform>();
		}

		public RectTransform GetOuterRectTransform()
		{
			return imageObject.GetComponent<RectTransform>();
		}

		public Vector2 GetSize()
		{
			return bubbleObject.GetComponent<RectTransform>().sizeDelta + imageObject.GetComponent<RectTransform>().sizeDelta;
		}

		public string GetText()
		{
			return bubbleObject.GetComponent<Text>().text;
		}

		public void SetText(string text)
		{
			bubbleObject.GetComponent<Text>().text = text;
		}

		public void Destroy()
		{
			Object.Destroy(bubbleObject);
		}
	}
}
