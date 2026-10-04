using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LLMUnitySamples;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class AvatarMinecraftMessages : MonoBehaviour
{
	[Serializable]
	public enum McEventType
	{
		Entity = 0,
		DayStart = 1,
		NightStart = 2,
		LowHealth = 3,
		LowHunger = 4,
		Death = 5,
		RainStart = 6,
		Drowning = 7,
		Sleep = 8,
		Crafting = 9,
		Eat = 10,
		KillConfirm = 11,
		BiomeDiscovery = 12
	}

	[Serializable]
	public class McMessageEntry
	{
		public string locKey;

		[TextArea]
		public string text;

		public McEventType type;
	}

	[Serializable]
	private class ProxEvent
	{
		public string type;

		public string phase;

		public string uuid;

		public string id;

		public string name;

		public float distance;

		public long ts;

		public string player;

		public string biome;
	}

	[Header("Toggle")]
	public bool enableMinecraftMessages = true;

	[Header("Networking")]
	public int port = 32145;

	public bool debugLog = true;

	[Header("Bubble")]
	public Material bubbleMaterial;

	public string localizationTable = "Languages (UI)";

	[Range(1f, 60f)]
	public int despawnTime = 10;

	[HideInInspector]
	public List<AvatarMessage> messages = new List<AvatarMessage>();

	[Header("MC Messages")]
	public List<McMessageEntry> mcMessages = new List<McMessageEntry>();

	public Transform chatContainer;

	public Sprite bubbleSprite;

	public Color bubbleColor = new Color32(120, 120, 255, 255);

	public Color fontColor = Color.white;

	public Font font;

	public int fontSize = 16;

	public int bubbleWidth = 600;

	public float textPadding = 10f;

	public float bubbleSpacing = 10f;

	[Range(5f, 100f)]
	public int streamSpeed = 35;

	[Header("Gating")]
	public string[] allowedStates = new string[1] { "Idle" };

	public bool requireAllowedStates = true;

	public List<GameObject> blockObjects = new List<GameObject>();

	public bool useBlockObjects = true;

	public AudioSource streamAudioSource;

	private Bubble activeBubble;

	private Coroutine streamCoroutine;

	private Coroutine despawnCoroutine;

	private Animator avatarAnimator;

	private UdpClient client;

	private Thread thread;

	private volatile bool run;

	private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

	private System.Random rng = new System.Random();

	private void Start()
	{
		avatarAnimator = GetComponent<Animator>();
	}

	private string InjectEntity(string text, string entity)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return text.Replace("<entity>", entity).Replace("<Entity>", entity).Replace("{entity}", entity)
			.Replace("{Entity}", entity);
	}

	private string InjectBiome(string text, string biome)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return text.Replace("<biome>", biome).Replace("<Biome>", biome).Replace("{biome}", biome)
			.Replace("{Biome}", biome);
	}

	private void OnEnable()
	{
		try
		{
			client = new UdpClient(port);
			client.Client.ReceiveTimeout = 1000;
			run = true;
			thread = new Thread(Listen)
			{
				IsBackground = true
			};
			thread.Start();
			if (debugLog)
			{
				Debug.Log("[AvatarMinecraftMessages] UDP ready on " + port);
			}
		}
		catch (Exception ex)
		{
			if (debugLog)
			{
				Debug.LogError("[AvatarMinecraftMessages] UDP bind failed: " + ex.Message);
			}
			SafeClose();
		}
	}

	private void OnDisable()
	{
		SafeClose();
	}

	private void SafeClose()
	{
		run = false;
		try
		{
			client?.Close();
		}
		catch
		{
		}
		client = null;
		try
		{
			thread?.Join(100);
		}
		catch
		{
		}
		thread = null;
	}

	private void Listen()
	{
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
		while (run)
		{
			try
			{
				if (client == null)
				{
					Thread.Sleep(50);
					continue;
				}
				byte[] bytes = client.Receive(ref remoteEP);
				string item = Encoding.UTF8.GetString(bytes);
				queue.Enqueue(item);
			}
			catch (SocketException)
			{
			}
			catch
			{
			}
		}
	}

	private void Update()
	{
		string result;
		while (queue.TryDequeue(out result))
		{
			if (!enableMinecraftMessages)
			{
				continue;
			}
			ProxEvent proxEvent = JsonUtility.FromJson<ProxEvent>(result);
			if (proxEvent == null)
			{
				continue;
			}
			if (proxEvent.type == "mob_proximity")
			{
				if (string.IsNullOrEmpty(proxEvent.phase) || !(proxEvent.phase != "enter"))
				{
					string text = ((!string.IsNullOrEmpty(proxEvent.name)) ? proxEvent.name : (string.IsNullOrEmpty(proxEvent.id) ? "entity" : proxEvent.id));
					if (debugLog)
					{
						Debug.Log("[AvatarMinecraftMessages] Event: " + text);
					}
					ShowEvent(McEventType.Entity, text);
				}
			}
			else if (proxEvent.type == "day_start" || proxEvent.type == "time_day")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: day_start");
				}
				ShowEvent(McEventType.DayStart, "");
			}
			else if (proxEvent.type == "night_start" || proxEvent.type == "time_night")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: night_start");
				}
				ShowEvent(McEventType.NightStart, "");
			}
			else if (proxEvent.type == "low_health")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: low_health");
				}
				ShowEvent(McEventType.LowHealth, "");
			}
			else if (proxEvent.type == "low_hunger")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: low_hunger");
				}
				ShowEvent(McEventType.LowHunger, "");
			}
			else if (proxEvent.type == "death" || proxEvent.type == "player_death" || proxEvent.type == "you_died")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: death");
				}
				ShowEvent(McEventType.Death, "");
			}
			else if (proxEvent.type == "rain_start" || proxEvent.type == "weather_rain_start" || proxEvent.type == "rain")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: rain_start");
				}
				ShowEvent(McEventType.RainStart, "");
			}
			else if (proxEvent.type == "drowning" || proxEvent.type == "low_air" || proxEvent.type == "air_low")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: drowning");
				}
				ShowEvent(McEventType.Drowning, "");
			}
			else if (proxEvent.type == "sleep" || proxEvent.type == "sleep_start" || proxEvent.type == "player_sleep")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: sleep");
				}
				ShowEvent(McEventType.Sleep, "");
			}
			else if (proxEvent.type == "crafting" || proxEvent.type == "crafted" || proxEvent.type == "crafted_item")
			{
				if (rng.NextDouble() < 0.3)
				{
					if (debugLog)
					{
						Debug.Log("[AvatarMinecraftMessages] Event: crafting");
					}
					ShowEvent(McEventType.Crafting, "");
				}
			}
			else if (proxEvent.type == "eat")
			{
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: eat");
				}
				ShowEvent(McEventType.Eat, "");
			}
			else if (proxEvent.type == "kill_confirm")
			{
				string text2 = ((!string.IsNullOrEmpty(proxEvent.name)) ? proxEvent.name : (string.IsNullOrEmpty(proxEvent.id) ? "entity" : proxEvent.id));
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: kill_confirm " + text2);
				}
				ShowEvent(McEventType.KillConfirm, text2);
			}
			else if (proxEvent.type == "biome_discovery")
			{
				string text3 = (string.IsNullOrEmpty(proxEvent.biome) ? "" : proxEvent.biome);
				if (debugLog)
				{
					Debug.Log("[AvatarMinecraftMessages] Event: biome_discovery " + text3);
				}
				ShowEvent(McEventType.BiomeDiscovery, "", text3);
			}
		}
	}

	public void ShowEntityMessage(string entityDisplayName)
	{
		ShowEvent(McEventType.Entity, entityDisplayName);
	}

	private void ShowEvent(McEventType type, string entityDisplayName, string biomeName = "")
	{
		if (!enableMinecraftMessages)
		{
			return;
		}
		if (chatContainer == null)
		{
			if (debugLog)
			{
				Debug.LogWarning("[AvatarMinecraftMessages] No chatContainer");
			}
			return;
		}
		if (useBlockObjects && IsBlockedByObjects())
		{
			if (debugLog)
			{
				Debug.Log("[AvatarMinecraftMessages] Blocked by object");
			}
			return;
		}
		if (requireAllowedStates && !IsInAllowedState())
		{
			if (debugLog)
			{
				Debug.Log("[AvatarMinecraftMessages] State not allowed");
			}
			return;
		}
		string text = PickTextFor(type);
		if (string.IsNullOrEmpty(text))
		{
			AvatarMessage msg = null;
			if (messages != null && messages.Count > 0)
			{
				msg = messages[0];
			}
			text = ResolveText(msg);
			if (string.IsNullOrEmpty(text))
			{
				text = "there's a <entity> nearby... take care!";
			}
		}
		string text2 = text;
		text2 = InjectEntity(text2, entityDisplayName);
		text2 = InjectBiome(text2, biomeName);
		RemoveBubble();
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
		activeBubble = new Bubble(chatContainer, ui, "MinecraftBubble", "");
		Image[] componentsInChildren = activeBubble.GetRectTransform().GetComponentsInChildren<Image>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (bubbleMaterial != null)
			{
				componentsInChildren[i].material = bubbleMaterial;
			}
			componentsInChildren[i].pixelsPerUnitMultiplier = 0.25f;
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
		if (avatarAnimator != null)
		{
			avatarAnimator.SetBool("isTalking", value: true);
		}
		streamCoroutine = StartCoroutine(FakeStreamText(text2));
		if (despawnCoroutine != null)
		{
			StopCoroutine(despawnCoroutine);
		}
		despawnCoroutine = StartCoroutine(DespawnAfterDelay());
	}

	private string PickTextFor(McEventType type)
	{
		if (mcMessages == null || mcMessages.Count == 0)
		{
			return "";
		}
		List<McMessageEntry> list = new List<McMessageEntry>();
		for (int i = 0; i < mcMessages.Count; i++)
		{
			McMessageEntry mcMessageEntry = mcMessages[i];
			if (mcMessageEntry != null && mcMessageEntry.type == type)
			{
				list.Add(mcMessageEntry);
			}
		}
		if (list.Count == 0)
		{
			return "";
		}
		McMessageEntry mcMessageEntry2 = list[rng.Next(list.Count)];
		return ResolveText(mcMessageEntry2.locKey, mcMessageEntry2.text);
	}

	private string ResolveText(AvatarMessage msg)
	{
		if (msg != null && !string.IsNullOrEmpty(msg.locKey))
		{
			try
			{
				string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(localizationTable, msg.locKey, null, FallbackBehavior.UseProjectSettings);
				if (!string.IsNullOrEmpty(localizedString))
				{
					return localizedString;
				}
			}
			catch
			{
			}
		}
		if (msg != null && !string.IsNullOrEmpty(msg.text))
		{
			return msg.text;
		}
		return "";
	}

	private string ResolveText(string locKey, string fallback)
	{
		if (!string.IsNullOrEmpty(locKey))
		{
			try
			{
				string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(localizationTable, locKey, null, FallbackBehavior.UseProjectSettings);
				if (!string.IsNullOrEmpty(localizedString))
				{
					return localizedString;
				}
			}
			catch
			{
			}
		}
		if (!string.IsNullOrEmpty(fallback))
		{
			return fallback;
		}
		return "";
	}

	private IEnumerator FakeStreamText(string fullText)
	{
		if (activeBubble == null)
		{
			yield break;
		}
		activeBubble.SetText("");
		int length = 0;
		float delay = 1f / (float)Mathf.Max(streamSpeed, 1);
		while (length < fullText.Length)
		{
			length++;
			if (activeBubble == null)
			{
				yield break;
			}
			activeBubble.SetText(fullText.Substring(0, length));
			yield return new WaitForSeconds(delay);
		}
		if (activeBubble != null)
		{
			activeBubble.SetText(fullText);
		}
		if (streamAudioSource != null && streamAudioSource.isPlaying)
		{
			streamAudioSource.Stop();
		}
		if (avatarAnimator != null)
		{
			avatarAnimator.SetBool("isTalking", value: false);
		}
		streamCoroutine = null;
	}

	private IEnumerator DespawnAfterDelay()
	{
		yield return new WaitForSeconds(Mathf.Max(1, despawnTime));
		RemoveBubble();
	}

	private void RemoveBubble()
	{
		if (streamCoroutine != null)
		{
			StopCoroutine(streamCoroutine);
			streamCoroutine = null;
		}
		if (despawnCoroutine != null)
		{
			StopCoroutine(despawnCoroutine);
			despawnCoroutine = null;
		}
		if (activeBubble != null)
		{
			activeBubble.Destroy();
			activeBubble = null;
		}
		if (streamAudioSource != null && streamAudioSource.isPlaying)
		{
			streamAudioSource.Stop();
		}
		if (avatarAnimator != null)
		{
			avatarAnimator.SetBool("isTalking", value: false);
		}
	}

	private bool IsInAllowedState()
	{
		if (avatarAnimator == null || allowedStates == null || allowedStates.Length == 0)
		{
			return true;
		}
		AnimatorStateInfo currentAnimatorStateInfo = avatarAnimator.GetCurrentAnimatorStateInfo(0);
		for (int i = 0; i < allowedStates.Length; i++)
		{
			string value = allowedStates[i];
			if (!string.IsNullOrEmpty(value) && currentAnimatorStateInfo.IsName(value))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsBlockedByObjects()
	{
		if (blockObjects == null || blockObjects.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < blockObjects.Count; i++)
		{
			GameObject gameObject = blockObjects[i];
			if (gameObject != null && gameObject.activeInHierarchy)
			{
				return true;
			}
		}
		return false;
	}
}
