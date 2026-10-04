using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerLights : MonoBehaviour
{
	[Serializable]
	public class LightControlEntry
	{
		public string lightID;

		public Slider intensitySlider;

		public Slider saturationSlider;

		public Slider hueSlider;

		public float defaultIntensity;

		public float defaultSaturation;

		public float defaultHue;
	}

	[Serializable]
	public class LightToggleEntry
	{
		public string activeID;

		public string nonActiveID;

		public Toggle checkmark;
	}

	public List<LightControlEntry> lights = new List<LightControlEntry>();

	public List<LightToggleEntry> lightToggles = new List<LightToggleEntry>();

	public ColorController colorController;

	private void Start()
	{
		for (int i = 0; i < lights.Count; i++)
		{
			int idx = i;
			LightControlEntry entry = lights[i];
			entry.defaultIntensity = entry.intensitySlider.value;
			entry.defaultSaturation = entry.saturationSlider.value;
			entry.defaultHue = entry.hueSlider.value;
			entry.intensitySlider.onValueChanged.AddListener(delegate(float v)
			{
				SaveLoadHandler.Instance.data.lightIntensities[entry.lightID] = v;
				OnLightSliderChanged(idx);
				Save();
			});
			entry.saturationSlider.onValueChanged.AddListener(delegate(float v)
			{
				SaveLoadHandler.Instance.data.lightSaturations[entry.lightID] = v;
				OnLightSliderChanged(idx);
				Save();
			});
			entry.hueSlider.onValueChanged.AddListener(delegate(float v)
			{
				SaveLoadHandler.Instance.data.lightHues[entry.lightID] = v;
				OnLightSliderChanged(idx);
				Save();
			});
		}
		for (int num = 0; num < lightToggles.Count; num++)
		{
			int idx2 = num;
			LightToggleEntry entry2 = lightToggles[num];
			if (!(entry2.checkmark != null))
			{
				continue;
			}
			entry2.checkmark.onValueChanged.AddListener(delegate(bool v)
			{
				if (!string.IsNullOrEmpty(entry2.activeID))
				{
					SaveLoadHandler.Instance.data.groupToggles[entry2.activeID] = v;
				}
				OnLightToggleChanged(idx2, v);
				Save();
			});
		}
		LoadSettings();
		ApplySettings();
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		for (int i = 0; i < lights.Count; i++)
		{
			LightControlEntry lightControlEntry = lights[i];
			if (!string.IsNullOrEmpty(lightControlEntry.lightID))
			{
				if (data.lightIntensities.TryGetValue(lightControlEntry.lightID, out var value))
				{
					lightControlEntry.intensitySlider.SetValueWithoutNotify(value);
				}
				if (data.lightSaturations.TryGetValue(lightControlEntry.lightID, out var value2))
				{
					lightControlEntry.saturationSlider.SetValueWithoutNotify(value2);
				}
				if (data.lightHues.TryGetValue(lightControlEntry.lightID, out var value3))
				{
					lightControlEntry.hueSlider.SetValueWithoutNotify(value3);
				}
			}
			OnLightSliderChanged(i);
		}
		for (int j = 0; j < lightToggles.Count; j++)
		{
			LightToggleEntry lightToggleEntry = lightToggles[j];
			if (!string.IsNullOrEmpty(lightToggleEntry.activeID) && lightToggleEntry.checkmark != null)
			{
				bool flag = false;
				if (data.groupToggles.TryGetValue(lightToggleEntry.activeID, out var value4))
				{
					flag = value4;
				}
				lightToggleEntry.checkmark.SetIsOnWithoutNotify(flag);
				OnLightToggleChanged(j, flag);
			}
		}
	}

	public void ApplySettings()
	{
		for (int i = 0; i < lights.Count; i++)
		{
			OnLightSliderChanged(i);
		}
		for (int j = 0; j < lightToggles.Count; j++)
		{
			LightToggleEntry lightToggleEntry = lightToggles[j];
			OnLightToggleChanged(j, lightToggleEntry.checkmark != null && lightToggleEntry.checkmark.isOn);
		}
	}

	public void ResetLightToDefault(int idx)
	{
		LightControlEntry lightControlEntry = lights[idx];
		lightControlEntry.intensitySlider.value = lightControlEntry.defaultIntensity;
		lightControlEntry.saturationSlider.value = lightControlEntry.defaultSaturation;
		lightControlEntry.hueSlider.value = lightControlEntry.defaultHue;
		OnLightSliderChanged(idx);
	}

	public void ResetAllLightsToDefault()
	{
		for (int i = 0; i < lights.Count; i++)
		{
			LightControlEntry lightControlEntry = lights[i];
			lightControlEntry.intensitySlider.value = lightControlEntry.defaultIntensity;
			lightControlEntry.saturationSlider.value = lightControlEntry.defaultSaturation;
			lightControlEntry.hueSlider.value = lightControlEntry.defaultHue;
			if (!string.IsNullOrEmpty(lightControlEntry.lightID))
			{
				SaveLoadHandler.Instance.data.lightIntensities[lightControlEntry.lightID] = lightControlEntry.defaultIntensity;
				SaveLoadHandler.Instance.data.lightSaturations[lightControlEntry.lightID] = lightControlEntry.defaultSaturation;
				SaveLoadHandler.Instance.data.lightHues[lightControlEntry.lightID] = lightControlEntry.defaultHue;
			}
		}
		SaveLoadHandler.Instance.SaveToDisk();
	}

	public void ResetAllLightTogglesToDefault()
	{
		for (int i = 0; i < lightToggles.Count; i++)
		{
			LightToggleEntry lightToggleEntry = lightToggles[i];
			if (lightToggleEntry.checkmark != null)
			{
				lightToggleEntry.checkmark.SetIsOnWithoutNotify(value: false);
				OnLightToggleChanged(i, state: false);
			}
			if (!string.IsNullOrEmpty(lightToggleEntry.activeID))
			{
				SaveLoadHandler.Instance.data.groupToggles[lightToggleEntry.activeID] = false;
			}
		}
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void OnLightSliderChanged(int idx)
	{
		LightControlEntry entry = lights[idx];
		if (!(colorController == null))
		{
			ColorController.ColorTarget colorTarget = colorController.targets.Find((ColorController.ColorTarget t) => t.id == entry.lightID);
			if (colorTarget != null)
			{
				colorTarget.intensity = entry.intensitySlider.value;
				colorTarget.saturation = entry.saturationSlider.value;
				colorTarget.hue = entry.hueSlider.value;
			}
		}
	}

	private void OnLightToggleChanged(int idx, bool state)
	{
		LightToggleEntry lightToggleEntry = lightToggles[idx];
		if (!(colorController == null))
		{
			colorController.SetGroupEnabled(lightToggleEntry.activeID, state);
			colorController.SetGroupEnabled(lightToggleEntry.nonActiveID, !state);
		}
	}

	private void Save()
	{
		SaveLoadHandler.Instance.SaveToDisk();
	}
}
