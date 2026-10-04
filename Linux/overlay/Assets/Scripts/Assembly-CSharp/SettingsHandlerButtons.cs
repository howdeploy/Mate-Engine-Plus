using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerButtons : MonoBehaviour
{
	public Button applyButton;

	public Button resetButton;

	public Button windowSizeButton;

	public Button refreshAppsListButton;

	public SettingsHandlerToggles togglesHandler;

	public SettingsHandlerSliders slidersHandler;

	public SettingsHandlerDropdowns dropdownsHandler;

	public SettingsHandlerAudio audioHandler;

	public SettingsHandlerLights lightsHandler;

	public SettingsHandlerAccessory accessoryHandler;

	public SettingsHandlerBigScreen bigScreenHandler;

	public VRMLoader vrmLoader;

	public GameObject uniWindowControllerObject;

	private WindowManager uniWindowController;

	private void Start()
	{
		if (applyButton != null)
		{
			applyButton.onClick.AddListener(OnApplyClicked);
		}
		if (resetButton != null)
		{
			resetButton.onClick.AddListener(OnResetClicked);
		}
		if (windowSizeButton != null)
		{
			windowSizeButton.onClick.AddListener(CycleWindowSize);
		}
		if (refreshAppsListButton != null)
		{
			refreshAppsListButton.onClick.AddListener(OnRefreshAppsClicked);
		}
		if (uniWindowControllerObject != null)
		{
			uniWindowController = uniWindowControllerObject.GetComponent<WindowManager>();
		}
		else
		{
			uniWindowController = Object.FindFirstObjectByType<WindowManager>();
		}
	}

	private void OnApplyClicked()
	{
		togglesHandler?.ApplySettings();
		slidersHandler?.ApplySettings();
		dropdownsHandler?.ApplySettings();
		audioHandler?.ApplySettings();
		lightsHandler?.ApplySettings();
		accessoryHandler?.ApplySettings();
		bigScreenHandler?.ApplySettings();
		SaveLoadHandler.Instance.SaveToDisk();
		SaveLoadHandler.ApplyAllSettingsToAllAvatars();
	}

	private void OnResetClicked()
	{
		togglesHandler?.ResetToDefaults();
		slidersHandler?.ResetToDefaults();
		dropdownsHandler?.ResetToDefaults();
		audioHandler?.ResetToDefaults();
		lightsHandler?.ResetAllLightsToDefault();
		lightsHandler?.ResetAllLightTogglesToDefault();
		accessoryHandler?.ResetToDefaults();
		bigScreenHandler?.ResetToDefaults();
		if (vrmLoader != null)
		{
			vrmLoader.ResetModel();
		}
		SaveLoadHandler.Instance.SaveToDisk();
	}

	public void CycleWindowSize()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		WindowManager uniWindowController = this.uniWindowController ?? WindowManager.Instance;
		if (uniWindowController == null) return;
		switch (data.windowSizeState)
		{
		case SaveLoadHandler.SettingsData.WindowSizeState.Normal:
			data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Big;
			uniWindowController.SetWindowSize(new Vector2Int(2048, 1536));
			break;
		case SaveLoadHandler.SettingsData.WindowSizeState.Big:
			data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Small;
			uniWindowController.SetWindowSize(new Vector2Int(768, 512));
			break;
		case SaveLoadHandler.SettingsData.WindowSizeState.Small:
			data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Normal;
			uniWindowController.SetWindowSize(new Vector2Int(1536, 1024));
			break;
		}
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void OnRefreshAppsClicked()
	{
		AllowedAppsManager allowedAppsManager = Object.FindFirstObjectByType<AllowedAppsManager>();
		if (allowedAppsManager != null)
		{
			allowedAppsManager.RefreshUI();
		}
	}
}
