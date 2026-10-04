using System.Collections.Generic;
using System.Collections;
using System.Linq;
using PulseAudio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AllowedAppsManager : MonoBehaviour
{
	public TMP_Dropdown runningAppsDropdown;

	public Button addToAllowedListButton;

	public Transform allowedAppsListContent;

	public GameObject allowedAppItemPrefab;

	private Coroutine refreshCoroutine;

	private List<string> currentRunningAppNames = new List<string>();

	private List<string> allowedApps => SaveLoadHandler.Instance.data.allowedApps;

	private void Start()
	{
		addToAllowedListButton.onClick.AddListener(delegate
		{
			if (runningAppsDropdown.options.Count != 0)
			{
				string text = runningAppsDropdown.options[runningAppsDropdown.value].text;
				if (!allowedApps.Contains(text))
				{
					allowedApps.Add(text);
					UpdateAllowedListUI();
					RefreshRunningAppsDropdown();
					SaveLoadHandler.Instance.SaveToDisk();
					SaveLoadHandler.SyncAllowedAppsToAllAvatars();
				}
			}
		});
		RefreshRunningAppsDropdown();
		UpdateAllowedListUI();
		SaveLoadHandler.SyncAllowedAppsToAllAvatars();
	}

	private void RefreshRunningAppsDropdown()
	{
		if (refreshCoroutine == null) refreshCoroutine = StartCoroutine(QueryRunningApps());
	}

	private IEnumerator QueryRunningApps()
	{
		yield return null;
		while (PulseAudioManager.Instance == null || !PulseAudioManager.Instance.allSet || PulseAudioManager.Instance.callbackRunning) yield return null;
		List<AudioProgram> programs = null;
		PulseAudioManager.Instance.GetPlayingAudioPrograms(result => programs = result);
		while (programs == null) yield return null;
		currentRunningAppNames = programs.Select(app => string.IsNullOrEmpty(app.ProcessName) ? app.Name : app.ProcessName)
			.Where(name => !string.IsNullOrEmpty(name)).Select(name => name.ToLowerInvariant()).Distinct().ToList();
		UpdateRunningAppsDropdown();
		refreshCoroutine = null;
	}

	private void UpdateRunningAppsDropdown()
	{
		List<string> list = (from app in currentRunningAppNames
			where !allowedApps.Contains(app)
			orderby app
			select app).ToList();
		runningAppsDropdown.ClearOptions();
		runningAppsDropdown.AddOptions(list.Select((string app) => new TMP_Dropdown.OptionData(app)).ToList());
		if (list.Count == 0)
		{
			runningAppsDropdown.value = 0;
		}
	}

	public void OnDropdownOpened()
	{
		RefreshRunningAppsDropdown();
	}

	private void UpdateAllowedListUI()
	{
		foreach (Transform item2 in allowedAppsListContent)
		{
			Object.Destroy(item2.gameObject);
		}
		foreach (string app in allowedApps)
		{
			GameObject item = Object.Instantiate(allowedAppItemPrefab, allowedAppsListContent);
			TextMeshProUGUI textMeshProUGUI = item.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault((TextMeshProUGUI t) => t.transform.parent == item.transform);
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.text = app;
			}
			Button button = item.transform.Find("Button")?.GetComponent<Button>();
			if (button != null)
			{
				button.onClick.AddListener(delegate
				{
					allowedApps.Remove(app);
					UpdateAllowedListUI();
					SaveLoadHandler.Instance.SaveToDisk();
					SaveLoadHandler.SyncAllowedAppsToAllAvatars();
				});
			}
		}
	}

	private void OnDisable()
	{
		if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
		refreshCoroutine = null;
	}

	public void RefreshAppListOnMenuOpen()
	{
		RefreshRunningAppsDropdown();
		UpdateAllowedListUI();
		SaveLoadHandler.SyncAllowedAppsToAllAvatars();
	}

	public void RefreshUI()
	{
		RefreshRunningAppsDropdown();
		UpdateAllowedListUI();
		SaveLoadHandler.SyncAllowedAppsToAllAvatars();
	}
}
