using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerSliders : MonoBehaviour
{
	public Slider soundThresholdSlider;

	public Slider idleSwitchTimeSlider;

	public Slider idleTransitionTimeSlider;

	public Slider avatarSizeSlider;

	public Slider fpsLimitSlider;

	public Slider headBlendSlider;

	public Slider spineBlendSlider;

	public Slider eyeBlendSlider;

	public Slider hueShiftSlider;

	public Slider saturationSlider;

	public Slider windowSitYOffsetSlider;

	public Slider danceSwitchTimeSlider;

	public Slider danceTransitionTimeSlider;

	private void Start()
	{
		soundThresholdSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.soundThreshold = v;
			SaveAll();
		});
		idleSwitchTimeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.idleSwitchTime = v;
			SaveAll();
		});
		idleTransitionTimeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.idleTransitionTime = v;
			SaveAll();
		});
		avatarSizeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.avatarSize = v;
			SaveAll();
		});
		fpsLimitSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.fpsLimit = (int)v;
			FPSLimiter[] array = Object.FindObjectsByType<FPSLimiter>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetFPSLimit((int)v);
			}
			SaveAll();
		});
		headBlendSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.headBlend = v;
			SaveAll();
		});
		spineBlendSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.spineBlend = v;
			SaveAll();
		});
		eyeBlendSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.eyeBlend = v;
			SaveAll();
		});
		hueShiftSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.uiHueShift = v;
			ThemeManager themeManager = Object.FindFirstObjectByType<ThemeManager>();
			if (themeManager != null)
			{
				themeManager.SetHue(v);
			}
			SaveAll();
		});
		saturationSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.uiSaturation = v;
			ThemeManager themeManager = Object.FindFirstObjectByType<ThemeManager>();
			if (themeManager != null)
			{
				themeManager.SetSaturation(v);
			}
			SaveAll();
		});
		windowSitYOffsetSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.windowSitYOffset = v;
			SaveAll();
		});
		danceSwitchTimeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.danceSwitchTime = v;
			SaveAll();
		});
		danceTransitionTimeSlider?.onValueChanged.AddListener(delegate(float v)
		{
			SaveLoadHandler.Instance.data.danceTransitionTime = v;
			SaveAll();
		});
		LoadSettings();
		ApplySettings();
	}

	private void SaveAll()
	{
		SaveLoadHandler.Instance.SaveToDisk();
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		soundThresholdSlider?.SetValueWithoutNotify(data.soundThreshold);
		idleSwitchTimeSlider?.SetValueWithoutNotify(data.idleSwitchTime);
		idleTransitionTimeSlider?.SetValueWithoutNotify(data.idleTransitionTime);
		avatarSizeSlider?.SetValueWithoutNotify(data.avatarSize);
		fpsLimitSlider?.SetValueWithoutNotify(data.fpsLimit);
		headBlendSlider?.SetValueWithoutNotify(data.headBlend);
		spineBlendSlider?.SetValueWithoutNotify(data.spineBlend);
		eyeBlendSlider?.SetValueWithoutNotify(data.eyeBlend);
		hueShiftSlider?.SetValueWithoutNotify(data.uiHueShift);
		saturationSlider?.SetValueWithoutNotify(data.uiSaturation);
		windowSitYOffsetSlider?.SetValueWithoutNotify(data.windowSitYOffset);
		danceSwitchTimeSlider?.SetValueWithoutNotify(data.danceSwitchTime);
		danceTransitionTimeSlider?.SetValueWithoutNotify(data.danceTransitionTime);
	}

	public void ApplySettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		FPSLimiter[] array = Object.FindObjectsByType<FPSLimiter>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetFPSLimit(data.fpsLimit);
		}
		AvatarScaleController avatarScaleController = Object.FindFirstObjectByType<AvatarScaleController>();
		if (avatarScaleController != null)
		{
			avatarScaleController.SyncWithSlider();
		}
		ThemeManager themeManager = Object.FindFirstObjectByType<ThemeManager>();
		if (themeManager != null)
		{
			themeManager.SetHue(data.uiHueShift);
			themeManager.SetSaturation(data.uiSaturation);
		}
		AvatarWindowHandler[] array2 = Object.FindObjectsByType<AvatarWindowHandler>(FindObjectsSortMode.None);
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].windowSitYOffset = SaveLoadHandler.Instance.data.windowSitYOffset;
		}
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
	}

	public void ResetToDefaults()
	{
		soundThresholdSlider?.SetValueWithoutNotify(0.2f);
		idleSwitchTimeSlider?.SetValueWithoutNotify(10f);
		idleTransitionTimeSlider?.SetValueWithoutNotify(1f);
		avatarSizeSlider?.SetValueWithoutNotify(1f);
		fpsLimitSlider?.SetValueWithoutNotify(90f);
		headBlendSlider?.SetValueWithoutNotify(0.7f);
		spineBlendSlider?.SetValueWithoutNotify(0.5f);
		eyeBlendSlider?.SetValueWithoutNotify(1f);
		hueShiftSlider?.SetValueWithoutNotify(0f);
		saturationSlider?.SetValueWithoutNotify(1f);
		windowSitYOffsetSlider?.SetValueWithoutNotify(0f);
		danceSwitchTimeSlider?.SetValueWithoutNotify(15f);
		danceTransitionTimeSlider?.SetValueWithoutNotify(2f);
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.soundThreshold = 0.2f;
		data.idleSwitchTime = 10f;
		data.idleTransitionTime = 1f;
		data.avatarSize = 1f;
		data.fpsLimit = 90;
		data.headBlend = 0.7f;
		data.spineBlend = 0.5f;
		data.eyeBlend = 1f;
		data.windowSitYOffset = 0f;
		data.danceSwitchTime = 15f;
		data.danceTransitionTime = 2f;
		data.uiHueShift = 0f;
		data.uiSaturation = 1f;
		SaveLoadHandler.Instance.SaveToDisk();
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
		ApplySettings();
	}
}
