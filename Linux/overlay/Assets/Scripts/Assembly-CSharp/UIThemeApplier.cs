using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIThemeApplier : MonoBehaviour
{
	[Header("========== BACKGROUND ==========")]
	public Color backgroundPanelColor = new Color(0.2f, 0.1f, 0.3f, 1f);

	public GameObject menuPanel;

	[Header("========== TITLE TEXT ==========")]
	public Color titleTextColor = Color.white;

	public GameObject titleTextObject;

	[Header("========== SLIDER COLORS ==========")]
	public Color sliderNormalColor = Color.magenta;

	public Color sliderHighlightedColor = Color.white;

	public Color sliderPressedColor = Color.white;

	public Color sliderSelectedColor = Color.white;

	public Color sliderDisabledColor = Color.gray;

	public Color sliderLabelColor = Color.white;

	public Color sliderBackgroundColor = new Color(0.3f, 0.2f, 0.4f, 1f);

	public Color sliderFillColor = new Color(1f, 0.5f, 1f, 1f);

	[Header("========== TOGGLE COLORS ==========")]
	public Color toggleNormalColor = Color.magenta;

	public Color toggleHighlightedColor = Color.white;

	public Color togglePressedColor = Color.white;

	public Color toggleSelectedColor = Color.white;

	public Color toggleDisabledColor = Color.gray;

	public Color toggleLabelColor = Color.white;

	public Color toggleBackgroundColor = new Color(0.3f, 0.2f, 0.4f, 1f);

	[Header("========== BUTTON COLORS ==========")]
	public Color buttonNormalColor = Color.magenta;

	public Color buttonHighlightedColor = Color.white;

	public Color buttonPressedColor = Color.white;

	public Color buttonSelectedColor = Color.white;

	public Color buttonDisabledColor = Color.gray;

	public Color buttonTextColor = Color.white;

	[Header("========== SCROLLBAR COLORS ==========")]
	public Color scrollbarNormalColor = Color.magenta;

	public Color scrollbarHighlightedColor = Color.white;

	public Color scrollbarPressedColor = Color.white;

	public Color scrollbarSelectedColor = Color.white;

	public Color scrollbarDisabledColor = Color.gray;

	public Color scrollbarHandleColor = new Color(0.8f, 0.6f, 1f, 1f);

	public Color scrollbarBackgroundColor = new Color(0.3f, 0.2f, 0.4f, 1f);

	[Header("========== DROPDOWN COLORS ==========")]
	public Color dropdownNormalColor = Color.magenta;

	public Color dropdownHighlightedColor = Color.white;

	public Color dropdownPressedColor = Color.white;

	public Color dropdownSelectedColor = Color.white;

	public Color dropdownDisabledColor = Color.gray;

	public Color dropdownBackgroundColor = new Color(0.3f, 0.2f, 0.4f, 1f);

	public Color dropdownTextColor = Color.white;

	[ContextMenu("Apply Theme Colors")]
	public void ApplyTheme()
	{
		if (menuPanel == null)
		{
			Debug.LogError("Menu Panel is not assigned.");
			return;
		}
		Image component = menuPanel.GetComponent<Image>();
		if (component != null)
		{
			component.color = backgroundPanelColor;
		}
		if (titleTextObject != null)
		{
			TextMeshProUGUI component2 = titleTextObject.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.color = titleTextColor;
			}
		}
		Slider[] componentsInChildren = menuPanel.GetComponentsInChildren<Slider>(includeInactive: true);
		foreach (Slider obj in componentsInChildren)
		{
			ColorBlock colors = obj.colors;
			colors.normalColor = sliderNormalColor;
			colors.highlightedColor = sliderHighlightedColor;
			colors.pressedColor = sliderPressedColor;
			colors.selectedColor = sliderSelectedColor;
			colors.disabledColor = sliderDisabledColor;
			obj.colors = colors;
			TextMeshProUGUI componentInChildren = obj.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren != null)
			{
				componentInChildren.color = sliderLabelColor;
			}
			Image image = obj.transform.Find("Background")?.GetComponent<Image>();
			if (image != null)
			{
				image.color = sliderBackgroundColor;
			}
			Image image2 = obj.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
			if (image2 != null)
			{
				image2.color = sliderFillColor;
			}
		}
		Toggle[] componentsInChildren2 = menuPanel.GetComponentsInChildren<Toggle>(includeInactive: true);
		foreach (Toggle obj2 in componentsInChildren2)
		{
			ColorBlock colors2 = obj2.colors;
			colors2.normalColor = toggleNormalColor;
			colors2.highlightedColor = toggleHighlightedColor;
			colors2.pressedColor = togglePressedColor;
			colors2.selectedColor = toggleSelectedColor;
			colors2.disabledColor = toggleDisabledColor;
			obj2.colors = colors2;
			TextMeshProUGUI componentInChildren2 = obj2.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.color = toggleLabelColor;
			}
			Image componentInChildren3 = obj2.GetComponentInChildren<Image>();
			if (componentInChildren3 != null)
			{
				componentInChildren3.color = toggleBackgroundColor;
			}
		}
		Button[] componentsInChildren3 = menuPanel.GetComponentsInChildren<Button>(includeInactive: true);
		foreach (Button obj3 in componentsInChildren3)
		{
			ColorBlock colors3 = obj3.colors;
			colors3.normalColor = buttonNormalColor;
			colors3.highlightedColor = buttonHighlightedColor;
			colors3.pressedColor = buttonPressedColor;
			colors3.selectedColor = buttonSelectedColor;
			colors3.disabledColor = buttonDisabledColor;
			obj3.colors = colors3;
			TextMeshProUGUI componentInChildren4 = obj3.GetComponentInChildren<TextMeshProUGUI>();
			if (componentInChildren4 != null)
			{
				componentInChildren4.color = buttonTextColor;
			}
		}
		Scrollbar[] componentsInChildren4 = menuPanel.GetComponentsInChildren<Scrollbar>(includeInactive: true);
		foreach (Scrollbar obj4 in componentsInChildren4)
		{
			ColorBlock colors4 = obj4.colors;
			colors4.normalColor = scrollbarNormalColor;
			colors4.highlightedColor = scrollbarHighlightedColor;
			colors4.pressedColor = scrollbarPressedColor;
			colors4.selectedColor = scrollbarSelectedColor;
			colors4.disabledColor = scrollbarDisabledColor;
			obj4.colors = colors4;
			Image image3 = obj4.transform.Find("Sliding Area/Handle")?.GetComponent<Image>();
			if (image3 != null)
			{
				image3.color = scrollbarHandleColor;
			}
			Image component3 = obj4.GetComponent<Image>();
			if (component3 != null)
			{
				component3.color = scrollbarBackgroundColor;
			}
		}
		TMP_Dropdown[] componentsInChildren5 = menuPanel.GetComponentsInChildren<TMP_Dropdown>(includeInactive: true);
		foreach (TMP_Dropdown obj5 in componentsInChildren5)
		{
			ColorBlock colors5 = obj5.colors;
			colors5.normalColor = dropdownNormalColor;
			colors5.highlightedColor = dropdownHighlightedColor;
			colors5.pressedColor = dropdownPressedColor;
			colors5.selectedColor = dropdownSelectedColor;
			colors5.disabledColor = dropdownDisabledColor;
			obj5.colors = colors5;
			TextMeshProUGUI textMeshProUGUI = obj5.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.color = dropdownTextColor;
			}
			Image component4 = obj5.GetComponent<Image>();
			if (component4 != null)
			{
				component4.color = dropdownBackgroundColor;
			}
			Image image4 = obj5.transform.Find("Arrow")?.GetComponent<Image>();
			if (image4 != null)
			{
				image4.color = dropdownTextColor;
			}
		}
		Debug.Log("✔ All UI theme colors applied!");
	}
}
