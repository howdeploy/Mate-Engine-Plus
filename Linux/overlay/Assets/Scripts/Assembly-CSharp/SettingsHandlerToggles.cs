using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerToggles : MonoBehaviour
{
	[Header("Toggles")]
	public Toggle enableDancingToggle;

	public Toggle enableMouseTrackingToggle;

	public Toggle isTopmostToggle;

	public Toggle enableParticlesToggle;

	public Toggle bloomToggle;

	public Toggle dayNightToggle;

	public Toggle enableWindowSittingToggle;

	public Toggle enableDiscordRPCToggle;

	public Toggle enableHandHoldingToggle;

	public Toggle ambientOcclusionToggle;

	public Toggle enableIKToggle;

	public Toggle enableDanceSwitchToggle;

	public Toggle enableRandomMessagesToggle;

	public Toggle enableHusbandoModeToggle;

	public Toggle enableAutoMemoryTrimToggle;

	public Toggle enableMinecraftMessagesToggle;

	public Toggle enableFeedSystemToggle;

	public Toggle enableRandomAvatarToggle;

	public Toggle enableLocomotionToggle;

	[Header("External Objects")]
	public GameObject bloomObject;

	public GameObject dayNightObject;

	public GameObject ambientOcclusionObject;

	public GameObject uniWindowControllerObject;

	private WindowManager uniWindowController;

	private AvatarParticleHandler currentParticleHandler;

	private void Start()
	{
		if (uniWindowControllerObject != null)
		{
			uniWindowController = uniWindowControllerObject.GetComponent<WindowManager>();
		}
		else
		{
			uniWindowController = Object.FindFirstObjectByType<WindowManager>();
		}
		enableDancingToggle?.onValueChanged.AddListener(OnEnableDancingChanged);
		enableMouseTrackingToggle?.onValueChanged.AddListener(OnEnableMouseTrackingChanged);
		isTopmostToggle?.onValueChanged.AddListener(OnIsTopmostChanged);
		enableParticlesToggle?.onValueChanged.AddListener(OnEnableParticlesChanged);
		bloomToggle?.onValueChanged.AddListener(OnBloomChanged);
		dayNightToggle?.onValueChanged.AddListener(OnDayNightChanged);
		enableWindowSittingToggle?.onValueChanged.AddListener(OnEnableWindowSittingChanged);
		enableDiscordRPCToggle?.onValueChanged.AddListener(OnEnableDiscordRPCChanged);
		enableHandHoldingToggle?.onValueChanged.AddListener(OnEnableHandHoldingChanged);
		ambientOcclusionToggle?.onValueChanged.AddListener(OnAmbientOcclusionChanged);
		enableIKToggle?.onValueChanged.AddListener(OnEnableIKChanged);
		enableDanceSwitchToggle?.onValueChanged.AddListener(OnEnableDanceSwitchChanged);
		enableRandomMessagesToggle?.onValueChanged.AddListener(OnEnableRandomMessagesChanged);
		enableHusbandoModeToggle?.onValueChanged.AddListener(OnEnableHusbandoModeChanged);
		enableAutoMemoryTrimToggle?.onValueChanged.AddListener(OnEnableAutoMemoryTrimChanged);
		enableMinecraftMessagesToggle?.onValueChanged.AddListener(OnEnableMinecraftMessagesChanged);
		enableFeedSystemToggle?.onValueChanged.AddListener(OnEnableFeedSystemChanged);
		enableRandomAvatarToggle?.onValueChanged.AddListener(OnEnableRandomAvatarChanged);
		enableLocomotionToggle?.onValueChanged.AddListener(OnEnableLocomotionChanged);
		LoadSettings();
		ApplySettings();
	}

	private void OnEnableDancingChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableDancing = v;
		ApplySettings();
		Save();
	}

	private void OnEnableMouseTrackingChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableMouseTracking = v;
		ApplySettings();
		Save();
	}

	private void OnIsTopmostChanged(bool v)
	{
		SaveLoadHandler.Instance.data.isTopmost = v;
		ApplySettings();
		Save();
	}

	private void OnEnableParticlesChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableParticles = v;
		ApplySettings();
		Save();
	}

	private void OnBloomChanged(bool v)
	{
		SaveLoadHandler.Instance.data.bloom = v;
		ApplySettings();
		Save();
	}

	private void OnDayNightChanged(bool v)
	{
		SaveLoadHandler.Instance.data.dayNight = v;
		ApplySettings();
		Save();
	}

	private void OnEnableWindowSittingChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableWindowSitting = v;
		ApplySettings();
		if (!v)
		{
			AvatarWindowHandler[] array = Object.FindObjectsByType<AvatarWindowHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ForceExitWindowSitting();
			}
		}
		Save();
	}

	private void OnEnableDiscordRPCChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableDiscordRPC = v;
		ApplySettings();
		Save();
	}

	private void OnEnableHandHoldingChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableHandHolding = v;
		ApplySettings();
		Save();
	}

	private void OnAmbientOcclusionChanged(bool v)
	{
		SaveLoadHandler.Instance.data.ambientOcclusion = v;
		ApplySettings();
		Save();
	}

	private void OnEnableIKChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableIK = v;
		ApplySettings();
		Save();
	}

	private void OnEnableDanceSwitchChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableDanceSwitch = v;
		Save();
	}

	private void OnEnableAutoMemoryTrimChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableAutoMemoryTrim = v;
		ApplySettings();
		Save();
	}

	private void OnEnableRandomMessagesChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableRandomMessages = v;
		ApplySettings();
		Save();
	}

	private void OnEnableHusbandoModeChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableHusbandoMode = v;
		ApplySettings();
		Save();
	}

	private void OnEnableMinecraftMessagesChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableMinecraftMessages = v;
		ApplySettings();
		Save();
	}

	private void OnEnableFeedSystemChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableFeedSystem = v;
		ApplySettings();
		Save();
	}

	private void OnEnableRandomAvatarChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableRandomAvatar = v;
		Save();
	}

	private void OnEnableLocomotionChanged(bool v)
	{
		SaveLoadHandler.Instance.data.enableLocomotion = v;
		ApplySettings();
		Save();
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		enableDancingToggle?.SetIsOnWithoutNotify(data.enableDancing);
		enableMouseTrackingToggle?.SetIsOnWithoutNotify(data.enableMouseTracking);
		isTopmostToggle?.SetIsOnWithoutNotify(data.isTopmost);
		enableParticlesToggle?.SetIsOnWithoutNotify(data.enableParticles);
		bloomToggle?.SetIsOnWithoutNotify(data.bloom);
		dayNightToggle?.SetIsOnWithoutNotify(data.dayNight);
		enableWindowSittingToggle?.SetIsOnWithoutNotify(data.enableWindowSitting);
		enableDiscordRPCToggle?.SetIsOnWithoutNotify(data.enableDiscordRPC);
		enableHandHoldingToggle?.SetIsOnWithoutNotify(data.enableHandHolding);
		ambientOcclusionToggle?.SetIsOnWithoutNotify(data.ambientOcclusion);
		enableIKToggle?.SetIsOnWithoutNotify(data.enableIK);
		enableDanceSwitchToggle?.SetIsOnWithoutNotify(data.enableDanceSwitch);
		enableRandomMessagesToggle?.SetIsOnWithoutNotify(data.enableRandomMessages);
		enableHusbandoModeToggle?.SetIsOnWithoutNotify(data.enableHusbandoMode);
		enableAutoMemoryTrimToggle?.SetIsOnWithoutNotify(data.enableAutoMemoryTrim);
		enableMinecraftMessagesToggle?.SetIsOnWithoutNotify(data.enableMinecraftMessages);
		enableFeedSystemToggle?.SetIsOnWithoutNotify(SaveLoadHandler.Instance.data.enableFeedSystem);
		enableRandomAvatarToggle?.SetIsOnWithoutNotify(SaveLoadHandler.Instance.data.enableRandomAvatar);
		enableLocomotionToggle?.SetIsOnWithoutNotify(data.enableLocomotion);
		ApplySettings();
	}

	public void ApplySettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		AvatarRandomMessages[] array = Resources.FindObjectsOfTypeAll<AvatarRandomMessages>();
		foreach (AvatarRandomMessages avatarRandomMessages in array)
		{
			avatarRandomMessages.enableRandomMessages = data.enableRandomMessages;
			if (data.enableRandomMessages && avatarRandomMessages.isActiveAndEnabled)
			{
				avatarRandomMessages.StopAllCoroutines();
				avatarRandomMessages.StartCoroutine("RandomMessageLoop");
			}
			else
			{
				avatarRandomMessages.StopAllCoroutines();
			}
		}
		MemoryTrim[] array2 = Resources.FindObjectsOfTypeAll<MemoryTrim>();
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetAutoTrimEnabled(data.enableAutoMemoryTrim);
		}
		if (bloomObject != null)
		{
			bloomObject.SetActive(data.bloom);
		}
		if (dayNightObject != null)
		{
			dayNightObject.SetActive(data.dayNight);
		}
		if (ambientOcclusionObject != null)
		{
			ambientOcclusionObject.SetActive(data.ambientOcclusion);
		}
		if (uniWindowController == null)
		{
			uniWindowController = Object.FindFirstObjectByType<WindowManager>();
		}
		if (uniWindowController != null)
		{
			uniWindowController.SetTopmost(data.isTopmost);
		}
		AvatarFoodController[] array3 = Resources.FindObjectsOfTypeAll<AvatarFoodController>();
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].SetFeatureEnabled(SaveLoadHandler.Instance.data.enableFeedSystem);
		}
		if (currentParticleHandler == null)
		{
			AvatarParticleHandler[] array4 = Object.FindObjectsByType<AvatarParticleHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			currentParticleHandler = ((array4.Length != 0) ? array4[0] : null);
		}
		if (currentParticleHandler != null)
		{
			currentParticleHandler.featureEnabled = data.enableParticles;
			currentParticleHandler.enabled = data.enableParticles;
		}
		PetVoiceReactionHandler.GlobalHoverObjectsEnabled = data.enableParticles;
		AvatarMinecraftMessages[] array5 = Resources.FindObjectsOfTypeAll<AvatarMinecraftMessages>();
		for (int i = 0; i < array5.Length; i++)
		{
			array5[i].enableMinecraftMessages = data.enableMinecraftMessages;
		}
		AvatarLocomotionController[] array6 = Resources.FindObjectsOfTypeAll<AvatarLocomotionController>();
		for (int i = 0; i < array6.Length; i++)
		{
			array6[i].EnableLocomotion = data.enableLocomotion;
		}
	}

	public void ResetToDefaults()
	{
		enableDancingToggle?.SetIsOnWithoutNotify(value: true);
		enableMouseTrackingToggle?.SetIsOnWithoutNotify(value: true);
		isTopmostToggle?.SetIsOnWithoutNotify(value: true);
		enableParticlesToggle?.SetIsOnWithoutNotify(value: true);
		bloomToggle?.SetIsOnWithoutNotify(value: false);
		dayNightToggle?.SetIsOnWithoutNotify(value: true);
		enableWindowSittingToggle?.SetIsOnWithoutNotify(value: false);
		enableDiscordRPCToggle?.SetIsOnWithoutNotify(value: true);
		enableHandHoldingToggle?.SetIsOnWithoutNotify(value: true);
		ambientOcclusionToggle?.SetIsOnWithoutNotify(value: false);
		enableIKToggle?.SetIsOnWithoutNotify(value: true);
		enableDanceSwitchToggle?.SetIsOnWithoutNotify(value: false);
		enableRandomMessagesToggle?.SetIsOnWithoutNotify(value: false);
		enableHusbandoModeToggle?.SetIsOnWithoutNotify(value: false);
		enableAutoMemoryTrimToggle?.SetIsOnWithoutNotify(value: false);
		enableMinecraftMessagesToggle?.SetIsOnWithoutNotify(value: false);
		enableFeedSystemToggle?.SetIsOnWithoutNotify(value: false);
		enableRandomAvatarToggle?.SetIsOnWithoutNotify(value: false);
		enableLocomotionToggle?.SetIsOnWithoutNotify(value: false);
		SaveLoadHandler.Instance.data.enableLocomotion = false;
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.enableDancing = true;
		data.enableMouseTracking = true;
		data.isTopmost = true;
		data.enableParticles = true;
		data.bloom = false;
		data.dayNight = true;
		data.enableWindowSitting = false;
		data.enableDiscordRPC = true;
		data.enableHandHolding = true;
		data.ambientOcclusion = false;
		data.enableIK = true;
		data.enableDanceSwitch = false;
		data.enableRandomMessages = false;
		data.enableHusbandoMode = false;
		data.enableAutoMemoryTrim = false;
		data.enableFeedSystem = false;
		data.enableMinecraftMessages = false;
		SaveLoadHandler.Instance.SaveToDisk();
		ApplySettings();
	}

	private void Save()
	{
		SaveLoadHandler.Instance.SaveToDisk();
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
	}
}
