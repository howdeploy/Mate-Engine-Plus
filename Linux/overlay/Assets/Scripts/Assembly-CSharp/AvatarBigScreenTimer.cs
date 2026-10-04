using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LLMUnitySamples;
using UnityEngine;
using UnityEngine.UI;

public class AvatarBigScreenTimer : MonoBehaviour
{
	public struct POINT
	{
		public int X;

		public int Y;
	}

	[Header("Bubble Material")]
	public Material bubbleMaterial;

	[Header("Enable BigScreen Alarm Feature")]
	public bool enableBigScreenAlarm;

	[Header("Allowed Animator States")]
	public bool useAllowedStatesWhitelist;

	public string[] allowedStates = new string[1] { "Idle" };

	[Header("Click disables BigScreen completely")]
	public bool clickDisablesBoth;

	[Header("Audio")]
	public AudioSource audioSource;

	public List<AudioClip> alarmClips = new List<AudioClip>();

	[Header("Alarm Chat Bubble")]
	[TextArea(1, 3)]
	public string alarmText = "Wake up! This is your alarm!";

	public Transform chatContainer;

	public Sprite bubbleSprite;

	public Color bubbleColor = new Color32(255, 72, 38, 255);

	public Color fontColor = Color.white;

	public Font font;

	public int fontSize = 16;

	public int bubbleWidth = 600;

	public float textPadding = 10f;

	public float bubbleSpacing = 10f;

	[Header("Fake Stream Settings")]
	[Tooltip("Stream speed: characters per second")]
	[Range(5f, 100f)]
	public int streamSpeed = 35;

	[Header("Stream Audio")]
	public AudioSource streamAudioSource;

	private float alarmInputBlockUntil;

	[Header("Alarm Cooldown")]
	public float alarmInputBlockDuration = 5f;

	[Header("Live Status (Inspector)")]
	[SerializeField]
	private string inspectorEvent;

	[SerializeField]
	private string inspectorTargetTime;

	[SerializeField]
	private string inspectorCurrentTime;

	private AvatarBigScreenHandler bigScreenHandler;

	private Animator avatarAnimator;

	private bool alarmActive;

	private Bubble alarmBubble;

	private Coroutine streamCoroutine;

	private readonly Queue<string> pendingEvents = new Queue<string>();

	private bool lastGlobalMouseDown;


	private void Start()
	{
		bigScreenHandler = GetComponent<AvatarBigScreenHandler>();
		avatarAnimator = GetComponent<Animator>();
		alarmActive = false;
		RemoveAlarmBubble();
	}

	private void CheckMultiAlarms()
	{
		SaveLoadHandler.SettingsData settingsData = SaveLoadHandler.Instance?.data;
		if (settingsData == null || !settingsData.alarmsEnabled || settingsData.alarms == null || settingsData.alarms.Count == 0)
		{
			return;
		}
		DateTime now = DateTime.Now;
		int hour = now.Hour;
		int minute = now.Minute;
		int num = (int)((now.DayOfWeek == DayOfWeek.Sunday) ? DayOfWeek.Saturday : (now.DayOfWeek - 1));
		long num2 = new DateTimeOffset(now.Year, now.Month, now.Day, hour, minute, 0, TimeSpan.Zero).ToUnixTimeSeconds() / 60;
		for (int i = 0; i < settingsData.alarms.Count; i++)
		{
			SaveLoadHandler.SettingsData.AlarmEntry alarmEntry = settingsData.alarms[i];
			if (alarmEntry != null && alarmEntry.enabled && alarmEntry.hour == hour && alarmEntry.minute == minute && (alarmEntry.daysMask == 0 || (alarmEntry.daysMask & (1 << num)) != 0) && alarmEntry.lastTriggeredUnixMinute != num2)
			{
				alarmEntry.lastTriggeredUnixMinute = num2;
				SaveLoadHandler.Instance.SaveToDisk();
				alarmEntry.lastTriggeredUnixMinute = num2;
				SaveLoadHandler.Instance.SaveToDisk();
				EnqueueOrTrigger(string.IsNullOrEmpty(alarmEntry.text) ? "Alarm" : alarmEntry.text);
				inspectorEvent = "Alarm queued";
			}
		}
	}

	private void Update()
	{
		enableBigScreenAlarm = SaveLoadHandler.Instance.data.alarmsEnabled;
		if (!enableBigScreenAlarm)
		{
			inspectorEvent = "Alarms disabled";
			StopAlarm();
			return;
		}
		inspectorCurrentTime = DateTime.Now.ToString("HH:mm:ss");
		DateTime nextTime;
		SaveLoadHandler.SettingsData.AlarmEntry nextAlarm = GetNextAlarm(out nextTime);
		inspectorTargetTime = ((nextAlarm != null) ? nextTime.ToString("yyyy-MM-dd HH:mm") : "-");
		if (!alarmActive && nextAlarm != null)
		{
			alarmText = (string.IsNullOrEmpty(nextAlarm.text) ? "Alarm" : nextAlarm.text);
		}
		if (useAllowedStatesWhitelist && !IsInAllowedState())
		{
			inspectorEvent = "Alarm blocked by state";
			StopAlarm();
			RemoveAlarmBubble();
			return;
		}
		CheckMultiAlarms();
		CheckTimers();
		bool num = avatarAnimator != null && avatarAnimator.GetBool("isBigScreen");
		bool flag = avatarAnimator != null && avatarAnimator.GetBool("isBigScreenAlarm");
		if (!flag)
		{
			RemoveAlarmBubble();
		}
		if (!(num && flag) || !alarmActive)
		{
			return;
		}
		if (Time.time < alarmInputBlockUntil)
		{
			inspectorEvent = "Cooldown";
			return;
		}
		inspectorEvent = "Waiting for input";
		if (!IsGlobalUserInput())
		{
			return;
		}
		inspectorEvent = "Alarm stopped by input";
		avatarAnimator.SetBool("isBigScreenAlarm", value: false);
		alarmActive = false;
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		if (clickDisablesBoth)
		{
			avatarAnimator.SetBool("isBigScreen", value: false);
			if (bigScreenHandler != null)
			{
				bigScreenHandler.SendMessage("DeactivateBigScreen");
			}
		}
		if (pendingEvents.Count > 0)
		{
			string text = pendingEvents.Dequeue();
			alarmText = text;
			TriggerAlarmNow();
		}
		else
		{
			RemoveAlarmBubble();
		}
	}

	private void PlayRandomAlarm()
	{
		if (audioSource != null && alarmClips != null && alarmClips.Count > 0)
		{
			AudioClip clip = alarmClips[UnityEngine.Random.Range(0, alarmClips.Count)];
			audioSource.clip = clip;
			audioSource.loop = true;
			audioSource.Play();
		}
	}

	private void StopAlarm()
	{
		if (avatarAnimator != null)
		{
			avatarAnimator.SetBool("isBigScreenAlarm", value: false);
		}
		alarmActive = false;
		if (audioSource != null && audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		RemoveAlarmBubble();
		pendingEvents.Clear();
	}

	private bool IsInAllowedState()
	{
		if (avatarAnimator == null || allowedStates == null || allowedStates.Length == 0)
		{
			return true;
		}
		AnimatorStateInfo currentAnimatorStateInfo = avatarAnimator.GetCurrentAnimatorStateInfo(0);
		string[] array = allowedStates;
		foreach (string value in array)
		{
			if (!string.IsNullOrEmpty(value) && currentAnimatorStateInfo.IsName(value))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsGlobalUserInput()
	{
		var wm = WindowManager.Instance;
		bool flag = wm != null && wm.GetMouseButton(KeyCode.Mouse0);
		bool flag2 = flag && !lastGlobalMouseDown;
		lastGlobalMouseDown = flag;
		bool flag3 = wm != null && wm.IsAnyKeyDown();
		return flag2 || flag3;
	}

	public void TriggerAlarmNow()
	{
		if (avatarAnimator != null)
		{
			avatarAnimator.SetBool("isBigScreen", value: true);
			avatarAnimator.SetBool("isBigScreenAlarm", value: true);
			avatarAnimator.SetBool("isBigScreenSaver", value: false);
			avatarAnimator.SetBool("isWindowSit", value: false);
			avatarAnimator.SetBool("isSitting", value: false);
		}
		if (bigScreenHandler != null)
		{
			bigScreenHandler.SendMessage("ActivateBigScreen");
		}
		PlayRandomAlarm();
		alarmActive = true;
		alarmInputBlockUntil = Time.time + alarmInputBlockDuration;
		inspectorEvent = "Alarm triggered manually";
		StartCoroutine(ShowAlarmBubbleStreamedDelayed());
	}

	private void ShowAlarmBubbleStreamed()
	{
		if (!(chatContainer == null))
		{
			RemoveAlarmBubble();
			BubbleUI ui = new BubbleUI
			{
				sprite = bubbleSprite,
				font = font,
				fontSize = fontSize,
				fontColor = fontColor,
				bubbleColor = bubbleColor,
				bottomPosition = 0f,
				leftPosition = 1f,
				textPadding = textPadding,
				bubbleOffset = bubbleSpacing,
				bubbleWidth = bubbleWidth,
				bubbleHeight = -1f
			};
			alarmBubble = new Bubble(chatContainer, ui, "AlarmBubble", "");
			Image componentInChildren = alarmBubble.GetRectTransform().GetComponentInChildren<Image>(includeInactive: true);
			if (componentInChildren != null && bubbleMaterial != null)
			{
				componentInChildren.material = bubbleMaterial;
			}
			if (streamAudioSource != null)
			{
				streamAudioSource.Stop();
				streamAudioSource.Play();
			}
			if (streamCoroutine != null)
			{
				StopCoroutine(streamCoroutine);
			}
			streamCoroutine = StartCoroutine(FakeStreamAlarmText(alarmText));
		}
	}

	private IEnumerator FakeStreamAlarmText(string fullText)
	{
		if (alarmBubble == null)
		{
			yield break;
		}
		alarmBubble.SetText("");
		int length = 0;
		float delay = 1f / (float)Mathf.Max(streamSpeed, 1);
		while (length < fullText.Length)
		{
			length++;
			alarmBubble.SetText(fullText.Substring(0, length));
			yield return new WaitForSeconds(delay);
			if (alarmBubble == null)
			{
				yield break;
			}
		}
		alarmBubble.SetText(fullText);
		if (streamAudioSource != null && streamAudioSource.isPlaying)
		{
			streamAudioSource.Stop();
		}
		streamCoroutine = null;
	}

	private void RemoveAlarmBubble()
	{
		if (streamCoroutine != null)
		{
			StopCoroutine(streamCoroutine);
			streamCoroutine = null;
		}
		if (alarmBubble != null)
		{
			alarmBubble.Destroy();
			alarmBubble = null;
		}
		if (streamAudioSource != null && streamAudioSource.isPlaying)
		{
			streamAudioSource.Stop();
		}
	}

	private IEnumerator ShowAlarmBubbleStreamedDelayed()
	{
		yield return new WaitForSeconds(3f);
		ShowAlarmBubbleStreamed();
	}

	private SaveLoadHandler.SettingsData.AlarmEntry GetNextAlarm(out DateTime nextTime)
	{
		nextTime = DateTime.MinValue;
		SaveLoadHandler.SettingsData settingsData = SaveLoadHandler.Instance?.data;
		if (settingsData == null || !settingsData.alarmsEnabled || settingsData.alarms == null || settingsData.alarms.Count == 0)
		{
			return null;
		}
		DateTime now = DateTime.Now;
		SaveLoadHandler.SettingsData.AlarmEntry alarmEntry = null;
		DateTime dateTime = DateTime.MaxValue;
		for (int i = 0; i < settingsData.alarms.Count; i++)
		{
			SaveLoadHandler.SettingsData.AlarmEntry alarmEntry2 = settingsData.alarms[i];
			if (alarmEntry2 != null && alarmEntry2.enabled)
			{
				DateTime dateTime2 = ComputeNextTime(alarmEntry2, now);
				if (dateTime2 < dateTime)
				{
					dateTime = dateTime2;
					alarmEntry = alarmEntry2;
				}
			}
		}
		if (alarmEntry != null)
		{
			nextTime = dateTime;
		}
		return alarmEntry;
	}

	private void EnqueueOrTrigger(string text)
	{
		if (alarmActive)
		{
			pendingEvents.Enqueue(string.IsNullOrEmpty(text) ? "Alarm" : text);
			return;
		}
		alarmText = (string.IsNullOrEmpty(text) ? "Alarm" : text);
		TriggerAlarmNow();
	}

	private void CheckTimers()
	{
		SaveLoadHandler.SettingsData settingsData = SaveLoadHandler.Instance?.data;
		if (settingsData == null || !settingsData.alarmsEnabled || settingsData.timers == null || settingsData.timers.Count == 0)
		{
			return;
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		for (int i = 0; i < settingsData.timers.Count; i++)
		{
			SaveLoadHandler.SettingsData.TimerEntry timerEntry = settingsData.timers[i];
			if (timerEntry != null && timerEntry.enabled && timerEntry.running && timerEntry.targetUnix > 0 && num >= timerEntry.targetUnix)
			{
				timerEntry.running = false;
				timerEntry.targetUnix = 0L;
				SaveLoadHandler.Instance.SaveToDisk();
				string text = (string.IsNullOrEmpty(timerEntry.text) ? "Timer" : timerEntry.text);
				EnqueueOrTrigger(text);
			}
		}
	}

	private DateTime ComputeNextTime(SaveLoadHandler.SettingsData.AlarmEntry a, DateTime now)
	{
		if (a == null)
		{
			return DateTime.MaxValue;
		}
		if (a.daysMask == 0)
		{
			DateTime dateTime = new DateTime(now.Year, now.Month, now.Day, Mathf.Clamp(a.hour, 0, 23), Mathf.Clamp(a.minute, 0, 59), 0);
			if (dateTime <= now)
			{
				return dateTime.AddDays(1.0);
			}
			return dateTime;
		}
		int num = (int)((now.DayOfWeek == DayOfWeek.Sunday) ? DayOfWeek.Saturday : (now.DayOfWeek - 1));
		for (int i = 0; i < 7; i++)
		{
			int num2 = (num + i) % 7;
			if ((a.daysMask & (1 << num2)) != 0)
			{
				DateTime dateTime2 = new DateTime(now.Year, now.Month, now.Day, Mathf.Clamp(a.hour, 0, 23), Mathf.Clamp(a.minute, 0, 59), 0).AddDays(i);
				if (i != 0 || !(dateTime2 <= now))
				{
					return dateTime2;
				}
			}
		}
		return now.AddDays(7.0);
	}
}
