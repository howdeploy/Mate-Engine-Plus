using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SaveLoadHandler : MonoBehaviour
{
	[Serializable]
	public class SettingsData
	{
		public enum WindowSizeState
		{
			Normal = 0,
			Big = 1,
			Small = 2
		}

		[Serializable]
		public class AlarmEntry
		{
			public string id;

			public bool enabled;

			public int hour;

			public int minute;

			public byte daysMask;

			public string text;

			public long lastTriggeredUnixMinute;
		}

		[Serializable]
		public class TimerEntry
		{
			public string id;

			public bool enabled;

			public int hours;

			public int minutes;

			public int presetSeconds;

			public bool running;

			public long targetUnix;

			public string text;
		}

		public WindowSizeState windowSizeState;

		public WindowType windowType = WindowType.Normal;

		public bool useKWinApi;

		public bool useLegacyMoveResizeCalls;

		public bool allowHyprlandMonitorSitting;

		public float soundThreshold = 0.2f;

		public float idleSwitchTime = 10f;

		public float idleTransitionTime = 1f;

		public bool enableDanceSwitch;

		public float danceSwitchTime = 15f;

		public float danceTransitionTime = 2f;

		public float avatarSize = 1f;

		public bool enableDancing = true;

		public bool enableMouseTracking = true;

		public int fpsLimit = 90;

		public bool isTopmost = true;

		public List<string> allowedApps = new List<string>();

		public bool bloom;

		public bool dayNight = true;

		public bool enableParticles = true;

		public float petVolume = 1f;

		public float effectsVolume = 1f;

		public float menuVolume = 1f;

		public float headBlend = 0.7f;

		public float eyeBlend = 1f;

		public float spineBlend = 0.5f;

		public bool enableHandHolding = true;

		public bool enableWindowSitting;

		public bool ambientOcclusion;

		public float uiHueShift;

		public float uiSaturation = 1f;

		public bool enableDiscordRPC = true;

		public bool tutorialDone;

		public string selectedLocaleCode = "en";

		public bool enableIK = true;

		public int bigScreenScreenSaverTimeoutIndex;

		public bool bigScreenScreenSaverEnabled;

		public float windowSitYOffset;

		public Dictionary<string, float> lightIntensities = new Dictionary<string, float>();

		public Dictionary<string, float> lightSaturations = new Dictionary<string, float>();

		public Dictionary<string, float> lightHues = new Dictionary<string, float>();

		public Dictionary<string, bool> groupToggles = new Dictionary<string, bool>();

		public Dictionary<string, bool> modStates = new Dictionary<string, bool>();

		public int graphicsQualityLevel = 1;

		public Dictionary<string, bool> accessoryStates = new Dictionary<string, bool>();

		public bool startWithWindows;

		public bool enableRandomMessages;

		public string selectedModelPath = "";

		public int contextLength = 4096;

		public bool enableHusbandoMode;

		public bool enableAutoMemoryTrim;

		public int settingsVersion;

		public bool alarmsEnabled = true;

		public bool enableMinecraftMessages;

		public string selectedParticleTheme = "Standard";

		public bool enableFeedSystem;

		public bool enableRandomAvatar;

		public bool enableLocomotion;

		public List<AlarmEntry> alarms = new List<AlarmEntry>();

		public List<TimerEntry> timers = new List<TimerEntry>();
	}

	public SettingsData data;

	public bool safeMode;

	private static string fileName = "settings.json";

	private static string customDataDir = null;

	public static SaveLoadHandler Instance { get; private set; }

	private string BaseDir
	{
		get
		{
			if (!string.IsNullOrEmpty(customDataDir))
			{
				return Path.Combine(Application.persistentDataPath, customDataDir);
			}
			return Application.persistentDataPath;
		}
	}

	private string FilePath => Path.Combine(BaseDir, fileName);

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i].Equals("--safe-mode", StringComparison.OrdinalIgnoreCase)) safeMode = true;
			if (commandLineArgs[i].Equals("--savefile", StringComparison.OrdinalIgnoreCase) && i + 1 < commandLineArgs.Length)
			{
				fileName = commandLineArgs[i + 1].Trim('"');
			}
			if (commandLineArgs[i].Equals("--datadir", StringComparison.OrdinalIgnoreCase) && i + 1 < commandLineArgs.Length)
			{
				customDataDir = commandLineArgs[i + 1].Trim('"');
			}
		}
		LoadFromDisk();
		ApplyAllSettingsToAllAvatars();
		ThemeManager themeManager = UnityEngine.Object.FindFirstObjectByType<ThemeManager>();
		if (themeManager != null)
		{
			themeManager.SetHue(data.uiHueShift);
			themeManager.SetSaturation(data.uiSaturation);
		}
		FPSLimiter[] array = UnityEngine.Object.FindObjectsByType<FPSLimiter>(FindObjectsSortMode.None);
		foreach (FPSLimiter obj in array)
		{
			obj.targetFPS = data.fpsLimit;
			obj.ApplyFPSLimit();
		}
	}

	public void SaveToDisk()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(FilePath);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string contents = JsonConvert.SerializeObject(data, Formatting.Indented);
			File.WriteAllText(FilePath, contents);
			Debug.Log("[SaveLoadHandler] Saved settings to: " + FilePath);
		}
		catch (Exception ex)
		{
			Debug.LogError("[SaveLoadHandler] Failed to save: " + ex);
		}
	}

	public void LoadFromDisk()
	{
		if (File.Exists(FilePath))
		{
			try
			{
				string value = File.ReadAllText(FilePath);
				data = JsonConvert.DeserializeObject<SettingsData>(value);
			}
			catch
			{
				data = new SettingsData();
			}
		}
		else
		{
			data = new SettingsData();
		}
		MigrateAfterLoad();
	}

	private void MigrateAfterLoad()
	{
		if (data.timers == null)
		{
			data.timers = new List<SettingsData.TimerEntry>();
		}
		if (string.IsNullOrEmpty(data.selectedParticleTheme))
		{
			data.selectedParticleTheme = "Standard";
		}
		if (data == null)
		{
			data = new SettingsData();
		}
		if (data.alarms == null)
		{
			data.alarms = new List<SettingsData.AlarmEntry>();
		}
		if (data.settingsVersion < 1)
		{
			data.settingsVersion = 1;
			SaveToDisk();
		}
	}

	public static void SyncAllowedAppsToAllAvatars()
	{
		AvatarAnimatorController[] array = Resources.FindObjectsOfTypeAll<AvatarAnimatorController>();
		List<string> allowedApps = new List<string>(Instance.data.allowedApps);
		AvatarAnimatorController[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].allowedApps = allowedApps;
		}
	}

	public static void ApplyAllSettingsToAllAvatars()
	{
		SettingsData settingsData = Instance.data;
		AvatarAnimatorController[] array = Resources.FindObjectsOfTypeAll<AvatarAnimatorController>();
		foreach (AvatarAnimatorController avatarAnimatorController in array)
		{
			avatarAnimatorController.SOUND_THRESHOLD = settingsData.soundThreshold;
			avatarAnimatorController.IDLE_SWITCH_TIME = settingsData.idleSwitchTime;
			avatarAnimatorController.IDLE_TRANSITION_TIME = settingsData.idleTransitionTime;
			avatarAnimatorController.enableDancing = settingsData.enableDancing;
			avatarAnimatorController.allowedApps = new List<string>(settingsData.allowedApps);
			avatarAnimatorController.transform.localScale = Vector3.one * settingsData.avatarSize;
			avatarAnimatorController.DANCE_SWITCH_TIME = settingsData.danceSwitchTime;
			avatarAnimatorController.DANCE_TRANSITION_TIME = settingsData.danceTransitionTime;
			avatarAnimatorController.enableDanceSwitch = settingsData.enableDanceSwitch;
			avatarAnimatorController.enableHusbandoMode = settingsData.enableHusbandoMode;
			AvatarMouseTracking[] componentsInChildren = avatarAnimatorController.GetComponentsInChildren<AvatarMouseTracking>(includeInactive: true);
			foreach (AvatarMouseTracking obj in componentsInChildren)
			{
				obj.enableMouseTracking = settingsData.enableMouseTracking;
				obj.headBlend = settingsData.headBlend;
				obj.spineBlend = settingsData.spineBlend;
				obj.eyeBlend = settingsData.eyeBlend;
			}
			IKFix[] componentsInChildren2 = avatarAnimatorController.GetComponentsInChildren<IKFix>(includeInactive: true);
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].enableIK = settingsData.enableIK;
			}
			AvatarParticleHandler[] componentsInChildren3 = avatarAnimatorController.GetComponentsInChildren<AvatarParticleHandler>(includeInactive: true);
			foreach (AvatarParticleHandler avatarParticleHandler in componentsInChildren3)
			{
				avatarParticleHandler.featureEnabled = settingsData.enableParticles;
				avatarParticleHandler.enabled = settingsData.enableParticles;
				avatarParticleHandler.selectedTheme = settingsData.selectedParticleTheme;
				try
				{
					avatarParticleHandler.SetTheme(settingsData.selectedParticleTheme);
				}
				catch
				{
				}
			}
			HandHolder[] componentsInChildren4 = avatarAnimatorController.GetComponentsInChildren<HandHolder>(includeInactive: true);
			for (int j = 0; j < componentsInChildren4.Length; j++)
			{
				componentsInChildren4[j].enableHandHolding = settingsData.enableHandHolding;
			}
			if (avatarAnimatorController.animator != null && avatarAnimatorController.animator.isActiveAndEnabled && avatarAnimatorController.animator.runtimeAnimatorController != null)
			{
				avatarAnimatorController.animator.SetBool("isDancing", value: false);
				avatarAnimatorController.animator.SetBool("isDragging", value: false);
				avatarAnimatorController.isDancing = false;
				avatarAnimatorController.isDragging = false;
			}
			AvatarFoodController[] array2 = Resources.FindObjectsOfTypeAll<AvatarFoodController>();
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].SetFeatureEnabled(Instance.data.enableFeedSystem);
			}
			AvatarWindowHandler[] array3 = Resources.FindObjectsOfTypeAll<AvatarWindowHandler>();
			for (int j = 0; j < array3.Length; j++)
			{
				array3[j].windowSitYOffset = settingsData.windowSitYOffset;
			}
			AvatarLocomotionController[] array4 = Resources.FindObjectsOfTypeAll<AvatarLocomotionController>();
			for (int j = 0; j < array4.Length; j++)
			{
				array4[j].EnableLocomotion = settingsData.enableLocomotion;
			}
		}
	}
}
