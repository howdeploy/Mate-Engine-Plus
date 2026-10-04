using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerAudio : MonoBehaviour
{
	public Slider petVolumeSlider;

	public Slider effectsVolumeSlider;

	public Slider menuVolumeSlider;

	public List<AudioSource> petAudioSources = new List<AudioSource>();

	public List<AudioSource> effectsAudioSources = new List<AudioSource>();

	public List<AudioSource> menuAudioSources = new List<AudioSource>();

	private Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();

	private void Start()
	{
		SetupListeners();
		LoadSettings();
		UpdateAllCategoryVolumes();
	}

	public void SetupListeners()
	{
		petVolumeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.petVolume = v;
			UpdateAllCategoryVolumes();
			Save();
		});
		effectsVolumeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.effectsVolume = v;
			UpdateAllCategoryVolumes();
			Save();
		});
		menuVolumeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.menuVolume = v;
			UpdateAllCategoryVolumes();
			Save();
		});
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		petVolumeSlider?.SetValueWithoutNotify(data.petVolume);
		effectsVolumeSlider?.SetValueWithoutNotify(data.effectsVolume);
		menuVolumeSlider?.SetValueWithoutNotify(data.menuVolume);
		UpdateAllCategoryVolumes();
	}

	public void ApplySettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.petVolume = petVolumeSlider?.value ?? data.petVolume;
		data.effectsVolume = effectsVolumeSlider?.value ?? data.effectsVolume;
		data.menuVolume = menuVolumeSlider?.value ?? data.menuVolume;
		UpdateAllCategoryVolumes();
	}

	public void ResetToDefaults()
	{
		petVolumeSlider?.SetValueWithoutNotify(1f);
		effectsVolumeSlider?.SetValueWithoutNotify(1f);
		menuVolumeSlider?.SetValueWithoutNotify(1f);
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.petVolume = 1f;
		data.effectsVolume = 1f;
		data.menuVolume = 1f;
		UpdateAllCategoryVolumes();
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void UpdateAllCategoryVolumes()
	{
		float num = petVolumeSlider?.value ?? 1f;
		float num2 = effectsVolumeSlider?.value ?? 1f;
		float num3 = menuVolumeSlider?.value ?? 1f;
		foreach (AudioSource petAudioSource in petAudioSources)
		{
			if (petAudioSource != null)
			{
				petAudioSource.volume = GetBaseVolume(petAudioSource) * num;
			}
		}
		foreach (AudioSource effectsAudioSource in effectsAudioSources)
		{
			if (effectsAudioSource != null)
			{
				effectsAudioSource.volume = GetBaseVolume(effectsAudioSource) * num2;
			}
		}
		foreach (AudioSource menuAudioSource in menuAudioSources)
		{
			if (menuAudioSource != null)
			{
				menuAudioSource.volume = GetBaseVolume(menuAudioSource) * num3;
			}
		}
	}

	private float GetBaseVolume(AudioSource src)
	{
		if (src == null)
		{
			return 1f;
		}
		if (!baseVolumes.TryGetValue(src, out var value))
		{
			value = src.volume;
			baseVolumes[src] = value;
		}
		return value;
	}

	private void Save()
	{
		SaveLoadHandler.Instance.SaveToDisk();
	}
}
