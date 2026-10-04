using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomDancePlayer
{
	public class AvatarDanceHandler : MonoBehaviour
	{
		private class BusCmd
		{
			public int v;

			public string cmd;

			public string sid;

			public string title;

			public int index;

			public double atUtc;

			public double writeUtc;
		}

		private class DanceEntry
		{
			public string id;

			public string path;

			public string bundlePath;

			public AnimationClip clip;

			public AudioClip audio;

			public AssetBundle bundle;

			public bool fromME;

			public string extractedDir;

			public string author;

			public string stableId;
		}

		[Serializable]
		private class DanceMeta
		{
			public string songName;

			public string songAuthor;

			public string mmdAuthor;

			public float songLength;

			public string placeholderClipName;
		}

		private class PooledItem
		{
			public GameObject go;

			public TMP_Text titleTMP;

			public TMP_Text authorTMP;

			public Text titleFB;

			public Text authorFB;

			public Button button;
		}

		[Header("UI")]
		public Button playButton;

		public Button stopButton;

		public Button prevButton;

		public Button nextButton;

		public Slider progressSlider;

		public TMP_Text playingNowText;

		public TMP_Text playTimeText;

		public TMP_Text maxPlayTimeText;

		public TMP_Text authorText;

		[Header("UI Fallback")]
		public bool useFallbackFont;

		public Text playingNowFallbackText;

		public Text authorFallbackText;

		[Header("Sound")]
		public AudioSource audioSource;

		public Slider volumeSlider;

		[Header("Animator")]
		public string danceLayerName = "Dance Layer";

		public string danceStateName = "Custom Dance";

		public string placeholderClipName = "CUSTOM_DANCE";

		public string customDancingParam = "isCustomDancing";

		public string waitingParam = "isWaitingForDancing";

		[Header("List UI")]
		public Transform contentObject;

		public GameObject prefab;

		public Button songPlayButton;

		[Header("Sources")]
		public string streamingSubfolder = "CustomDances";

		public string modsFolderName = "Mods";

		[Header("Sync")]
		public bool enableSync = true;

		public string syncFileName = "avatar_dance_play_bus.json";

		public float pollInterval = 0.05f;

		public double leadSeconds = 1.5;

		private AnimationClip placeholderClipCached;

		private Coroutine playRoutine;

		private bool autoNextScheduled;

		private Animator animator;

		private Animator lastAnimator;

		private RuntimeAnimatorController defaultController;

		private AnimatorOverrideController overrideController;

		private int layerIndex = -1;

		private int stateHash;

		private int currentIndex = -1;

		private string defaultPlayingNowText = "";

		private string defaultPlayTimeText = "";

		private string defaultMaxPlayTimeText = "";

		private string defaultAuthorText = "";

		private const string unknownAuthorLabel = "Author: Unknown";

		private float currentTotalSeconds;

		private float playStartTime;

		private bool isPlaying;

		private List<int> filteredQueue;

		private bool holdDuringTransition;

		private bool pendingStop;

		private readonly HashSet<string> mmdBlendShapeNames = new HashSet<string>(new string[42]
		{
			"まばたき", "ウィンク", "ウィンク２", "ウィンク右", "笑い", "なごみ", "びっくり", "ジト目", "瞳小", "キリッ",
			"星目", "はぁと", "はちゅ目", "はっ", "ハイライト消し", "怒るいい子！", "あ", "い", "う", "え",
			"お", "えーん", "ん", "▲", "口", "ω口", "はんっ！", "にっこり", "にやり", "にやり２",
			"べろっ", "てへぺろ", "口角上げ", "口角下げ", "口横広げ", "真面目", "上下", "困る", "怒り", "照れ",
			"涙", "すぼめ"
		}, StringComparer.Ordinal);

		private readonly List<DanceEntry> entries = new List<DanceEntry>();

		private readonly Dictionary<string, DanceEntry> byId = new Dictionary<string, DanceEntry>(StringComparer.OrdinalIgnoreCase);

		private DanceEntry loadedEntry;

		private readonly List<PooledItem> uiPool = new List<PooledItem>();

		private string busPath;

		private int lastSeenV = -1;

		private Mutex leaderMutex;

		private bool isLeader;

		private Coroutine scheduledCo;

		private bool guardActive;

		private double guardUntilUtc;

		private bool animatorFrozen;

		private float animatorPrevSpeed = 1f;

		private readonly List<Button> tempDisabled = new List<Button>();

		private float storedSliderValue = -1f;

		private float storedAudioVolume = -1f;

		private bool followerMuted;

		public bool loopOn;

		public bool shuffleOn;

		public bool IsPlaying => isPlaying;

		private void Awake()
		{
			if (!useFallbackFont)
			{
				if (playingNowText != null)
				{
					defaultPlayingNowText = playingNowText.text;
				}
				if (authorText != null)
				{
					defaultAuthorText = (string.IsNullOrWhiteSpace(authorText.text) ? "Author: Unknown" : authorText.text);
				}
			}
			else
			{
				if (playingNowFallbackText != null)
				{
					defaultPlayingNowText = playingNowFallbackText.text;
				}
				if (authorFallbackText != null)
				{
					defaultAuthorText = (string.IsNullOrWhiteSpace(authorFallbackText.text) ? "Author: Unknown" : authorText.text);
				}
			}
			if (playTimeText != null)
			{
				defaultPlayTimeText = playTimeText.text;
			}
			if (maxPlayTimeText != null)
			{
				defaultMaxPlayTimeText = maxPlayTimeText.text;
			}
			BindUI();
			string text = Path.Combine(Application.persistentDataPath, "Sync");
			try
			{
				Directory.CreateDirectory(text);
			}
			catch
			{
			}
			busPath = Path.Combine(text, syncFileName);
			TryAcquireLeader();
		}

		private IEnumerator Start()
		{
			if (audioSource == null)
			{
				EnsureAudioSource();
			}
			yield return null;
			FindAvatarSmart();
			LoadAllSources();
			BuildListUI();
			if (entries.Count > 0 && currentIndex < 0)
			{
				currentIndex = 0;
			}
			UpdatePlayingNowLabel(null);
			UpdateAuthorLabel(null);
			UpdateTimeLabels(0f, 0f);
		}

		private void OnEnable()
		{
			if (enableSync)
			{
				StartCoroutine(Poll());
				StartCoroutine(LeaderAutoNextWatcher());
			}
		}

		private bool IsOnDanceState()
		{
			if (animator == null)
			{
				return false;
			}
			if (layerIndex < 0)
			{
				layerIndex = animator.GetLayerIndex(danceLayerName);
			}
			return animator.GetCurrentAnimatorStateInfo(layerIndex).shortNameHash == stateHash;
		}

		private bool IsFullyInWaiting()
		{
			if (animator == null)
			{
				return false;
			}
			if (layerIndex < 0)
			{
				layerIndex = animator.GetLayerIndex(danceLayerName);
			}
			if (!IsOnDanceState() && !animator.IsInTransition(layerIndex) && HasBool(waitingParam))
			{
				return animator.GetBool(waitingParam);
			}
			return false;
		}

		private void PauseAudio()
		{
			if (audioSource != null)
			{
				try
				{
					audioSource.Pause();
				}
				catch
				{
				}
			}
		}

		private void ResumeAudio()
		{
			if (audioSource != null && audioSource.clip != null)
			{
				try
				{
					audioSource.Play();
				}
				catch
				{
				}
			}
		}

		private void OnDisable()
		{
			if (scheduledCo != null)
			{
				StopCoroutine(scheduledCo);
				scheduledCo = null;
			}
			StopAllCoroutines();
			ReleaseLeader();
			UnfreezeAnimator();
			guardActive = false;
			ReenableAll();
		}

		private void Update()
		{
			RefreshAnimatorIfChanged();
			bool flag = animator != null && HasBool(customDancingParam) && animator.GetBool(customDancingParam);
			if (isPlaying && !flag && !holdDuringTransition)
			{
				StopAndUnload();
			}
			float length = currentTotalSeconds;
			float num = 0f;
			if (isPlaying)
			{
				if (audioSource != null && audioSource.clip != null)
				{
					length = audioSource.clip.length;
					num = audioSource.time;
					currentTotalSeconds = length;
				}
				else
				{
					num = Mathf.Clamp(Time.time - playStartTime, 0f, length);
				}
			}
			if (progressSlider != null && length > 0f)
			{
				progressSlider.value = Mathf.Clamp01(num / length);
			}
			else if (progressSlider != null)
			{
				progressSlider.value = 0f;
			}
			UpdateTimeLabels(num, length);
			if ((!enableSync || !isLeader) && isPlaying && length > 0f)
			{
				bool num2 = audioSource != null && audioSource.clip != null && !audioSource.loop && audioSource.time >= audioSource.clip.length - 0.05f;
				bool flag2 = num >= length - 0.05f;
				if (num2 || flag2)
				{
					TryAutoNext();
				}
			}
			if (guardActive)
			{
				if (UtcNow() < guardUntilUtc)
				{
					EnforceHold();
				}
				else
				{
					guardActive = false;
				}
			}
		}

		private void BindUI()
		{
			if (playButton != null)
			{
				playButton.onClick.AddListener(OnPlayClicked);
			}
			if (stopButton != null)
			{
				stopButton.onClick.AddListener(OnStopClicked);
			}
			if (prevButton != null)
			{
				prevButton.onClick.AddListener(OnPrevClicked);
			}
			if (nextButton != null)
			{
				nextButton.onClick.AddListener(OnNextClicked);
			}
			if (songPlayButton != null)
			{
				songPlayButton.onClick.AddListener(OnPlayClicked);
			}
		}

		private void OnPlayClicked()
		{
			SetWaiting(v: true);
			SetDancing(v: false);
			if (enableSync && isLeader)
			{
				int idx = ResolvePlayableIndex();
				DanceEntry e = ((idx >= 0 && idx < entries.Count) ? entries[idx] : null);
				double atUtc = UtcNow() + leadSeconds;
				guardActive = true;
				guardUntilUtc = atUtc;
				EnforceHold();
				ScheduleLocal(delegate
				{
					TryPlayByStableIdOrFallback((e != null) ? e.stableId : null, idx, (e != null) ? e.id : null);
				}, atUtc);
				Broadcast("PlayByStableId", (e != null) ? e.stableId : null, idx, (e != null) ? e.id : null, atUtc);
			}
			else
			{
				TryPlayCurrentOrFirst();
			}
		}

		private void OnListItemClicked(int idx)
		{
			SetWaiting(v: true);
			SetDancing(v: false);
			if (enableSync && isLeader)
			{
				DanceEntry e = ((idx >= 0 && idx < entries.Count) ? entries[idx] : null);
				double atUtc = UtcNow() + leadSeconds;
				guardActive = true;
				guardUntilUtc = atUtc;
				EnforceHold();
				ScheduleLocal(delegate
				{
					TryPlayByStableIdOrFallback((e != null) ? e.stableId : null, idx, (e != null) ? e.id : null);
				}, atUtc);
				Broadcast("PlayByStableId", (e != null) ? e.stableId : null, idx, (e != null) ? e.id : null, atUtc);
			}
			else
			{
				currentIndex = idx;
				PlayIndex(idx);
			}
		}

		private void OnNextClicked()
		{
			SetWaiting(v: true);
			SetDancing(v: false);
			int target = NextIndexForManual(forward: true);
			if (enableSync && isLeader)
			{
				DanceEntry e = ((target >= 0 && target < entries.Count) ? entries[target] : null);
				double atUtc = UtcNow() + leadSeconds;
				guardActive = true;
				guardUntilUtc = atUtc;
				EnforceHold();
				ScheduleLocal(delegate
				{
					TryPlayByStableIdOrFallback((e != null) ? e.stableId : null, target, (e != null) ? e.id : null);
				}, atUtc);
				Broadcast("PlayByStableId", (e != null) ? e.stableId : null, target, (e != null) ? e.id : null, atUtc);
			}
			else if (target >= 0)
			{
				PlayIndex(target);
			}
		}

		private void OnPrevClicked()
		{
			SetWaiting(v: true);
			SetDancing(v: false);
			int target = NextIndexForManual(forward: false);
			if (enableSync && isLeader)
			{
				DanceEntry e = ((target >= 0 && target < entries.Count) ? entries[target] : null);
				double atUtc = UtcNow() + leadSeconds;
				guardActive = true;
				guardUntilUtc = atUtc;
				EnforceHold();
				ScheduleLocal(delegate
				{
					TryPlayByStableIdOrFallback((e != null) ? e.stableId : null, target, (e != null) ? e.id : null);
				}, atUtc);
				Broadcast("PlayByStableId", (e != null) ? e.stableId : null, target, (e != null) ? e.id : null, atUtc);
			}
			else if (target >= 0)
			{
				PlayIndex(target);
			}
		}

		private void OnStopClicked()
		{
			SetDancing(v: false);
			SetWaiting(v: false);
			if (enableSync && isLeader)
			{
				double atUtc = UtcNow();
				ScheduleLocal(delegate
				{
					TryStopPlay();
				}, atUtc);
				Broadcast("StopPlay", null, -1, null, atUtc);
			}
			else
			{
				StopPlay();
			}
		}

		private int ResolvePlayableIndex()
		{
			if (filteredQueue != null && filteredQueue.Count > 0)
			{
				if (currentIndex >= 0 && filteredQueue.Contains(currentIndex))
				{
					return currentIndex;
				}
				return filteredQueue[0];
			}
			if (currentIndex >= 0)
			{
				return currentIndex;
			}
			return 0;
		}

		private void EnsureAudioSource()
		{
			GameObject gameObject = GameObject.Find("SoundFX");
			if (gameObject == null)
			{
				gameObject = new GameObject("SoundFX");
			}
			Transform transform = gameObject.transform.Find("CustomDanceAudio");
			GameObject gameObject2 = (transform ? transform.gameObject : new GameObject("CustomDanceAudio"));
			if (!transform)
			{
				gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			}
			audioSource = gameObject2.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = gameObject2.AddComponent<AudioSource>();
			}
			audioSource.playOnAwake = false;
			audioSource.loop = false;
			audioSource.spatialBlend = 0f;
			audioSource.volume = 0.25f;
		}

		private void FindAvatarSmart()
		{
			Animator animator = null;
			VRMLoader vRMLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
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
				animator = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault((Animator a) => (bool)a && a.isActiveAndEnabled);
			}
			if (animator != this.animator)
			{
				this.animator = animator;
				lastAnimator = this.animator;
				defaultController = ((this.animator != null) ? this.animator.runtimeAnimatorController : null);
				layerIndex = ((this.animator != null) ? this.animator.GetLayerIndex(danceLayerName) : (-1));
				stateHash = Animator.StringToHash(danceStateName);
				overrideController = null;
			}
		}

		private void RefreshAnimatorIfChanged()
		{
			if (animator == null || lastAnimator == null || animator != lastAnimator || animator.runtimeAnimatorController != defaultController)
			{
				FindAvatarSmart();
			}
		}

		private void LoadAllSources()
		{
			UnloadEntry(loadedEntry);
			loadedEntry = null;
			foreach (DanceEntry entry in entries)
			{
				try
				{
					entry.bundle?.Unload(unloadAllLoadedObjects: true);
				}
				catch
				{
				}
				entry.bundle = null;
				entry.clip = null;
				entry.audio = null;
			}
			entries.Clear();
			byId.Clear();
			List<string> list = new List<string>();
			string path = Path.Combine(Application.streamingAssetsPath, streamingSubfolder);
			if (Directory.Exists(path))
			{
				list.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
			}
			string path2 = Path.Combine(Application.persistentDataPath, modsFolderName);
			Directory.CreateDirectory(path2);
			list.AddRange(Directory.GetFiles(path2, "*", SearchOption.AllDirectories));
			for (int i = 0; i < list.Count; i++)
			{
				string text = list[i];
				string extension = Path.GetExtension(text);
				if (!string.IsNullOrEmpty(extension))
				{
					if (extension.Equals(".unity3d", StringComparison.OrdinalIgnoreCase))
					{
						TryAddUnity3D(text);
					}
					else if (extension.Equals(".me", StringComparison.OrdinalIgnoreCase))
					{
						TryAddME(text);
					}
				}
			}
			entries.Sort((DanceEntry a, DanceEntry b) => string.Compare(a.id, b.id, StringComparison.OrdinalIgnoreCase));
		}

		private void TryAddUnity3D(string path)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
			if (IsModEnabled(fileNameWithoutExtension) && !byId.ContainsKey(fileNameWithoutExtension))
			{
				DanceEntry danceEntry = new DanceEntry
				{
					id = fileNameWithoutExtension,
					path = path,
					bundlePath = path,
					clip = null,
					audio = null,
					bundle = null,
					fromME = false,
					extractedDir = null,
					author = "Author: Unknown"
				};
				entries.Add(danceEntry);
				byId[fileNameWithoutExtension] = danceEntry;
			}
		}

		private void TryAddME(string mePath)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(mePath);
			if (!IsModEnabled(fileNameWithoutExtension) || byId.ContainsKey(fileNameWithoutExtension))
			{
				return;
			}
			string text = Path.Combine(Application.temporaryCachePath, "ME_Cache");
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, fileNameWithoutExtension);
			bool flag = true;
			try
			{
				if (Directory.Exists(text2))
				{
					DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(mePath);
					if (Directory.GetLastWriteTimeUtc(text2) >= lastWriteTimeUtc)
					{
						flag = false;
					}
					else
					{
						Directory.Delete(text2, recursive: true);
					}
				}
				if (flag)
				{
					ZipFile.ExtractToDirectory(mePath, text2);
					Directory.SetLastWriteTimeUtc(text2, File.GetLastWriteTimeUtc(mePath));
				}
			}
			catch
			{
				return;
			}
			string text3 = Path.Combine(text2, "dance_meta.json");
			string text4 = Path.Combine(text2, "dance.json");
			if (!File.Exists(text3) && !File.Exists(text4))
			{
				return;
			}
			string text5 = Directory.GetFiles(text2, "*.bundle", SearchOption.AllDirectories).FirstOrDefault();
			if (string.IsNullOrEmpty(text5) || !File.Exists(text5))
			{
				return;
			}
			string author = "Author: Unknown";
			string path = (File.Exists(text3) ? text3 : text4);
			if (File.Exists(path))
			{
				try
				{
					DanceMeta danceMeta = JsonUtility.FromJson<DanceMeta>(File.ReadAllText(path));
					string text6 = null;
					if (!string.IsNullOrWhiteSpace(danceMeta.songAuthor))
					{
						text6 = danceMeta.songAuthor;
					}
					else if (!string.IsNullOrWhiteSpace(danceMeta.mmdAuthor))
					{
						text6 = danceMeta.mmdAuthor;
					}
					if (!string.IsNullOrWhiteSpace(text6))
					{
						author = "Author: " + text6;
					}
				}
				catch
				{
				}
			}
			DanceEntry danceEntry = new DanceEntry
			{
				id = fileNameWithoutExtension,
				path = mePath,
				bundlePath = text5,
				clip = null,
				audio = null,
				bundle = null,
				fromME = true,
				extractedDir = text2,
				author = author,
				stableId = "sha1:" + ComputeFileSha1(text5)
			};
			entries.Add(danceEntry);
			byId[fileNameWithoutExtension] = danceEntry;
		}

		private string ComputeFileSha1(string path)
		{
			try
			{
				using FileStream inputStream = File.OpenRead(path);
				using SHA1 sHA = SHA1.Create();
				return BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant();
			}
			catch
			{
				return null;
			}
		}

		public string GetCurrentStableId()
		{
			if (loadedEntry == null)
			{
				return null;
			}
			return loadedEntry.stableId;
		}

		public bool PlayByStableId(string stableId)
		{
			if (string.IsNullOrEmpty(stableId))
			{
				return false;
			}
			int num = entries.FindIndex((DanceEntry e) => string.Equals(e.stableId, stableId, StringComparison.OrdinalIgnoreCase));
			if (num < 0)
			{
				return false;
			}
			return PlayIndex(num);
		}

		private void BuildListUI()
		{
			if (contentObject == null || prefab == null)
			{
				return;
			}
			if (uiPool.Count == 0)
			{
				for (int num = contentObject.childCount - 1; num >= 0; num--)
				{
					UnityEngine.Object.Destroy(contentObject.GetChild(num).gameObject);
				}
			}
			while (uiPool.Count < entries.Count)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(prefab, contentObject);
				PooledItem pooledItem = new PooledItem
				{
					go = gameObject,
					titleTMP = FindChildByName<TMP_Text>(gameObject.transform, "Title"),
					authorTMP = FindChildByName<TMP_Text>(gameObject.transform, "Author"),
					titleFB = FindChildByName<Text>(gameObject.transform, "TitleFallback"),
					authorFB = FindChildByName<Text>(gameObject.transform, "AuthorFallback"),
					button = FindChildByName<Button>(gameObject.transform, "Button")
				};
				if (pooledItem.button == null)
				{
					Button[] componentsInChildren = gameObject.GetComponentsInChildren<Button>(includeInactive: true);
					if (componentsInChildren != null && componentsInChildren.Length != 0)
					{
						pooledItem.button = componentsInChildren[0];
					}
				}
				gameObject.SetActive(value: false);
				uiPool.Add(pooledItem);
			}
			for (int i = 0; i < entries.Count; i++)
			{
				DanceEntry danceEntry = entries[i];
				PooledItem pooledItem2 = uiPool[i];
				if (useFallbackFont && (pooledItem2.titleFB != null || pooledItem2.authorFB != null))
				{
					if (pooledItem2.titleFB != null)
					{
						pooledItem2.titleFB.text = danceEntry.id;
						pooledItem2.titleFB.gameObject.SetActive(value: true);
					}
					if (pooledItem2.authorFB != null)
					{
						pooledItem2.authorFB.text = (string.IsNullOrWhiteSpace(danceEntry.author) ? "Author: Unknown" : danceEntry.author);
						pooledItem2.authorFB.gameObject.SetActive(value: true);
					}
					if (pooledItem2.titleTMP != null)
					{
						pooledItem2.titleTMP.gameObject.SetActive(value: false);
					}
					if (pooledItem2.authorTMP != null)
					{
						pooledItem2.authorTMP.gameObject.SetActive(value: false);
					}
				}
				else
				{
					if (pooledItem2.titleTMP != null)
					{
						pooledItem2.titleTMP.text = danceEntry.id;
						pooledItem2.titleTMP.gameObject.SetActive(value: true);
					}
					if (pooledItem2.authorTMP != null)
					{
						pooledItem2.authorTMP.text = (string.IsNullOrWhiteSpace(danceEntry.author) ? "Author: Unknown" : danceEntry.author);
						pooledItem2.authorTMP.gameObject.SetActive(value: true);
					}
					if (pooledItem2.titleFB != null)
					{
						pooledItem2.titleFB.gameObject.SetActive(value: false);
					}
					if (pooledItem2.authorFB != null)
					{
						pooledItem2.authorFB.gameObject.SetActive(value: false);
					}
				}
				if (pooledItem2.button != null)
				{
					pooledItem2.button.onClick.RemoveAllListeners();
					int idx = i;
					pooledItem2.button.onClick.AddListener(delegate
					{
						OnListItemClicked(idx);
					});
				}
				pooledItem2.go.SetActive(value: true);
			}
			for (int num2 = entries.Count; num2 < uiPool.Count; num2++)
			{
				uiPool[num2].go.SetActive(value: false);
			}
		}

		private T FindChildByName<T>(Transform root, string name) where T : Component
		{
			if (root == null || string.IsNullOrEmpty(name))
			{
				return null;
			}
			Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				if (transform.name == name)
				{
					return transform.GetComponent<T>();
				}
			}
			return null;
		}

		private void PlayPrev()
		{
			if (entries.Count != 0)
			{
				if (filteredQueue == null || filteredQueue.Count == 0)
				{
					currentIndex = ((currentIndex <= 0) ? (entries.Count - 1) : (currentIndex - 1));
				}
				else
				{
					int num = filteredQueue.IndexOf(currentIndex);
					currentIndex = ((num < 0) ? filteredQueue[filteredQueue.Count - 1] : ((num == 0) ? filteredQueue[filteredQueue.Count - 1] : filteredQueue[num - 1]));
				}
				PlayIndex(currentIndex);
			}
		}

		private void PlayNext()
		{
			if (entries.Count != 0)
			{
				if (filteredQueue == null || filteredQueue.Count == 0)
				{
					currentIndex = (currentIndex + 1) % entries.Count;
				}
				else
				{
					int num = filteredQueue.IndexOf(currentIndex);
					currentIndex = ((num < 0) ? filteredQueue[0] : filteredQueue[(num + 1) % filteredQueue.Count]);
				}
				PlayIndex(currentIndex);
			}
		}

		private bool EnsureAnimatorReady()
		{
			RefreshAnimatorIfChanged();
			if (animator == null)
			{
				return false;
			}
			if (defaultController == null)
			{
				defaultController = animator.runtimeAnimatorController;
			}
			if (layerIndex < 0)
			{
				layerIndex = animator.GetLayerIndex(danceLayerName);
			}
			if (stateHash == 0)
			{
				stateHash = Animator.StringToHash(danceStateName);
			}
			if (overrideController == null && defaultController != null)
			{
				overrideController = new AnimatorOverrideController(defaultController);
				animator.runtimeAnimatorController = overrideController;
			}
			if (placeholderClipCached == null && defaultController != null)
			{
				placeholderClipCached = FindPlaceholderClip(defaultController, placeholderClipName);
				if (placeholderClipCached != null && overrideController != null)
				{
					overrideController[placeholderClipName] = placeholderClipCached;
				}
			}
			return true;
		}

		private AnimationClip FindPlaceholderClip(RuntimeAnimatorController ctrl, string name)
		{
			if (ctrl == null)
			{
				return null;
			}
			return new AnimatorOverrideController(ctrl).animationClips.FirstOrDefault((AnimationClip c) => c != null && c.name == name);
		}

		public bool PlayIndex(int index)
		{
			if (entries.Count == 0 || index < 0 || index >= entries.Count)
			{
				return false;
			}
			if (!EnsureAnimatorReady())
			{
				return false;
			}
			if (playRoutine != null)
			{
				StopCoroutine(playRoutine);
			}
			pendingStop = false;
			playRoutine = StartCoroutine(SmoothPlayFlow(index));
			return true;
		}

		private IEnumerator SmoothPlayFlow(int index)
		{
			holdDuringTransition = true;
			FreezeAnimator();
			PauseAudio();
			SetDancing(v: false);
			SetWaiting(v: true);
			float timeout = 2f;
			float t0 = Time.unscaledTime;
			while (!IsFullyInWaiting() && Time.unscaledTime - t0 < timeout)
			{
				yield return null;
			}
			DanceEntry danceEntry = loadedEntry;
			DanceEntry danceEntry2 = entries[index];
			if (danceEntry2.bundle == null)
			{
				string path = (string.IsNullOrEmpty(danceEntry2.bundlePath) ? danceEntry2.path : danceEntry2.bundlePath);
				danceEntry2.bundle = AssetBundle.LoadFromFile(path);
				if (danceEntry2.bundle == null)
				{
					UnfreezeAnimator();
					holdDuringTransition = false;
					yield break;
				}
			}
			if (danceEntry2.clip == null)
			{
				danceEntry2.clip = danceEntry2.bundle.LoadAllAssets<AnimationClip>().FirstOrDefault();
			}
			if (danceEntry2.audio == null)
			{
				danceEntry2.audio = danceEntry2.bundle.LoadAllAssets<AudioClip>().FirstOrDefault();
			}
			if (!EnsureAnimatorReady())
			{
				UnfreezeAnimator();
				holdDuringTransition = false;
				yield break;
			}
			if (placeholderClipCached == null)
			{
				placeholderClipCached = FindPlaceholderClip(defaultController, placeholderClipName);
			}
			if (overrideController == null || placeholderClipCached == null)
			{
				UnfreezeAnimator();
				holdDuringTransition = false;
				yield break;
			}
			overrideController[placeholderClipName] = ((danceEntry2.clip != null) ? danceEntry2.clip : placeholderClipCached);
			if (danceEntry != null && danceEntry != danceEntry2)
			{
				UnloadEntry(danceEntry);
				StartCoroutine(UnloadUnusedAssetsRoutine());
			}
			if (audioSource == null)
			{
				EnsureAudioSource();
			}
			if (audioSource != null)
			{
				audioSource.Stop();
				audioSource.clip = danceEntry2.audio;
				audioSource.time = 0f;
				audioSource.loop = false;
			}
			currentTotalSeconds = ((danceEntry2.audio != null) ? danceEntry2.audio.length : ((danceEntry2.clip != null) ? danceEntry2.clip.length : 0f));
			playStartTime = Time.time;
			isPlaying = true;
			currentIndex = index;
			loadedEntry = danceEntry2;
			UpdatePlayingNowLabel(danceEntry2.id);
			UpdateAuthorLabel(danceEntry2.author);
			UpdateTimeLabels(0f, currentTotalSeconds);
			SetWaiting(v: false);
			SetDancing(v: true);
			UnfreezeAnimator();
			ResumeAudio();
			holdDuringTransition = false;
			playRoutine = null;
		}

		private void StopAndUnload()
		{
			if (audioSource != null)
			{
				try
				{
					audioSource.Stop();
				}
				catch
				{
				}
				try
				{
					if (audioSource.clip != null)
					{
						audioSource.clip.UnloadAudioData();
					}
				}
				catch
				{
				}
				audioSource.clip = null;
			}
			if (animator != null)
			{
				if (overrideController != null && placeholderClipCached != null)
				{
					overrideController[placeholderClipName] = placeholderClipCached;
				}
				SetDancing(v: false);
				SetWaiting(v: false);
			}
			isPlaying = false;
			UpdatePlayingNowLabel(null);
			UpdateAuthorLabel(null);
			UpdateTimeLabels(0f, 0f);
			StartCoroutine(UnloadUnusedAssetsRoutine());
		}

		public void StopPlay()
		{
			if (!EnsureAnimatorReady())
			{
				isPlaying = false;
				UpdatePlayingNowLabel(null);
				UpdateAuthorLabel(null);
				UpdateTimeLabels(0f, 0f);
				return;
			}
			if (playRoutine != null)
			{
				StopCoroutine(playRoutine);
			}
			pendingStop = true;
			playRoutine = StartCoroutine(SmoothStopFlow());
		}

		private IEnumerator SmoothStopFlow()
		{
			holdDuringTransition = true;
			FreezeAnimator();
			PauseAudio();
			SetDancing(v: false);
			float timeout = 2f;
			float t0 = Time.unscaledTime;
			while (IsOnDanceState() && Time.unscaledTime - t0 < timeout)
			{
				yield return null;
			}
			if (overrideController != null && placeholderClipCached != null)
			{
				overrideController[placeholderClipName] = placeholderClipCached;
			}
			if (audioSource != null)
			{
				try
				{
					audioSource.Stop();
				}
				catch
				{
				}
				try
				{
					if (audioSource.clip != null)
					{
						audioSource.clip.UnloadAudioData();
					}
				}
				catch
				{
				}
				audioSource.clip = null;
			}
			DanceEntry danceEntry = loadedEntry;
			loadedEntry = null;
			if (danceEntry != null)
			{
				UnloadEntry(danceEntry);
				StartCoroutine(UnloadUnusedAssetsRoutine());
			}
			isPlaying = false;
			UpdatePlayingNowLabel(null);
			UpdateAuthorLabel(null);
			UpdateTimeLabels(0f, 0f);
			UnfreezeAnimator();
			holdDuringTransition = false;
			playRoutine = null;
		}

		private void UnloadEntry(DanceEntry e)
		{
			if (e == null)
			{
				return;
			}
			try
			{
				if (e.bundle != null)
				{
					e.bundle.Unload(unloadAllLoadedObjects: true);
				}
			}
			catch
			{
			}
			e.bundle = null;
			e.clip = null;
			e.audio = null;
		}

		private void UpdatePlayingNowLabel(string nameOrNull)
		{
			if (useFallbackFont && playingNowFallbackText != null)
			{
				playingNowFallbackText.text = (string.IsNullOrEmpty(nameOrNull) ? defaultPlayingNowText : nameOrNull);
				if (playingNowText != null)
				{
					playingNowText.text = "";
				}
			}
			else if (playingNowText != null)
			{
				playingNowText.text = (string.IsNullOrEmpty(nameOrNull) ? defaultPlayingNowText : nameOrNull);
			}
		}

		private void UpdateAuthorLabel(string authorOrNull)
		{
			if (useFallbackFont && authorFallbackText != null)
			{
				if (string.IsNullOrWhiteSpace(authorOrNull))
				{
					authorFallbackText.text = (string.IsNullOrWhiteSpace(defaultAuthorText) ? "Author: Unknown" : defaultAuthorText);
				}
				else
				{
					authorFallbackText.text = authorOrNull;
				}
				if (authorText != null)
				{
					authorText.text = "";
				}
			}
			else if (!(authorText == null))
			{
				if (string.IsNullOrWhiteSpace(authorOrNull))
				{
					authorText.text = (string.IsNullOrWhiteSpace(defaultAuthorText) ? "Author: Unknown" : defaultAuthorText);
				}
				else
				{
					authorText.text = authorOrNull;
				}
			}
		}

		private void UpdateTimeLabels(float elapsed, float total)
		{
			if (playTimeText != null)
			{
				playTimeText.text = ((total <= 0f) ? defaultPlayTimeText : FormatTime(elapsed));
			}
			if (maxPlayTimeText != null)
			{
				maxPlayTimeText.text = ((total <= 0f) ? defaultMaxPlayTimeText : FormatTime(total));
			}
		}

		private string FormatTime(float seconds)
		{
			int num = Mathf.FloorToInt(seconds + 0.0001f);
			int num2 = num / 60;
			int num3 = num % 60;
			return num2.ToString("00") + ":" + num3.ToString("00");
		}

		private void OnDestroy()
		{
			UnloadEntry(loadedEntry);
			foreach (DanceEntry entry in entries)
			{
				try
				{
					entry.bundle?.Unload(unloadAllLoadedObjects: true);
				}
				catch
				{
				}
			}
			entries.Clear();
			byId.Clear();
		}

		private IEnumerator UnloadUnusedAssetsRoutine()
		{
			yield return Resources.UnloadUnusedAssets();
		}

		private void TryAutoNext()
		{
			if (!autoNextScheduled)
			{
				autoNextScheduled = true;
				StartCoroutine(AutoNextCo());
			}
		}

		private IEnumerator AutoNextCo()
		{
			yield return null;
			int num = NextIndexForAuto();
			if (num >= 0)
			{
				PlayIndex(num);
			}
			autoNextScheduled = false;
		}

		public AnimationClip GetCurrentClip()
		{
			if (loadedEntry == null)
			{
				return null;
			}
			return loadedEntry.clip;
		}

		public float GetPlaybackTime()
		{
			if (audioSource != null && audioSource.clip != null)
			{
				return audioSource.time;
			}
			if (isPlaying)
			{
				return Mathf.Clamp(Time.time - playStartTime, 0f, currentTotalSeconds);
			}
			return 0f;
		}

		public float GetPlaybackLength()
		{
			return currentTotalSeconds;
		}

		public void SetQueueByIndices(List<int> indices)
		{
			if (indices == null || indices.Count == 0)
			{
				filteredQueue = null;
				return;
			}
			HashSet<int> hashSet = new HashSet<int>();
			List<int> list = new List<int>(indices.Count);
			for (int i = 0; i < indices.Count; i++)
			{
				int num = indices[i];
				if (num >= 0 && num < entries.Count && hashSet.Add(num))
				{
					list.Add(num);
				}
			}
			filteredQueue = ((list.Count > 0) ? list : null);
		}

		public int FindIndexByTitle(string title)
		{
			if (string.IsNullOrEmpty(title))
			{
				return -1;
			}
			for (int i = 0; i < entries.Count; i++)
			{
				if (string.Equals(entries[i].id, title, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}

		private bool IsModEnabled(string id)
		{
			if (SaveLoadHandler.Instance == null || SaveLoadHandler.Instance.data == null)
			{
				return true;
			}
			if (SaveLoadHandler.Instance.data.modStates.TryGetValue(id, out var value))
			{
				return value;
			}
			return true;
		}

		private IEnumerator Poll()
		{
			WaitForSecondsRealtime wait = new WaitForSecondsRealtime(pollInterval);
			while (true)
			{
				if (!isLeader && enableSync)
				{
					BusCmd d = Read();
					if (d != null && d.v > lastSeenV)
					{
						lastSeenV = d.v;
						if (scheduledCo != null)
						{
							StopCoroutine(scheduledCo);
							scheduledCo = null;
						}
						if (d.cmd == "PlayCurrentOrFirst")
						{
							guardActive = true;
							guardUntilUtc = d.atUtc;
							EnforceHold();
							MuteFollower();
							ScheduleRemote(delegate
							{
								TryPlayCurrentOrFirst();
							}, d.atUtc);
						}
						else if (d.cmd == "PlayByStableId")
						{
							guardActive = true;
							guardUntilUtc = d.atUtc;
							EnforceHold();
							MuteFollower();
							ScheduleRemote(delegate
							{
								TryPlayByStableIdOrFallback(d.sid, d.index, d.title);
							}, d.atUtc);
						}
						else if (d.cmd == "PlayNext")
						{
							guardActive = true;
							guardUntilUtc = d.atUtc;
							EnforceHold();
							MuteFollower();
							ScheduleRemote(delegate
							{
								PlayNext();
							}, d.atUtc);
						}
						else if (d.cmd == "PlayPrev")
						{
							guardActive = true;
							guardUntilUtc = d.atUtc;
							EnforceHold();
							MuteFollower();
							ScheduleRemote(delegate
							{
								PlayPrev();
							}, d.atUtc);
						}
						else if (d.cmd == "StopPlay")
						{
							ScheduleRemote(delegate
							{
								TryStopPlay();
								UnmuteFollower();
							}, d.atUtc);
						}
					}
				}
				yield return wait;
			}
		}

		private IEnumerator LeaderAutoNextWatcher()
		{
			WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.05f);
			while (true)
			{
				if (enableSync && isLeader)
				{
					if (audioSource != null && audioSource.clip != null && audioSource.time > 0f)
					{
						float num = audioSource.clip.length - audioSource.time;
						if (num <= 0.18f && !autoNextScheduled)
						{
							autoNextScheduled = true;
							double atUtc = UtcNow() + leadSeconds;
							guardActive = true;
							guardUntilUtc = atUtc;
							EnforceHold();
							int target = NextIndexForAuto();
							DanceEntry e = ((target >= 0 && target < entries.Count) ? entries[target] : null);
							ScheduleLocal(delegate
							{
								TryPlayByStableIdOrFallback((e != null) ? e.stableId : null, target, (e != null) ? e.id : null);
							}, atUtc);
							Broadcast("PlayByStableId", (e != null) ? e.stableId : null, target, (e != null) ? e.id : null, atUtc);
						}
						else if (num > 0.5f)
						{
							autoNextScheduled = false;
						}
					}
					else
					{
						autoNextScheduled = false;
					}
				}
				yield return wait;
			}
		}

		private int NextFromFiltered(bool forward)
		{
			int num = currentIndex;
			if (entries.Count == 0)
			{
				return -1;
			}
			if (filteredQueue == null || filteredQueue.Count == 0)
			{
				if (num < 0)
				{
					return 0;
				}
				if (forward)
				{
					return (num + 1) % entries.Count;
				}
				if (num > 0)
				{
					return num - 1;
				}
				return entries.Count - 1;
			}
			int num2 = filteredQueue.IndexOf(num);
			if (num2 < 0)
			{
				return filteredQueue[0];
			}
			if (forward)
			{
				return filteredQueue[(num2 + 1) % filteredQueue.Count];
			}
			if (num2 != 0)
			{
				return filteredQueue[num2 - 1];
			}
			return filteredQueue[filteredQueue.Count - 1];
		}

		private void ScheduleLocal(Action act, double atUtc)
		{
			double wait = Math.Max(0.0, atUtc - UtcNow());
			if (scheduledCo != null)
			{
				StopCoroutine(scheduledCo);
			}
			scheduledCo = StartCoroutine(Co(wait, act));
		}

		private void ScheduleRemote(Action act, double atUtc)
		{
			double wait = Math.Max(0.0, atUtc - UtcNow());
			if (scheduledCo != null)
			{
				StopCoroutine(scheduledCo);
			}
			scheduledCo = StartCoroutine(Co(wait, act));
		}

		private IEnumerator Co(double wait, Action act)
		{
			if (wait > 0.0)
			{
				yield return new WaitForSecondsRealtime((float)wait);
			}
			act();
			UnfreezeAnimator();
			guardActive = false;
			ReenableAll();
			scheduledCo = null;
		}

		private void TryPlayCurrentOrFirst()
		{
			if (filteredQueue != null && filteredQueue.Count > 0)
			{
				int index = ((currentIndex >= 0 && filteredQueue.Contains(currentIndex)) ? currentIndex : filteredQueue[0]);
				PlayIndex(index);
			}
			else
			{
				int index2 = ((currentIndex >= 0) ? currentIndex : 0);
				PlayIndex(index2);
			}
		}

		private void TryStopPlay()
		{
			StopPlay();
		}

		private void TryPlayByStableIdOrFallback(string sid, int idx, string title)
		{
			if (!string.IsNullOrEmpty(sid) && PlayByStableId(sid))
			{
				return;
			}
			if (idx >= 0)
			{
				PlayIndex(idx);
				return;
			}
			if (!string.IsNullOrEmpty(title))
			{
				int num = FindIndexByTitle(title);
				if (num >= 0)
				{
					PlayIndex(num);
					return;
				}
			}
			TryPlayCurrentOrFirst();
		}

		private void Broadcast(string cmd, string sid, int index, string title, double atUtc)
		{
			BusCmd busCmd = Read();
			int v = ((busCmd != null) ? (busCmd.v + 1) : 0);
			BusCmd d = new BusCmd
			{
				v = v,
				cmd = cmd,
				sid = sid,
				index = index,
				title = title,
				atUtc = atUtc,
				writeUtc = UtcNow()
			};
			SafeWrite(d);
		}

		private BusCmd Read()
		{
			try
			{
				if (!File.Exists(busPath))
				{
					return null;
				}
				string text = File.ReadAllText(busPath);
				if (string.IsNullOrWhiteSpace(text))
				{
					return null;
				}
				return JsonUtility.FromJson<BusCmd>(text);
			}
			catch
			{
				return null;
			}
		}

		private void SafeWrite(BusCmd d)
		{
			try
			{
				string text = busPath + ".tmp";
				File.WriteAllText(text, JsonUtility.ToJson(d));
				if (File.Exists(busPath))
				{
					File.Delete(busPath);
				}
				File.Move(text, busPath);
			}
			catch
			{
			}
		}

		private void TryAcquireLeader()
		{
			ReleaseLeader();
			try
			{
				leaderMutex = new Mutex(initiallyOwned: false, "MateEngine.AvatarDanceSync.Leader", out var _);
				isLeader = leaderMutex.WaitOne(0);
			}
			catch
			{
				isLeader = GetInstanceIndex() == 0;
			}
		}

		private int GetInstanceIndex()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			for (int i = 0; i < commandLineArgs.Length - 1; i++)
			{
				if (string.Equals(commandLineArgs[i], "--instance", StringComparison.OrdinalIgnoreCase) && int.TryParse(commandLineArgs[i + 1], out var result))
				{
					return Math.Max(0, result);
				}
			}
			return 0;
		}

		private void ReleaseLeader()
		{
			if (leaderMutex != null)
			{
				try
				{
					if (isLeader)
					{
						leaderMutex.ReleaseMutex();
					}
				}
				catch
				{
				}
				leaderMutex.Dispose();
				leaderMutex = null;
			}
			isLeader = false;
		}

		private static double UtcNow()
		{
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return (DateTime.UtcNow - dateTime).TotalSeconds;
		}

		private void EnforceHold()
		{
			holdDuringTransition = true;
			FreezeAnimator();
			PauseAudio();
			SetDancing(v: false);
			SetWaiting(v: true);
		}

		private void FreezeAnimator()
		{
			if (!(animator == null) && !animatorFrozen)
			{
				animatorPrevSpeed = ((animator.speed > 0f) ? animator.speed : 1f);
				animator.speed = 0f;
				animatorFrozen = true;
			}
		}

		private void UnfreezeAnimator()
		{
			if (!(animator == null))
			{
				animator.speed = ((animatorPrevSpeed > 0f) ? animatorPrevSpeed : 1f);
				animatorFrozen = false;
			}
		}

		private void ReenableAll()
		{
			for (int i = 0; i < tempDisabled.Count; i++)
			{
				if (tempDisabled[i] != null)
				{
					tempDisabled[i].interactable = true;
				}
			}
			tempDisabled.Clear();
		}

		private void MuteFollower()
		{
			if (!isLeader && !followerMuted)
			{
				if (volumeSlider != null)
				{
					storedSliderValue = volumeSlider.value;
					volumeSlider.value = 0f;
				}
				if (audioSource != null)
				{
					storedAudioVolume = audioSource.volume;
					audioSource.volume = 0f;
				}
				followerMuted = true;
			}
		}

		public void RescanMods()
		{
			bool num = isPlaying;
			string sid = GetCurrentStableId();
			float playbackTime = GetPlaybackTime();
			LoadAllSources();
			BuildListUI();
			if (!num || string.IsNullOrEmpty(sid))
			{
				return;
			}
			int num2 = entries.FindIndex((DanceEntry e) => string.Equals(e.stableId, sid, StringComparison.OrdinalIgnoreCase));
			if (num2 >= 0)
			{
				PlayIndex(num2);
				if (audioSource != null && audioSource.clip != null)
				{
					audioSource.time = Mathf.Clamp(playbackTime, 0f, audioSource.clip.length);
				}
			}
		}

		private bool HasBool(string param)
		{
			if (animator == null)
			{
				return false;
			}
			AnimatorControllerParameter[] parameters = animator.parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].type == AnimatorControllerParameterType.Bool && parameters[i].name == param)
				{
					return true;
				}
			}
			return false;
		}

		private void SetWaiting(bool v)
		{
			if (HasBool(waitingParam))
			{
				animator.SetBool(waitingParam, v);
			}
		}

		private void SetDancing(bool v)
		{
			if (HasBool(customDancingParam))
			{
				animator.SetBool(customDancingParam, v);
			}
		}

		private void ResetMMDBlendShapes()
		{
			if (animator == null)
			{
				return;
			}
			SkinnedMeshRenderer[] componentsInChildren = animator.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren)
			{
				Mesh mesh = ((skinnedMeshRenderer != null) ? skinnedMeshRenderer.sharedMesh : null);
				if (mesh == null)
				{
					continue;
				}
				int blendShapeCount = mesh.blendShapeCount;
				for (int j = 0; j < blendShapeCount; j++)
				{
					string blendShapeName = mesh.GetBlendShapeName(j);
					if (!string.IsNullOrEmpty(blendShapeName) && mmdBlendShapeNames.Contains(blendShapeName))
					{
						skinnedMeshRenderer.SetBlendShapeWeight(j, 0f);
					}
				}
			}
		}

		private int PickRandomFromFilteredExcludingCurrent()
		{
			if (entries.Count == 0)
			{
				return -1;
			}
			List<int> list = ((filteredQueue != null && filteredQueue.Count > 0) ? filteredQueue : Enumerable.Range(0, entries.Count).ToList());
			if (list.Count == 0)
			{
				return -1;
			}
			if (list.Count == 1)
			{
				return list[0];
			}
			int num;
			do
			{
				num = list[UnityEngine.Random.Range(0, list.Count)];
			}
			while (num == currentIndex);
			return num;
		}

		private int NextIndexSequential(bool forward)
		{
			if (entries.Count == 0)
			{
				return -1;
			}
			if (filteredQueue == null || filteredQueue.Count == 0)
			{
				if (currentIndex < 0)
				{
					return 0;
				}
				if (!forward)
				{
					if (currentIndex > 0)
					{
						return currentIndex - 1;
					}
					return entries.Count - 1;
				}
				return (currentIndex + 1) % entries.Count;
			}
			int num = filteredQueue.IndexOf(currentIndex);
			if (num < 0)
			{
				return filteredQueue[0];
			}
			if (forward)
			{
				return filteredQueue[(num + 1) % filteredQueue.Count];
			}
			if (num != 0)
			{
				return filteredQueue[num - 1];
			}
			return filteredQueue[filteredQueue.Count - 1];
		}

		private int NextIndexForManual(bool forward)
		{
			if (shuffleOn)
			{
				return PickRandomFromFilteredExcludingCurrent();
			}
			return NextIndexSequential(forward);
		}

		private int NextIndexForAuto()
		{
			if (loopOn && currentIndex >= 0)
			{
				return currentIndex;
			}
			if (shuffleOn)
			{
				return PickRandomFromFilteredExcludingCurrent();
			}
			return NextIndexSequential(forward: true);
		}

		private void UnmuteFollower()
		{
			if (!isLeader && followerMuted)
			{
				if (volumeSlider != null && storedSliderValue >= 0f)
				{
					volumeSlider.value = storedSliderValue;
				}
				if (audioSource != null && storedAudioVolume >= 0f)
				{
					audioSource.volume = storedAudioVolume;
				}
				followerMuted = false;
			}
		}
	}
}
