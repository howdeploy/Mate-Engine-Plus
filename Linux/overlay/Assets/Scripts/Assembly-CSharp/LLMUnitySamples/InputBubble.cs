using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	internal class InputBubble : Bubble
	{
		protected GameObject inputFieldObject;

		protected InputField inputField;

		protected GameObject placeholderObject;

		public InputBubble(Transform parent, BubbleUI ui, string name, string message, int lineHeight = 4)
			: base(parent, ui, name, emptyLines(message, lineHeight))
		{
			Text component = bubbleObject.GetComponent<Text>();
			RectTransform component2 = bubbleObject.GetComponent<RectTransform>();
			bubbleObject.GetComponent<ContentSizeFitter>().enabled = false;
			placeholderObject = CreatePlaceholderObject(bubbleObject.transform, component2, component.text);
			inputFieldObject = CreateInputFieldObject(bubbleObject.transform, component, placeholderObject.GetComponent<Text>());
			inputField = inputFieldObject.GetComponent<InputField>();
			Canvas component3 = bubbleObject.GetComponent<Canvas>();
			if (component3 != null)
			{
				component3.sortingOrder = 2;
			}
			Canvas component4 = imageObject.GetComponent<Canvas>();
			if (component4 != null)
			{
				component4.sortingOrder = 2;
			}
		}

		private static string emptyLines(string message, int lineHeight)
		{
			string text = message;
			for (int i = 0; i < lineHeight - 1; i++)
			{
				text += "\n";
			}
			return text;
		}

		private GameObject CreatePlaceholderObject(Transform parent, RectTransform textRectTransform, string message)
		{
			GameObject gameObject = CreateTextObject(parent, "Placeholder", message, horizontalStretch: false);
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.sizeDelta = textRectTransform.sizeDelta;
			component.anchoredPosition = textRectTransform.anchoredPosition;
			component.localScale = Vector3.one;
			SyncParentRectTransform(component);
			return gameObject;
		}

		private GameObject CreateInputFieldObject(Transform parent, Text textObject, Text placeholderTextObject)
		{
			GameObject gameObject = new GameObject("InputField", typeof(RectTransform), typeof(InputField), typeof(Canvas));
			gameObject.transform.SetParent(parent);
			inputField = gameObject.GetComponent<InputField>();
			inputField.textComponent = textObject;
			inputField.placeholder = placeholderTextObject;
			inputField.interactable = true;
			inputField.lineType = InputField.LineType.MultiLineSubmit;
			inputField.shouldHideMobileInput = false;
			inputField.shouldActivateOnSelect = true;
			RectTransform component = gameObject.GetComponent<RectTransform>();
			component.localScale = Vector3.one;
			SyncParentRectTransform(component);
			return gameObject;
		}

		public void FixCaretSorting()
		{
			GameObject gameObject = GameObject.Find(inputField.name + " Input Caret");
			if (gameObject.GetComponent<Canvas>() == null)
			{
				Canvas canvas = gameObject.AddComponent<Canvas>();
				canvas.overrideSorting = true;
				canvas.sortingOrder = 3;
			}
		}

		public void AddSubmitListener(UnityAction<string> onInputFieldSubmit)
		{
			inputField.onSubmit.AddListener(onInputFieldSubmit);
		}

		public void AddValueChangedListener(UnityAction<string> onValueChanged)
		{
			inputField.onValueChanged.AddListener(onValueChanged);
		}

		public new string GetText()
		{
			return inputField.text;
		}

		public new void SetText(string text)
		{
			inputField.text = text;
			MoveTextEnd();
		}

		public void SetPlaceHolderText(string text)
		{
			placeholderObject.GetComponent<Text>().text = text;
		}

		public bool inputFocused()
		{
			return inputField.isFocused;
		}

		public void MoveTextEnd()
		{
			inputField.MoveTextEnd(shift: true);
		}

		public void setInteractable(bool interactable)
		{
			inputField.interactable = interactable;
		}

		public void SetSelectionColorAlpha(float alpha)
		{
			Color selectionColor = inputField.selectionColor;
			selectionColor.a = alpha;
			inputField.selectionColor = selectionColor;
		}

		public void ActivateInputField()
		{
			inputField.ActivateInputField();
			FixCaretSorting();
		}

		public void ReActivateInputField()
		{
			inputField.DeactivateInputField();
			inputField.Select();
			inputField.ActivateInputField();
		}
	}
}
