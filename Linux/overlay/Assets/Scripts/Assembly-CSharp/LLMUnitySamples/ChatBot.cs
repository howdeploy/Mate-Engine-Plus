using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class ChatBot : MonoBehaviour
	{
		[Header("Containers")]
		public Transform chatContainer;

		public Transform inputContainer;

		[Header("Colors & Font")]
		public Color playerColor = new Color32(81, 164, 81, 255);

		public Color aiColor = new Color32(29, 29, 73, 255);

		public Color fontColor = Color.white;

		public Font font;

		public int fontSize = 16;

		[Header("Bubble Layout")]
		public int bubbleWidth = 600;

		public float textPadding = 10f;

		public float bubbleSpacing = 10f;

		public float bottomPadding = 10f;

		public Sprite sprite;

		public Sprite roundedSprite16;

		public Sprite roundedSprite32;

		public Sprite roundedSprite64;

		[Header("LLM")]
		public LLMCharacter llmCharacter;

		[Header("Input Settings")]
		public string inputPlaceholder = "Message me";

		[Header("Streaming Audio")]
		public AudioSource streamAudioSource;

		[Header("Bubble Materials")]
		public Material playerMaterial;

		public Material aiMaterial;

		[Header("Text Materials")]
		public Material playerTextMaterial;

		public Material aiTextMaterial;

		[Header("Scroll")]
		public ScrollRect scrollRect;

		public bool autoScrollOnNewMessage = true;

		public bool respectUserScroll = true;

		[Header("History")]
		[Min(0f)]
		public int maxMessages = 100;

		public bool trimOnlyWhenAtBottom = true;

		public bool enableOffscreenTrim;

		[Header("Font Colors (per side)")]
		public Color playerFontColor = Color.white;

		public Color aiFontColor = Color.white;

		[Header("Rounded Sprite Radius")]
		[Range(0f, 64f)]
		public int cornerRadius = 16;

		private bool layoutDirty;

		private InputBubble inputBubble;

		private List<Bubble> chatBubbles = new List<Bubble>();

		private bool blockInput = true;

		private BubbleUI playerUI;

		private BubbleUI aiUI;

		private bool warmUpDone;

		private int lastBubbleOutsideFOV = -1;

		private Animator avatarAnimator;

		private Animator lastAvatarAnimator;

		private static readonly int isTalkingHash = Animator.StringToHash("isTalking");

		private bool onValidateWarning = true;

		private void Start()
		{
			avatarAnimator = GetComponent<Animator>();
			if (font == null)
			{
				font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			}
			if (cornerRadius <= 16)
			{
				sprite = roundedSprite16;
			}
			else if (cornerRadius <= 32)
			{
				sprite = roundedSprite32;
			}
			else
			{
				sprite = roundedSprite64;
			}
			playerUI = new BubbleUI
			{
				sprite = sprite,
				font = font,
				fontSize = fontSize,
				fontColor = playerFontColor,
				bubbleColor = playerColor,
				bottomPosition = 0f,
				leftPosition = 0f,
				textPadding = textPadding,
				bubbleOffset = bubbleSpacing,
				bubbleWidth = bubbleWidth,
				bubbleHeight = -1f
			};
			aiUI = new BubbleUI
			{
				sprite = sprite,
				font = font,
				fontSize = fontSize,
				fontColor = aiFontColor,
				bubbleColor = aiColor,
				bottomPosition = 0f,
				leftPosition = 1f,
				textPadding = textPadding,
				bubbleOffset = bubbleSpacing,
				bubbleWidth = bubbleWidth,
				bubbleHeight = -1f
			};
			Transform parent = ((inputContainer != null) ? inputContainer : chatContainer);
			inputBubble = new InputBubble(parent, playerUI, "InputBubble", "Loading...");
			inputBubble.AddSubmitListener(onInputFieldSubmit);
			inputBubble.AddValueChangedListener(onValueChanged);
			inputBubble.setInteractable(interactable: false);
			ShowLoadedMessages();
			llmCharacter.Warmup(WarmUpCallback);
			FindAvatarSmart();
		}

		private void FindAvatarSmart()
		{
			Animator animator = null;
			VRMLoader vRMLoader = Object.FindFirstObjectByType<VRMLoader>();
			if (vRMLoader != null)
			{
				GameObject currentModel = vRMLoader.GetCurrentModel();
				if (currentModel != null)
				{
					animator = currentModel.GetComponentsInChildren<Animator>(includeInactive: true).FirstOrDefault((Animator a) => (bool)a && a.gameObject.activeInHierarchy);
				}
			}
			if (animator == null)
			{
				GameObject gameObject = GameObject.Find("Model");
				if (gameObject != null)
				{
					animator = gameObject.GetComponentsInChildren<Animator>(includeInactive: true).FirstOrDefault((Animator a) => (bool)a && a.gameObject.activeInHierarchy);
				}
			}
			if (animator == null)
			{
				animator = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault((Animator a) => (bool)a && a.isActiveAndEnabled);
			}
			if (animator != avatarAnimator)
			{
				avatarAnimator = animator;
				lastAvatarAnimator = avatarAnimator;
			}
		}

		private void RefreshAvatarIfChanged()
		{
			if (avatarAnimator == null || lastAvatarAnimator == null || avatarAnimator != lastAvatarAnimator)
			{
				FindAvatarSmart();
			}
		}

		private void MarkLayoutDirty()
		{
			layoutDirty = true;
		}

		private void OnDisable()
		{
			if (streamAudioSource != null && streamAudioSource.isPlaying)
			{
				streamAudioSource.Stop();
				streamAudioSource.volume = 1f;
			}
			if (avatarAnimator != null)
			{
				avatarAnimator.SetBool(isTalkingHash, value: false);
			}
		}

		private Bubble AddBubble(string message, bool isPlayerMessage)
		{
			Bubble bubble = new Bubble(chatContainer, isPlayerMessage ? playerUI : aiUI, isPlayerMessage ? "PlayerBubble" : "AIBubble", message);
			chatBubbles.Add(bubble);
			bubble.OnResize(MarkLayoutDirty);
			Image componentInChildren = bubble.GetRectTransform().GetComponentInChildren<Image>(includeInactive: true);
			if (componentInChildren != null)
			{
				componentInChildren.material = (isPlayerMessage ? playerMaterial : aiMaterial);
			}
			Text componentInChildren2 = bubble.GetRectTransform().GetComponentInChildren<Text>(includeInactive: true);
			if (componentInChildren2 != null)
			{
				Material material = (isPlayerMessage ? playerTextMaterial : aiTextMaterial);
				if (material != null)
				{
					componentInChildren2.material = material;
				}
			}
			if (autoScrollOnNewMessage && (!respectUserScroll || IsAtBottom()))
			{
				StartCoroutine(ScrollToBottomNextFrame());
			}
			TrimHistoryIfNeeded();
			return bubble;
		}

		private void TrimHistoryIfNeeded()
		{
			if (maxMessages > 0 && chatBubbles.Count > maxMessages && (!trimOnlyWhenAtBottom || IsAtBottom()))
			{
				int num = chatBubbles.Count - maxMessages;
				for (int i = 0; i < num; i++)
				{
					chatBubbles[i].Destroy();
				}
				chatBubbles.RemoveRange(0, num);
				UpdateBubblePositions();
			}
		}

		private bool IsAtBottom(float tolerance = 0.01f)
		{
			if (scrollRect == null)
			{
				return true;
			}
			return scrollRect.verticalNormalizedPosition <= tolerance;
		}

		private void ShowLoadedMessages()
		{
			int num = 1;
			int count = llmCharacter.chat.Count;
			if (maxMessages > 0)
			{
				num = Mathf.Max(1, count - maxMessages);
			}
			for (int i = num; i < count; i++)
			{
				AddBubble(llmCharacter.chat[i].content, i % 2 == 1);
			}
			StartCoroutine(ScrollToBottomNextFrame());
		}

		private void onInputFieldSubmit(string newText)
		{
			inputBubble.ActivateInputField();
			if (blockInput || newText.Trim() == "" || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				StartCoroutine(BlockInteraction());
				return;
			}
			blockInput = true;
			string text = inputBubble.GetText().Replace("\v", "\n");
			AddBubble(text, isPlayerMessage: true);
			Bubble aiBubble = AddBubble("...", isPlayerMessage: false);
			if (streamAudioSource != null)
			{
				streamAudioSource.Play();
			}
			if (avatarAnimator != null)
			{
				avatarAnimator.SetBool(isTalkingHash, value: true);
			}
			llmCharacter.Chat(text, delegate(string partial)
			{
				aiBubble.SetText(partial);
				layoutDirty = true;
			}, delegate
			{
				if (avatarAnimator != null)
				{
					avatarAnimator.SetBool(isTalkingHash, value: false);
				}
				aiBubble.SetText(aiBubble.GetText());
				layoutDirty = true;
				if (streamAudioSource != null && streamAudioSource.isPlaying)
				{
					StartCoroutine(FadeOutStreamAudio());
				}
				AllowInput();
			});
			inputBubble.SetText("");
		}

		private IEnumerator FadeOutStreamAudio(float duration = 0.5f)
		{
			float startVolume = streamAudioSource.volume;
			while (streamAudioSource.volume > 0f)
			{
				streamAudioSource.volume -= startVolume * Time.deltaTime / duration;
				yield return null;
			}
			streamAudioSource.Stop();
			streamAudioSource.volume = startVolume;
		}

		public void WarmUpCallback()
		{
			warmUpDone = true;
			inputBubble.SetPlaceHolderText(inputPlaceholder);
			AllowInput();
		}

		public void AllowInput()
		{
			blockInput = false;
			inputBubble.ReActivateInputField();
		}

		public void CancelRequests()
		{
			llmCharacter.CancelRequests();
			AllowInput();
		}

		private IEnumerator<string> BlockInteraction()
		{
			inputBubble.setInteractable(interactable: false);
			yield return null;
			inputBubble.setInteractable(interactable: true);
			inputBubble.MoveTextEnd();
		}

		private void onValueChanged(string newText)
		{
			if (Input.GetKey(KeyCode.Return) && inputBubble.GetText().Trim() == "")
			{
				inputBubble.SetText("");
			}
		}

		public void UpdateBubblePositions()
		{
			float num = bottomPadding;
			for (int num2 = chatBubbles.Count - 1; num2 >= 0; num2--)
			{
				Bubble bubble = chatBubbles[num2];
				RectTransform rectTransform = bubble.GetRectTransform();
				rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, num);
				if (enableOffscreenTrim)
				{
					float height = chatContainer.GetComponent<RectTransform>().rect.height;
					if (num > height && lastBubbleOutsideFOV == -1)
					{
						lastBubbleOutsideFOV = num2;
					}
				}
				num += bubble.GetSize().y + bubbleSpacing;
			}
			chatContainer.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num + bottomPadding);
		}

		private void Update()
		{
			RefreshAvatarIfChanged();
			if (!inputBubble.inputFocused() && warmUpDone)
			{
				inputBubble.ActivateInputField();
				StartCoroutine(BlockInteraction());
			}
			if (enableOffscreenTrim && lastBubbleOutsideFOV != -1)
			{
				for (int i = 0; i <= lastBubbleOutsideFOV; i++)
				{
					chatBubbles[i].Destroy();
				}
				chatBubbles.RemoveRange(0, lastBubbleOutsideFOV + 1);
				lastBubbleOutsideFOV = -1;
				UpdateBubblePositions();
			}
		}

		public void ExitGame()
		{
			Debug.Log("Exit button clicked");
			Application.Quit();
		}

		private IEnumerator ScrollToBottomNextFrame()
		{
			yield return null;
			Canvas.ForceUpdateCanvases();
			if (scrollRect != null)
			{
				scrollRect.verticalNormalizedPosition = 0f;
			}
		}

		private void OnValidate()
		{
			if (cornerRadius <= 16)
			{
				sprite = roundedSprite16;
			}
			else if (cornerRadius <= 32)
			{
				sprite = roundedSprite32;
			}
			else
			{
				sprite = roundedSprite64;
			}
			if (onValidateWarning && llmCharacter != null && !llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
			{
				Debug.LogWarning("Please select a model in the " + llmCharacter.llm.gameObject.name + " GameObject!");
				onValidateWarning = false;
			}
		}

		private void LateUpdate()
		{
			if (layoutDirty)
			{
				layoutDirty = false;
				UpdateBubblePositions();
				if (autoScrollOnNewMessage && (!respectUserScroll || IsAtBottom()) && scrollRect != null)
				{
					scrollRect.verticalNormalizedPosition = 0f;
				}
			}
		}
	}
}
