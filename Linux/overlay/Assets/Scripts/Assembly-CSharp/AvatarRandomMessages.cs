using System.Collections;
using System.Collections.Generic;
using LLMUnitySamples;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class AvatarRandomMessages : MonoBehaviour
{
	[Header("Bubble Material")]
	public Material bubbleMaterial;

	[Header("Localization")]
	public string localizationTable = "Languages (UI)";

	public bool enableRandomMessages = true;

	[Range(5f, 60f)]
	public int minDelay = 10;

	[Range(5f, 60f)]
	public int maxDelay = 60;

	[Range(5f, 20f)]
	public int despawnTime = 10;

	[Range(0f, 100f)]
	public int onActiveChance = 100;

	public List<AvatarMessage> messages = new List<AvatarMessage>();

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

	public AudioSource streamAudioSource;

	public bool useAllowedStatesWhitelist;

	public string[] allowedStates = new string[1] { "Idle" };

	public List<GameObject> blockObjects = new List<GameObject>();

	[SerializeField]
	private string inspectorEvent;

	private Bubble activeBubble;

	private Coroutine streamCoroutine;

	private Coroutine despawnCoroutine;

	private Coroutine loopCoroutine;

	private bool isBubbleActive;

	private Animator avatarAnimator;

	private string lastAnimatorStateName = "";

	private static readonly int isMaleHash = Animator.StringToHash("isMale");

	private void Start()
	{
		avatarAnimator = GetComponent<Animator>();
		enableRandomMessages = GetGlobalEnabled();
		ApplyEnableStateImmediate(enableRandomMessages);
	}

	private void Update()
	{
		bool globalEnabled = GetGlobalEnabled();
		if (globalEnabled != enableRandomMessages)
		{
			enableRandomMessages = globalEnabled;
			ApplyEnableStateImmediate(enableRandomMessages);
		}
		if (!enableRandomMessages)
		{
			return;
		}
		if (isBubbleActive)
		{
			if (IsBlockedByObjects())
			{
				inspectorEvent = "Bubble removed (blocked)";
				RemoveBubble();
			}
			else if (useAllowedStatesWhitelist && !IsInAllowedState())
			{
				inspectorEvent = "Bubble removed (state not allowed)";
				RemoveBubble();
			}
		}
		if (!(avatarAnimator != null))
		{
			return;
		}
		AnimatorStateInfo current = avatarAnimator.GetCurrentAnimatorStateInfo(0);
		string currentStateName = GetCurrentStateName(current);
		if (currentStateName != lastAnimatorStateName)
		{
			List<AvatarMessage> list = messages.FindAll((AvatarMessage m) => m.onActive && !string.IsNullOrEmpty(m.state) && current.IsName(m.state) && IsMessageAllowedByGender(m));
			if (list.Count > 0 && Random.Range(0, 100) < onActiveChance)
			{
				AvatarMessage msg = list[Random.Range(0, list.Count)];
				ShowSpecificMessage(msg);
			}
			lastAnimatorStateName = currentStateName;
		}
	}

	private bool GetGlobalEnabled()
	{
		if (!(SaveLoadHandler.Instance != null))
		{
			return enableRandomMessages;
		}
		return SaveLoadHandler.Instance.data.enableRandomMessages;
	}

	private void ApplyEnableStateImmediate(bool enabled)
	{
		if (enabled)
		{
			if (loopCoroutine == null)
			{
				loopCoroutine = StartCoroutine(RandomMessageLoop());
			}
			return;
		}
		if (loopCoroutine != null)
		{
			StopCoroutine(loopCoroutine);
			loopCoroutine = null;
		}
		RemoveBubble();
	}

	private IEnumerator RandomMessageLoop()
	{
		while (true)
		{
			if (!enableRandomMessages)
			{
				yield return null;
			}
			else if (!isBubbleActive && messages.Count > 0)
			{
				float seconds = Random.Range(minDelay, maxDelay + 1);
				yield return new WaitForSeconds(seconds);
				if (enableRandomMessages)
				{
					if (IsBlockedByObjects())
					{
						yield return new WaitForSeconds(1f);
					}
					else if (useAllowedStatesWhitelist && !IsInAllowedState())
					{
						yield return new WaitForSeconds(1f);
					}
					else
					{
						ShowRandomMessage();
					}
				}
			}
			else
			{
				yield return null;
			}
		}
	}

	private void ShowRandomMessage()
	{
		if (enableRandomMessages)
		{
			List<AvatarMessage> list = messages.FindAll((AvatarMessage m) => !m.onActive && IsMessageAllowedByGender(m));
			if (list.Count != 0)
			{
				ShowSpecificMessage(list[Random.Range(0, list.Count)]);
			}
		}
	}

	private void ShowSpecificMessage(AvatarMessage msg)
	{
		if (enableRandomMessages && !(chatContainer == null) && msg != null)
		{
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
			string fullText = ResolveText(msg);
			activeBubble = new Bubble(chatContainer, ui, "RandomBubble", "");
			Image componentInChildren = activeBubble.GetRectTransform().GetComponentInChildren<Image>(includeInactive: true);
			if (componentInChildren != null && bubbleMaterial != null)
			{
				componentInChildren.material = bubbleMaterial;
			}
			isBubbleActive = true;
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
			streamCoroutine = StartCoroutine(FakeStreamText(fullText));
			if (despawnCoroutine != null)
			{
				StopCoroutine(despawnCoroutine);
			}
			despawnCoroutine = StartCoroutine(DespawnAfterDelay());
		}
	}

	private string ResolveText(AvatarMessage msg)
	{
		if (!string.IsNullOrEmpty(msg.locKey))
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
		return msg.text;
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
			if (!enableRandomMessages)
			{
				yield break;
			}
			length++;
			activeBubble.SetText(fullText.Substring(0, length));
			yield return new WaitForSeconds(delay);
			if (activeBubble == null)
			{
				yield break;
			}
		}
		activeBubble.SetText(fullText);
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
		float t = 0f;
		while (t < (float)despawnTime)
		{
			if (!enableRandomMessages)
			{
				yield break;
			}
			t += Time.deltaTime;
			yield return null;
		}
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
		isBubbleActive = false;
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

	private bool IsBlockedByObjects()
	{
		if (blockObjects == null || blockObjects.Count == 0)
		{
			return false;
		}
		foreach (GameObject blockObject in blockObjects)
		{
			if (blockObject != null && blockObject.activeInHierarchy)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsMessageAllowedByGender(AvatarMessage msg)
	{
		if (avatarAnimator == null)
		{
			return true;
		}
		if (!HasParam(isMaleHash))
		{
			return true;
		}
		bool flag = avatarAnimator.GetFloat(isMaleHash) > 0.5f;
		if (!msg.isHusbando)
		{
			return !flag;
		}
		return flag;
	}

	private bool HasParam(int hash)
	{
		AnimatorControllerParameter[] parameters = avatarAnimator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].nameHash == hash)
			{
				return true;
			}
		}
		return false;
	}

	private string GetCurrentStateName(AnimatorStateInfo stateInfo)
	{
		if (avatarAnimator == null)
		{
			return "";
		}
		AnimatorClipInfo[] currentAnimatorClipInfo = avatarAnimator.GetCurrentAnimatorClipInfo(0);
		if (currentAnimatorClipInfo.Length != 0 && currentAnimatorClipInfo[0].clip != null)
		{
			return currentAnimatorClipInfo[0].clip.name;
		}
		if (!stateInfo.IsName(""))
		{
			return stateInfo.shortNameHash.ToString();
		}
		return "";
	}
}
