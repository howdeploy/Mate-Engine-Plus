using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageDropdownHandler : MonoBehaviour
{
	[Tooltip("Add all TMP_Dropdowns that should reflect the selected language")]
	[SerializeField]
	private List<TMP_Dropdown> languageDropdowns = new List<TMP_Dropdown>();

	private bool isInitializing = true;

	private IEnumerator Start()
	{
		yield return LocalizationSettings.InitializationOperation;
		List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
		if (locales.Count == 0)
		{
			Debug.LogError("No localization locales were loaded.");
			yield break;
		}
		string savedCode = SaveLoadHandler.Instance.data.selectedLocaleCode;
		int num = locales.FindIndex((Locale locale) => locale.Identifier.Code == savedCode);
		if (num < 0)
		{
			num = 0;
		}
		foreach (TMP_Dropdown languageDropdown in languageDropdowns)
		{
			if (languageDropdown != null)
			{
				languageDropdown.SetValueWithoutNotify(num);
				languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
			}
		}
		LocalizationSettings.SelectedLocale = locales[num];
		isInitializing = false;
	}

	private void OnLanguageChanged(int index)
	{
		if (isInitializing)
		{
			return;
		}
		List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
		if (index < 0 || index >= locales.Count)
		{
			return;
		}
		Locale locale = (LocalizationSettings.SelectedLocale = locales[index]);
		foreach (TMP_Dropdown languageDropdown in languageDropdowns)
		{
			if (languageDropdown != null && languageDropdown.value != index)
			{
				languageDropdown.SetValueWithoutNotify(index);
			}
		}
		SaveLoadHandler.Instance.data.selectedLocaleCode = locale.Identifier.Code;
		SaveLoadHandler.Instance.SaveToDisk();
	}
}
