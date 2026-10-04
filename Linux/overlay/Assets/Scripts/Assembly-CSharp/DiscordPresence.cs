using System;
using System.Collections.Generic;
using DiscordRPC;
using UnityEngine;

public class DiscordPresence : MonoBehaviour
{
	public enum TimerMode
	{
		None = 0,
		StartNow = 1,
		FixedStartTime = 2
	}

	[Serializable]
	public class PresenceEntry
	{
		public string stateName;

		public string details;

		public string state;
	}

	[Header("Discord App Info")]
	public string appId = "123456789012345678";

	[Header("Default Text")]
	public string detailsLine = "Playing with my desktop pet";

	public string stateLine = "Just vibing";

	[Header("Timer")]
	public TimerMode timerMode = TimerMode.StartNow;

	public string fixedStartTimeISO = "2025-04-07T12:00:00Z";

	[Header("Button")]
	public string buttonLabel = "Visit Website";

	public string buttonUrl = "https://mateengine.com";

	[Header("Icons")]
	public string largeImageKey = "logo";

	public string largeImageText = "MateEngine";

	public string smallImageKey = "steam-icon";

	public string smallImageText = "Steam Edition";

	[Header("Model Root (VRMModel or CustomVRM must be child)")]
	public GameObject modelRoot;

	[Header("State-Based Overrides")]
	public List<PresenceEntry> presenceOverrides = new List<PresenceEntry>();

	private DiscordRpcClient client;

	private RichPresence presence;

	private string lastState = "";

	private Animator cachedAnimator;

	private bool wasRPCEnabled;

	private long gameStartTimestamp;

	private void Start()
	{
		wasRPCEnabled = SaveLoadHandler.Instance?.data.enableDiscordRPC ?? false;
		if (wasRPCEnabled)
		{
			InitGameStartTimestamp();
			client = new DiscordRpcClient(appId);
			client.Initialize();
			ResolveAnimator();
			UpdatePresence(force: true);
		}
	}

	private void Update()
	{
		bool flag = SaveLoadHandler.Instance?.data.enableDiscordRPC ?? false;
		if (flag != wasRPCEnabled)
		{
			wasRPCEnabled = flag;
			if (flag)
			{
				InitGameStartTimestamp();
				client = new DiscordRpcClient(appId);
				client.Initialize();
				ResolveAnimator();
				UpdatePresence(force: true);
				Debug.Log("[DiscordPresence] Enabled and client initialized at runtime.");
			}
			else if (client != null)
			{
				client.ClearPresence();
				client.Dispose();
				client = null;
				Debug.Log("[DiscordPresence] Disabled and client disposed at runtime.");
			}
		}
		if (!wasRPCEnabled || client == null)
		{
			return;
		}
		if (cachedAnimator == null)
		{
			ResolveAnimator();
			if (cachedAnimator == null)
			{
				return;
			}
		}
		UpdatePresence();
	}

	private void InitGameStartTimestamp()
	{
		if (timerMode == TimerMode.FixedStartTime)
		{
			if (DateTimeOffset.TryParse(fixedStartTimeISO, out var result))
			{
				gameStartTimestamp = result.ToUnixTimeMilliseconds();
				return;
			}
			Debug.LogWarning("[DiscordPresence] Invalid fixed timestamp. Using now.");
			gameStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}
		else if (timerMode == TimerMode.StartNow)
		{
			if (gameStartTimestamp == 0L)
			{
				gameStartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			}
		}
		else
		{
			gameStartTimestamp = 0L;
		}
	}

	private void ResolveAnimator()
	{
		if (modelRoot == null)
		{
			Debug.LogWarning("[DiscordPresence] ModelRoot not assigned.");
			return;
		}
		Transform transform = modelRoot.transform.Find("CustomVRM(Clone)");
		if (transform == null || !transform.gameObject.activeInHierarchy)
		{
			transform = modelRoot.transform.Find("VRMModel");
		}
		if (transform != null && transform.gameObject.activeInHierarchy)
		{
			cachedAnimator = transform.GetComponent<Animator>();
		}
	}

	private void UpdatePresence(bool force = false)
	{
		if (cachedAnimator == null)
		{
			return;
		}
		string currentAnimatorState = GetCurrentAnimatorState();
		if (!force && currentAnimatorState == lastState)
		{
			return;
		}
		lastState = currentAnimatorState;
		string details = detailsLine;
		string state = stateLine;
		for (int i = 0; i < presenceOverrides.Count; i++)
		{
			PresenceEntry presenceEntry = presenceOverrides[i];
			if (currentAnimatorState == presenceEntry.stateName)
			{
				if (!string.IsNullOrEmpty(presenceEntry.details))
				{
					details = presenceEntry.details;
				}
				if (!string.IsNullOrEmpty(presenceEntry.state))
				{
					state = presenceEntry.state;
				}
				break;
			}
		}
		presence = new RichPresence
		{
			Details = details,
			State = state,
			Assets = new Assets
			{
				LargeImageKey = largeImageKey,
				LargeImageText = largeImageText,
				SmallImageKey = smallImageKey,
				SmallImageText = smallImageText
			}
		};
		if (timerMode != TimerMode.None)
		{
			presence.Timestamps = new Timestamps
			{
				StartUnixMilliseconds = (ulong)gameStartTimestamp
			};
		}
		if (!string.IsNullOrEmpty(buttonLabel) && !string.IsNullOrEmpty(buttonUrl))
		{
			presence.Buttons = new Button[1]
			{
				new Button
				{
					Label = buttonLabel,
					Url = buttonUrl
				}
			};
		}
		client.SetPresence(presence);
		Debug.Log("[DiscordPresence] Updated to state: " + currentAnimatorState + " → " + details + " / " + state);
	}

	private string GetCurrentAnimatorState()
	{
		if (cachedAnimator == null)
		{
			return "";
		}
		AnimatorStateInfo currentAnimatorStateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
		if (cachedAnimator.IsInTransition(0))
		{
			return "";
		}
		for (int i = 0; i < presenceOverrides.Count; i++)
		{
			string stateName = presenceOverrides[i].stateName;
			if (currentAnimatorStateInfo.IsName(stateName))
			{
				return stateName;
			}
		}
		return "";
	}

	private void OnApplicationQuit()
	{
		try
		{
			if (client != null)
			{
				client.ClearPresence();
				client.Dispose();
				client = null;
				Debug.Log("[DiscordPresence] Presence cleared and client disposed.");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[DiscordPresence] Error during Discord shutdown: " + ex);
		}
	}
}
