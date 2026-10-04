using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsHandlerBigScreen : MonoBehaviour
{
	private class TimerRow
	{
		public SaveLoadHandler.SettingsData.TimerEntry e;

		public TMP_Text countdown;

		public TMP_Dropdown hours;

		public TMP_Dropdown minutes;

		public InputField text;

		public Button start;

		public Button stop;

		public Button remove;

		public GameObject go;
	}

	public Toggle bigScreenSaverEnableToggle;

	public Slider bigScreenSaverTimeoutSlider;

	public TMP_Text bigScreenSaverTimeoutLabel;

	public RectTransform alarmsContent;

	public GameObject alarmItemTemplate;

	public Button addAlarmButton;

	[Header("AlarmItem Template Refs")]
	public Toggle templateAlarmToggle;

	public TMP_Dropdown templateHours;

	public TMP_Dropdown templateMinutes;

	public Toggle templateMonday;

	public Toggle templateTuesday;

	public Toggle templateWednesday;

	public Toggle templateThursday;

	public Toggle templateFriday;

	public Toggle templateSaturday;

	public Toggle templateSunday;

	public InputField templateAlarmText;

	public Button templateRemove;

	public GameObject timerItemTemplate;

	public Button addTimerButton;

	[Header("TimerItem Template Refs")]
	public TMP_Dropdown templateTimerHours;

	public TMP_Dropdown templateTimerMinutes;

	public InputField templateTimerText;

	public TMP_Text templateTimerCountdown;

	public Button templateTimerStart;

	public Button templateTimerStop;

	public Button templateTimerRemove;

	private static readonly string[] TimeoutLabels = new string[11]
	{
		"30s", "1 min", "5 min", "15 min", "30 min", "45 min", "1 h", "1.5 h", "2 h", "2.5 h",
		"3 h"
	};

	private List<TimerRow> timerRows = new List<TimerRow>();

	private void Start()
	{
		SetupListeners();
		LoadSettings();
		if (addAlarmButton != null)
		{
			addAlarmButton.onClick.AddListener(OnAddAlarm);
		}
		if (addTimerButton != null)
		{
			addTimerButton.onClick.AddListener(OnAddTimer);
		}
		BuildAlarmsUI();
		BuildTimersUI();
	}

	private void BuildAlarmsUI()
	{
		if (alarmsContent == null || alarmItemTemplate == null)
		{
			return;
		}
		EnsureTemplateDropdowns();
		for (int num = alarmsContent.childCount - 1; num >= 0; num--)
		{
			GameObject gameObject = alarmsContent.GetChild(num).gameObject;
			if (gameObject.activeSelf && !(CloneGet<TMP_Text>(gameObject, templateTimerCountdown) != null))
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		if (data.alarms == null)
		{
			data.alarms = new List<SaveLoadHandler.SettingsData.AlarmEntry>();
		}
		foreach (SaveLoadHandler.SettingsData.AlarmEntry alarm in data.alarms)
		{
			AddRow(alarm);
		}
	}

	private string GetRelativePath(Transform root, Transform target)
	{
		List<string> list = new List<string>();
		Transform transform = target;
		while (transform != null && transform != root)
		{
			list.Add(transform.name);
			transform = transform.parent;
		}
		list.Reverse();
		return string.Join("/", list);
	}

	private void Update()
	{
		UpdateTimerLabels();
	}

	private void UpdateTimerLabels()
	{
		long num = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		foreach (TimerRow timerRow in timerRows)
		{
			if (timerRow != null && timerRow.e != null && !(timerRow.countdown == null))
			{
				long num2 = 0L;
				num2 = ((!timerRow.e.running || timerRow.e.targetUnix <= 0) ? timerRow.e.presetSeconds : Math.Max(0L, timerRow.e.targetUnix - num));
				int num3 = (int)(num2 / 3600);
				int num4 = (int)(num2 % 3600 / 60);
				int num5 = (int)(num2 % 60);
				timerRow.countdown.text = $"{num3:00}:{num4:00}:{num5:00}";
			}
		}
	}

	private void BuildTimersUI()
	{
		if (alarmsContent == null || timerItemTemplate == null)
		{
			return;
		}
		EnsureTimerDropdowns();
		foreach (TimerRow timerRow in timerRows)
		{
			if ((bool)timerRow.go)
			{
				UnityEngine.Object.Destroy(timerRow.go);
			}
		}
		timerRows.Clear();
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		if (data.timers == null)
		{
			data.timers = new List<SaveLoadHandler.SettingsData.TimerEntry>();
		}
		foreach (SaveLoadHandler.SettingsData.TimerEntry timer in data.timers)
		{
			AddTimerRow(timer);
		}
	}

	private void AddTimerRow(SaveLoadHandler.SettingsData.TimerEntry e)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(timerItemTemplate, alarmsContent);
		gameObject.SetActive(value: true);
		TimerRow row = new TimerRow
		{
			e = e,
			go = gameObject,
			hours = CloneGet<TMP_Dropdown>(gameObject, templateTimerHours),
			minutes = CloneGet<TMP_Dropdown>(gameObject, templateTimerMinutes),
			text = CloneGet<InputField>(gameObject, templateTimerText),
			countdown = CloneGet<TMP_Text>(gameObject, templateTimerCountdown),
			start = CloneGet<Button>(gameObject, templateTimerStart),
			stop = CloneGet<Button>(gameObject, templateTimerStop),
			remove = CloneGet<Button>(gameObject, templateTimerRemove)
		};
		if (row.hours != null)
		{
			row.hours.SetValueWithoutNotify(Mathf.Clamp(e.hours, 0, 24));
		}
		if (row.minutes != null)
		{
			row.minutes.SetValueWithoutNotify(Mathf.Clamp(e.minutes, 0, 59));
		}
		if (row.text != null)
		{
			row.text.SetTextWithoutNotify(e.text ?? "");
		}
		if (row.hours != null)
		{
			row.hours.onValueChanged.AddListener(delegate(int v)
			{
				e.hours = v;
				e.presetSeconds = e.hours * 3600 + e.minutes * 60;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (row.minutes != null)
		{
			row.minutes.onValueChanged.AddListener(delegate(int v)
			{
				e.minutes = v;
				e.presetSeconds = e.hours * 3600 + e.minutes * 60;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (row.text != null)
		{
			row.text.onEndEdit.AddListener(delegate(string v)
			{
				e.text = v ?? "";
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (row.start != null)
		{
			row.start.onClick.AddListener(delegate
			{
				e.enabled = true;
				e.presetSeconds = e.hours * 3600 + e.minutes * 60;
				if (e.presetSeconds > 0)
				{
					e.running = true;
					e.targetUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + e.presetSeconds;
					SaveLoadHandler.Instance.SaveToDisk();
				}
			});
		}
		if (row.stop != null)
		{
			row.stop.onClick.AddListener(delegate
			{
				e.running = false;
				e.targetUnix = 0L;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (row.remove != null)
		{
			row.remove.onClick.AddListener(delegate
			{
				SaveLoadHandler.Instance.data.timers.Remove(e);
				if ((bool)row.go)
				{
					UnityEngine.Object.Destroy(row.go);
				}
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		timerRows.Add(row);
	}

	private void OnAddTimer()
	{
		SaveLoadHandler.SettingsData.TimerEntry timerEntry = new SaveLoadHandler.SettingsData.TimerEntry
		{
			id = Guid.NewGuid().ToString("N"),
			enabled = true,
			hours = 0,
			minutes = 5,
			presetSeconds = 300,
			running = false,
			targetUnix = 0L,
			text = "Timer"
		};
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		if (data.timers == null)
		{
			data.timers = new List<SaveLoadHandler.SettingsData.TimerEntry>();
		}
		data.timers.Add(timerEntry);
		SaveLoadHandler.Instance.SaveToDisk();
		AddTimerRow(timerEntry);
	}

	private T CloneGet<T>(GameObject clone, Component templateComp) where T : Component
	{
		if (templateComp == null)
		{
			return null;
		}
		Transform transform = null;
		if (alarmItemTemplate != null && templateComp.transform.IsChildOf(alarmItemTemplate.transform))
		{
			transform = alarmItemTemplate.transform;
		}
		else if (timerItemTemplate != null && templateComp.transform.IsChildOf(timerItemTemplate.transform))
		{
			transform = timerItemTemplate.transform;
		}
		if (transform == null)
		{
			return null;
		}
		string relativePath = GetRelativePath(transform, templateComp.transform);
		Transform transform2 = clone.transform.Find(relativePath);
		if (!transform2)
		{
			return null;
		}
		return transform2.GetComponent<T>();
	}

	private void EnsureTemplateDropdowns()
	{
		if (templateHours != null && templateHours.options.Count != 24)
		{
			templateHours.ClearOptions();
			List<string> list = new List<string>();
			for (int i = 0; i < 24; i++)
			{
				list.Add(i.ToString("D2"));
			}
			templateHours.AddOptions(list);
		}
		if (templateMinutes != null && templateMinutes.options.Count != 60)
		{
			templateMinutes.ClearOptions();
			List<string> list2 = new List<string>();
			for (int j = 0; j < 60; j++)
			{
				list2.Add(j.ToString("D2"));
			}
			templateMinutes.AddOptions(list2);
		}
	}

	private void EnsureTimerDropdowns()
	{
		if (templateTimerHours != null && templateTimerHours.options.Count != 25)
		{
			templateTimerHours.ClearOptions();
			List<string> list = new List<string>();
			for (int i = 0; i <= 24; i++)
			{
				list.Add(i.ToString("D2"));
			}
			templateTimerHours.AddOptions(list);
		}
		if (templateTimerMinutes != null && templateTimerMinutes.options.Count != 60)
		{
			templateTimerMinutes.ClearOptions();
			List<string> list2 = new List<string>();
			for (int j = 0; j < 60; j++)
			{
				list2.Add(j.ToString("D2"));
			}
			templateTimerMinutes.AddOptions(list2);
		}
	}

	private void AddRow(SaveLoadHandler.SettingsData.AlarmEntry e)
	{
		GameObject go = UnityEngine.Object.Instantiate(alarmItemTemplate, alarmsContent);
		go.SetActive(value: true);
		Toggle toggle = CloneGet<Toggle>(go, templateAlarmToggle);
		TMP_Dropdown tMP_Dropdown = CloneGet<TMP_Dropdown>(go, templateHours);
		TMP_Dropdown tMP_Dropdown2 = CloneGet<TMP_Dropdown>(go, templateMinutes);
		Toggle toggle2 = CloneGet<Toggle>(go, templateMonday);
		Toggle toggle3 = CloneGet<Toggle>(go, templateTuesday);
		Toggle toggle4 = CloneGet<Toggle>(go, templateWednesday);
		Toggle toggle5 = CloneGet<Toggle>(go, templateThursday);
		Toggle toggle6 = CloneGet<Toggle>(go, templateFriday);
		Toggle toggle7 = CloneGet<Toggle>(go, templateSaturday);
		Toggle toggle8 = CloneGet<Toggle>(go, templateSunday);
		InputField inputField = CloneGet<InputField>(go, templateAlarmText);
		Button button = CloneGet<Button>(go, templateRemove);
		if (toggle != null)
		{
			toggle.SetIsOnWithoutNotify(e.enabled);
		}
		if (tMP_Dropdown != null)
		{
			tMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(e.hour, 0, 23));
		}
		if (tMP_Dropdown2 != null)
		{
			tMP_Dropdown2.SetValueWithoutNotify(Mathf.Clamp(e.minute, 0, 59));
		}
		if (toggle2 != null)
		{
			toggle2.SetIsOnWithoutNotify((e.daysMask & 1) != 0);
		}
		if (toggle3 != null)
		{
			toggle3.SetIsOnWithoutNotify((e.daysMask & 2) != 0);
		}
		if (toggle4 != null)
		{
			toggle4.SetIsOnWithoutNotify((e.daysMask & 4) != 0);
		}
		if (toggle5 != null)
		{
			toggle5.SetIsOnWithoutNotify((e.daysMask & 8) != 0);
		}
		if (toggle6 != null)
		{
			toggle6.SetIsOnWithoutNotify((e.daysMask & 0x10) != 0);
		}
		if (toggle7 != null)
		{
			toggle7.SetIsOnWithoutNotify((e.daysMask & 0x20) != 0);
		}
		if (toggle8 != null)
		{
			toggle8.SetIsOnWithoutNotify((e.daysMask & 0x40) != 0);
		}
		if (inputField != null)
		{
			inputField.SetTextWithoutNotify(e.text ?? "");
		}
		if (toggle != null)
		{
			toggle.onValueChanged.AddListener(delegate(bool v)
			{
				e.enabled = v;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (tMP_Dropdown != null)
		{
			tMP_Dropdown.onValueChanged.AddListener(delegate(int v)
			{
				e.hour = v;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (tMP_Dropdown2 != null)
		{
			tMP_Dropdown2.onValueChanged.AddListener(delegate(int v)
			{
				e.minute = v;
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle2 != null)
		{
			toggle2.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 0, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle3 != null)
		{
			toggle3.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 1, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle4 != null)
		{
			toggle4.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 2, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle5 != null)
		{
			toggle5.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 3, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle6 != null)
		{
			toggle6.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 4, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle7 != null)
		{
			toggle7.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 5, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (toggle8 != null)
		{
			toggle8.onValueChanged.AddListener(delegate(bool v)
			{
				e.daysMask = SetBit(e.daysMask, 6, v);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (inputField != null)
		{
			inputField.onEndEdit.AddListener(delegate(string v)
			{
				e.text = v ?? "";
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
		if (button != null)
		{
			button.onClick.AddListener(delegate
			{
				SaveLoadHandler.Instance.data.alarms.Remove(e);
				UnityEngine.Object.Destroy(go);
				SaveLoadHandler.Instance.SaveToDisk();
			});
		}
	}

	private void OnAddAlarm()
	{
		SaveLoadHandler.SettingsData.AlarmEntry alarmEntry = new SaveLoadHandler.SettingsData.AlarmEntry
		{
			id = Guid.NewGuid().ToString("N"),
			enabled = true,
			hour = 7,
			minute = 0,
			daysMask = 0,
			text = "Alarm",
			lastTriggeredUnixMinute = 0L
		};
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		if (data.alarms == null)
		{
			data.alarms = new List<SaveLoadHandler.SettingsData.AlarmEntry>();
		}
		data.alarms.Add(alarmEntry);
		data.alarmsEnabled = true;
		SaveLoadHandler.Instance.SaveToDisk();
		AddRow(alarmEntry);
	}

	private byte SetBit(byte mask, int bit, bool on)
	{
		if (!on)
		{
			return (byte)(mask & ~(1 << bit));
		}
		return (byte)(mask | (1 << bit));
	}

	private void SetupListeners()
	{
		bigScreenSaverEnableToggle?.onValueChanged.AddListener(OnScreenSaverEnableChanged);
		bigScreenSaverTimeoutSlider?.onValueChanged.AddListener(OnTimeoutSliderChanged);
	}

	public void LoadSettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		bigScreenSaverEnableToggle?.SetIsOnWithoutNotify(data.bigScreenScreenSaverEnabled);
		bigScreenSaverTimeoutSlider?.SetValueWithoutNotify(data.bigScreenScreenSaverTimeoutIndex);
		if (bigScreenSaverTimeoutLabel != null && data.bigScreenScreenSaverTimeoutIndex >= 0 && data.bigScreenScreenSaverTimeoutIndex < TimeoutLabels.Length)
		{
			bigScreenSaverTimeoutLabel.text = TimeoutLabels[data.bigScreenScreenSaverTimeoutIndex];
		}
	}

	private void OnScreenSaverEnableChanged(bool v)
	{
		SaveLoadHandler.Instance.data.bigScreenScreenSaverEnabled = v;
		Save();
	}

	private void OnTimeoutSliderChanged(float v)
	{
		int num = Mathf.Clamp(Mathf.RoundToInt(v), 0, TimeoutLabels.Length - 1);
		SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex = num;
		if (bigScreenSaverTimeoutLabel != null)
		{
			bigScreenSaverTimeoutLabel.text = TimeoutLabels[num];
		}
		Save();
	}

	public void ApplySettings()
	{
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.bigScreenScreenSaverEnabled = bigScreenSaverEnableToggle?.isOn ?? data.bigScreenScreenSaverEnabled;
		data.bigScreenScreenSaverTimeoutIndex = Mathf.RoundToInt(bigScreenSaverTimeoutSlider?.value ?? ((float)data.bigScreenScreenSaverTimeoutIndex));
	}

	public void ResetToDefaults()
	{
		bigScreenSaverEnableToggle?.SetIsOnWithoutNotify(value: false);
		bigScreenSaverTimeoutSlider?.SetValueWithoutNotify(0f);
		SaveLoadHandler.SettingsData data = SaveLoadHandler.Instance.data;
		data.bigScreenScreenSaverEnabled = false;
		data.bigScreenScreenSaverTimeoutIndex = 0;
		SaveLoadHandler.Instance.SaveToDisk();
	}

	private void Save()
	{
		SaveLoadHandler.Instance.SaveToDisk();
	}
}
