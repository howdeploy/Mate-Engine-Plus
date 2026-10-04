using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlendshapeUIBlock : MonoBehaviour
{
	[Header("9 Slots (Label + Slider)")]
	public TMP_Text[] labels = new TMP_Text[9];

	public Slider[] sliders = new Slider[9];

	public TMP_Text[] valueTexts = new TMP_Text[9];

	private GameObject[] slotRoots = new GameObject[9];

	private void Awake()
	{
		for (int i = 0; i < 9; i++)
		{
			if (sliders[i] != null)
			{
				slotRoots[i] = ((sliders[i].transform.parent != null) ? sliders[i].transform.parent.gameObject : sliders[i].gameObject);
			}
			else if (labels[i] != null)
			{
				slotRoots[i] = ((labels[i].transform.parent != null) ? labels[i].transform.parent.gameObject : labels[i].gameObject);
			}
			else
			{
				slotRoots[i] = null;
			}
		}
	}

	public void SetSlotActive(int index, bool active)
	{
		if (index >= 0 && index < 9 && slotRoots[index] != null)
		{
			slotRoots[index].SetActive(active);
		}
	}

	public void SetupSlot(int index, string displayName, float initialValue, Action<float> onChanged)
	{
		if (index < 0 || index >= 9)
		{
			return;
		}
		SetSlotActive(index, active: true);
		if (labels[index] != null)
		{
			labels[index].text = displayName;
		}
		if (sliders[index] != null)
		{
			sliders[index].minValue = 0f;
			sliders[index].maxValue = 100f;
			sliders[index].wholeNumbers = true;
			sliders[index].SetValueWithoutNotify(initialValue);
			sliders[index].onValueChanged.RemoveAllListeners();
			sliders[index].onValueChanged.AddListener(delegate(float v)
			{
				UpdateValueText(index, v);
				onChanged?.Invoke(v);
			});
			UpdateValueText(index, initialValue);
		}
	}

	public void ClearUnusedFrom(int startIndex)
	{
		for (int i = startIndex; i < 9; i++)
		{
			SetSlotActive(i, active: false);
		}
	}

	private void UpdateValueText(int index, float value)
	{
		if (index < 0 || index >= 9)
		{
			return;
		}
		if (valueTexts != null && valueTexts.Length == 9 && valueTexts[index] != null)
		{
			valueTexts[index].text = ((int)value).ToString();
		}
		else if (labels[index] != null)
		{
			string text = labels[index].text;
			int num = text.LastIndexOf(" (");
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			labels[index].text = $"{text} ({(int)value})";
		}
	}
}
