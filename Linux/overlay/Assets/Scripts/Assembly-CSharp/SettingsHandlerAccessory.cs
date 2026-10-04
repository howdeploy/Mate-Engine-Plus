using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerAccessory : MonoBehaviour
{
	[Serializable]
	public class AccessoryToggleEntry
	{
		public string ruleName;

		public Toggle toggle;
	}

	public List<AccessoryToggleEntry> accessoryToggleBindings = new List<AccessoryToggleEntry>();

	private void Start()
	{
		SetupListeners();
		LoadSettings();
	}

	public void SetupListeners()
	{
		foreach (AccessoryToggleEntry accessoryToggleBinding in accessoryToggleBindings)
		{
			if (!string.IsNullOrEmpty(accessoryToggleBinding.ruleName) && accessoryToggleBinding.toggle != null)
			{
				string key = accessoryToggleBinding.ruleName;
				accessoryToggleBinding.toggle.onValueChanged.AddListener(delegate(bool v)
				{
					SaveLoadHandler.Instance.data.accessoryStates[key] = v;
					UpdateAccessoryObjects();
					ForceRefreshSceneObjects();
					SaveLoadHandler.Instance.SaveToDisk();
				});
			}
		}
	}

	public void LoadSettings()
	{
		foreach (AccessoryToggleEntry accessoryToggleBinding in accessoryToggleBindings)
		{
			if (!string.IsNullOrEmpty(accessoryToggleBinding.ruleName) && accessoryToggleBinding.toggle != null)
			{
				bool value = false;
				SaveLoadHandler.Instance.data.accessoryStates.TryGetValue(accessoryToggleBinding.ruleName, out value);
				accessoryToggleBinding.toggle.SetIsOnWithoutNotify(value);
			}
		}
		UpdateAccessoryObjects();
		ForceRefreshSceneObjects();
	}

	public void ApplySettings()
	{
		UpdateAccessoryObjects();
		ForceRefreshSceneObjects();
	}

	public void ResetToDefaults()
	{
		foreach (AccessoryToggleEntry accessoryToggleBinding in accessoryToggleBindings)
		{
			if (!string.IsNullOrEmpty(accessoryToggleBinding.ruleName))
			{
				SaveLoadHandler.Instance.data.accessoryStates[accessoryToggleBinding.ruleName] = false;
				if (accessoryToggleBinding.toggle != null)
				{
					accessoryToggleBinding.toggle.SetIsOnWithoutNotify(value: false);
				}
			}
		}
		UpdateAccessoryObjects();
		ForceRefreshSceneObjects();
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void UpdateAccessoryObjects()
	{
		foreach (AccessoryToggleEntry accessoryToggleBinding in accessoryToggleBindings)
		{
			bool isEnabled = accessoryToggleBinding.toggle != null && accessoryToggleBinding.toggle.isOn;
			foreach (AccessoiresHandler activeHandler in AccessoiresHandler.ActiveHandlers)
			{
				foreach (AccessoiresHandler.AccessoryRule rule in activeHandler.rules)
				{
					if (rule.ruleName == accessoryToggleBinding.ruleName)
					{
						rule.isEnabled = isEnabled;
					}
				}
			}
		}
	}

	private void ForceRefreshSceneObjects()
	{
		foreach (AccessoiresHandler activeHandler in AccessoiresHandler.ActiveHandlers)
		{
			foreach (AccessoiresHandler.AccessoryRule rule in activeHandler.rules)
			{
				if (rule.linkedObject != null)
				{
					rule.linkedObject.SetActive(rule.isEnabled);
				}
			}
		}
	}
}
