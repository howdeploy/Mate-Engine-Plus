using System;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class SystemStartHandler : MonoBehaviour
{
	[Header("UI (Optional)")]
	public Toggle autoStartToggle;

	public TMP_Text checkmarkText;

	[Header("Settings")]
	public string runKeyName = "MateEngine";

	public string commandLineArgs = "";

	private bool _isApplyingUI;

	private void Awake()
	{
		if (SaveLoadHandler.Instance == null)
		{
			Debug.LogError("[SystemStartHandler] SaveLoadHandler.Instance is null. Place SaveLoadHandler in the scene first.");
			base.enabled = false;
		}
	}

	private void Start()
	{
		if (autoStartToggle != null)
		{
			autoStartToggle.onValueChanged.AddListener(OnUIToggleChanged);
		}
		LoadFromSaveWithoutNotify();
		TryApplyRegistry(SaveLoadHandler.Instance.data.startWithWindows);
	}

	private void OnDestroy()
	{
		if (autoStartToggle != null)
		{
			autoStartToggle.onValueChanged.RemoveListener(OnUIToggleChanged);
		}
	}

	private void OnUIToggleChanged(bool isOn)
	{
		if (!_isApplyingUI)
		{
			SaveLoadHandler.Instance.data.startWithWindows = isOn;
			SaveLoadHandler.Instance.SaveToDisk();
			TryApplyRegistry(isOn);
			UpdateCheckmarkText(isOn);
		}
	}

	public void OnCheckmarkClicked()
	{
		bool stateFromCode = !GetSavedState();
		SetStateFromCode(stateFromCode);
	}

	public void SetStateFromCode(bool isOn)
	{
		SaveLoadHandler.Instance.data.startWithWindows = isOn;
		SaveLoadHandler.Instance.SaveToDisk();
		TryApplyRegistry(isOn);
		ApplyToUIWithoutNotify(isOn);
	}

	private void LoadFromSaveWithoutNotify()
	{
		ApplyToUIWithoutNotify(GetSavedState());
	}

	private bool GetSavedState()
	{
		if (SaveLoadHandler.Instance.data != null)
		{
			return SaveLoadHandler.Instance.data.startWithWindows;
		}
		return false;
	}

	private void ApplyToUIWithoutNotify(bool isOn)
	{
		_isApplyingUI = true;
		try
		{
			if (autoStartToggle != null)
			{
				autoStartToggle.SetIsOnWithoutNotify(isOn);
			}
			UpdateCheckmarkText(isOn);
		}
		finally
		{
			_isApplyingUI = false;
		}
	}

	private void UpdateCheckmarkText(bool isOn)
	{
		if (checkmarkText != null)
		{
			checkmarkText.text = (isOn ? "☑ Start with system" : "☐ Start with system");
		}
	}

	private void TryApplyRegistry(bool enable)
	{
		if (Application.platform != RuntimePlatform.LinuxPlayer)
		{
			Debug.Log("[SystemStartHandler] Autostart is applied only in the Linux player.");
			return;
		}
		try
		{
			string config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			if (string.IsNullOrEmpty(config)) config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
			string path = Path.Combine(config, "autostart", "mateengine-shoji-3.4.desktop");
			if (!enable) { if (File.Exists(path)) File.Delete(path); return; }
			string launcher = Path.GetFullPath(Path.Combine(Application.dataPath, "../launch.sh"));
			if (!File.Exists(launcher)) throw new FileNotFoundException("Linux launcher is missing", launcher);
			string quoted = launcher.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`").Replace("$", "\\$").Replace("%", "%%");
			string arguments = commandLineArgs.Replace("\r", "").Replace("\n", "").Replace("%", "%%");
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, "[Desktop Entry]\nType=Application\nName=MateEngine Shoji 3.4\nExec=/bin/bash \"" + quoted + "\" " + arguments + "\nTerminal=false\n");
		}
		catch (Exception ex)
		{
			Debug.LogError("[SystemStartHandler] Autostart update failed: " + ex.Message);
		}
	}

	private string GetCurrentExecutablePathQuoted()
	{
		try
		{
			string text = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Application.productName + ".exe");
			if (File.Exists(text))
			{
				return "\"" + text + "\"";
			}
			string text2 = Process.GetCurrentProcess().MainModule?.FileName;
			return string.IsNullOrEmpty(text2) ? string.Empty : ("\"" + text2 + "\"");
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[SystemStartHandler] Failed to get exe path: " + ex.Message);
			return string.Empty;
		}
	}
}
