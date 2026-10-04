using System;
using System.Collections.Generic;
using LLMUnity;
using TMPro;
using UnityEngine;

public class SettingsHandlerDropdowns : MonoBehaviour
{
	[Serializable]
	public class ParticleThemeEntry
	{
		public string id = "Standard";

		public string display = "Standard";
	}

	public TMP_Dropdown graphicsDropdown;

	public TMP_Dropdown contextLengthDropdown;

	[Header("Particle Theme")]
	public TMP_Dropdown particleDropdown;

	public List<ParticleThemeEntry> particleThemes = new List<ParticleThemeEntry>();

	public LLM llm;

	private readonly int[] contextOptions = new int[5] { 2048, 4096, 8192, 16384, 32768 };

	private void Start()
	{
		if (graphicsDropdown != null)
		{
			graphicsDropdown.ClearOptions();
			graphicsDropdown.AddOptions(new List<string> { "ULTRA", "VERY HIGH", "HIGH", "NORMAL", "LOW" });
			graphicsDropdown.onValueChanged.AddListener(OnGraphicsChanged);
		}
		if (contextLengthDropdown != null)
		{
			contextLengthDropdown.ClearOptions();
			List<string> list = new List<string>();
			int[] array = contextOptions;
			foreach (int num in array)
			{
				list.Add($"{num / 1024}K");
			}
			contextLengthDropdown.AddOptions(list);
			contextLengthDropdown.onValueChanged.AddListener(OnContextChanged);
		}
		if (particleDropdown != null)
		{
			BuildParticleDropdown();
			particleDropdown.onValueChanged.AddListener(OnParticleChanged);
		}
		LoadSettings();
		ApplySettings();
	}

	private void BuildParticleDropdown()
	{
		if (!(particleDropdown == null))
		{
			if (particleThemes == null)
			{
				particleThemes = new List<ParticleThemeEntry>();
			}
			if (particleThemes.Count == 0)
			{
				particleThemes.Add(new ParticleThemeEntry
				{
					id = "Standard",
					display = "Standard"
				});
			}
			List<string> list = new List<string>();
			for (int i = 0; i < particleThemes.Count; i++)
			{
				string item = (string.IsNullOrWhiteSpace(particleThemes[i].display) ? particleThemes[i].id : particleThemes[i].display);
				list.Add(item);
			}
			particleDropdown.ClearOptions();
			particleDropdown.AddOptions(list);
			string sel = SaveLoadHandler.Instance.data.selectedParticleTheme;
			int valueWithoutNotify = Mathf.Max(0, particleThemes.FindIndex((ParticleThemeEntry e) => e.id == sel));
			particleDropdown.SetValueWithoutNotify(valueWithoutNotify);
		}
	}

	private void OnParticleChanged(int index)
	{
		if (particleThemes != null && index >= 0 && index < particleThemes.Count)
		{
			SaveLoadHandler.Instance.data.selectedParticleTheme = particleThemes[index].id;
			SaveLoadHandler.ApplyAllSettingsToAllAvatars();
			SaveLoadHandler.Instance.SaveToDisk();
		}
	}

	private void OnGraphicsChanged(int index)
	{
		SaveLoadHandler.Instance.data.graphicsQualityLevel = index;
		QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void OnContextChanged(int index)
	{
		if (llm != null)
		{
			llm.contextSize = contextOptions[index];
		}
		SaveLoadHandler.Instance.data.contextLength = contextOptions[index];
		SaveLoadHandler.Instance.SaveToDisk();
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		graphicsDropdown?.SetValueWithoutNotify(data.graphicsQualityLevel);
		QualitySettings.SetQualityLevel(data.graphicsQualityLevel, applyExpensiveChanges: true);
		int value = ((data.contextLength > 0) ? data.contextLength : 4096);
		int num = Array.IndexOf(contextOptions, value);
		if (num < 0)
		{
			num = 1;
		}
		contextLengthDropdown?.SetValueWithoutNotify(num);
		if (llm != null)
		{
			llm.contextSize = contextOptions[num];
		}
		if (particleDropdown != null)
		{
			BuildParticleDropdown();
		}
	}

	public void ApplySettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.graphicsQualityLevel = graphicsDropdown?.value ?? data.graphicsQualityLevel;
		QualitySettings.SetQualityLevel(data.graphicsQualityLevel, applyExpensiveChanges: true);
		if (contextLengthDropdown != null)
		{
			int value = contextLengthDropdown.value;
			data.contextLength = contextOptions[value];
			if (llm != null)
			{
				llm.contextSize = data.contextLength;
			}
		}
		if (particleDropdown != null)
		{
			int index = Mathf.Clamp(particleDropdown.value, 0, particleThemes.Count - 1);
			if (particleThemes.Count > 0)
			{
				data.selectedParticleTheme = particleThemes[index].id;
			}
		}
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
		SaveLoadHandler.Instance.SaveToDisk();
	}

	public void ResetToDefaults()
	{
		graphicsDropdown?.SetValueWithoutNotify(1);
		QualitySettings.SetQualityLevel(1, applyExpensiveChanges: true);
		SaveLoadHandler.Instance.data.graphicsQualityLevel = 1;
		int num = 1;
		contextLengthDropdown?.SetValueWithoutNotify(num);
		SaveLoadHandler.Instance.data.contextLength = contextOptions[num];
		if (llm != null)
		{
			llm.contextSize = contextOptions[num];
		}
		SaveLoadHandler.Instance.data.selectedParticleTheme = "Standard";
		if (particleDropdown != null)
		{
			BuildParticleDropdown();
		}
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
		SaveLoadHandler.Instance.SaveToDisk();
	}
}
