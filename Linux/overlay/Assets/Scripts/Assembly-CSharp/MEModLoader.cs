using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class MEModLoader : MonoBehaviour
{
	public static MEModLoader Instance;

	[Header("Required")]
	public ChibiToggle chibiToggle;

	[Header("Optional")]
	public AvatarDragSoundHandler dragSoundHandler;

	public PetVoiceReactionHandler petVoiceHandler;

	private string enterFolder;

	private string exitFolder;

	private string chibiSettingsPath;

	private string dragFolder;

	private string placeFolder;

	private string hoverReactionsFolder;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	private void Start()
	{
		string path = Path.Combine(Application.streamingAssetsPath, "Mods/ModLoader/Chibi Mode/Sounds");
		enterFolder = Path.Combine(path, "Enter Sounds");
		exitFolder = Path.Combine(path, "Exit Sounds");
		chibiSettingsPath = Path.Combine(Application.streamingAssetsPath, "Mods/ModLoader/Chibi Mode/settings.json");
		string path2 = Path.Combine(Application.streamingAssetsPath, "Mods/ModLoader/Drag Mode/Sounds");
		dragFolder = Path.Combine(path2, "Drag Sounds");
		placeFolder = Path.Combine(path2, "Place Sounds");
		hoverReactionsFolder = Path.Combine(Application.streamingAssetsPath, "Mods/ModLoader/Hover Reactions");
		EnsureFolderStructure();
		GameObject gameObject = FindCurrentActiveAvatar();
		if (gameObject != null)
		{
			AssignHandlersForCurrentAvatar(gameObject);
		}
		StartCoroutine(LoadChibiSounds());
		StartCoroutine(LoadDragSounds());
		StartCoroutine(ApplyChibiSettings());
		StartCoroutine(LoadHoverReactionSounds());
	}

	public void AssignHandlersForCurrentAvatar(GameObject avatar)
	{
		if (!(avatar == null))
		{
			chibiToggle = avatar.GetComponentInChildren<ChibiToggle>(includeInactive: true);
			dragSoundHandler = avatar.GetComponentInChildren<AvatarDragSoundHandler>(includeInactive: true);
			petVoiceHandler = avatar.GetComponentInChildren<PetVoiceReactionHandler>(includeInactive: true);
			StartCoroutine(LoadChibiSounds());
			StartCoroutine(LoadDragSounds());
			StartCoroutine(LoadHoverReactionSounds());
			StartCoroutine(ApplyChibiSettings());
		}
	}

	private GameObject FindCurrentActiveAvatar()
	{
		GameObject gameObject = GameObject.Find("Model");
		if (gameObject == null)
		{
			return null;
		}
		foreach (Transform item in gameObject.transform)
		{
			if (item.gameObject.activeInHierarchy)
			{
				return item.gameObject;
			}
		}
		return null;
	}

	private void EnsureFolderStructure()
	{
		TryCreateDirectory(enterFolder);
		TryCreateDirectory(exitFolder);
		TryCreateDirectory(dragFolder);
		TryCreateDirectory(placeFolder);
		TryCreateDirectory(hoverReactionsFolder);
	}

	private void TryCreateDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		string path2 = Path.Combine(path, ".keep");
		if (!File.Exists(path2))
		{
			File.WriteAllText(path2, "Keeps folder in build.");
		}
	}

	private IEnumerator LoadChibiSounds()
	{
		List<AudioClip> enterSounds = new List<AudioClip>();
		List<AudioClip> exitSounds = new List<AudioClip>();
		if (!Directory.Exists(enterFolder))
		{
			yield break;
		}
		string[] files = Directory.GetFiles(enterFolder);
		foreach (string filePath in files)
		{
			yield return LoadClip(filePath, delegate(AudioClip clip)
			{
				enterSounds.Add(clip);
			});
		}
		if (!Directory.Exists(exitFolder))
		{
			yield break;
		}
		files = Directory.GetFiles(exitFolder);
		foreach (string filePath2 in files)
		{
			yield return LoadClip(filePath2, delegate(AudioClip clip)
			{
				exitSounds.Add(clip);
			});
		}
		if (chibiToggle != null)
		{
			if (enterSounds.Count > 0)
			{
				chibiToggle.chibiEnterSounds = enterSounds;
			}
			if (exitSounds.Count > 0)
			{
				chibiToggle.chibiExitSounds = exitSounds;
			}
		}
	}

	private IEnumerator LoadDragSounds()
	{
		if (dragSoundHandler == null)
		{
			yield break;
		}
		List<AudioClip> dragClips = new List<AudioClip>();
		List<AudioClip> placeClips = new List<AudioClip>();
		if (Directory.Exists(dragFolder))
		{
			string[] files = Directory.GetFiles(dragFolder);
			foreach (string filePath in files)
			{
				yield return LoadClip(filePath, delegate(AudioClip clip)
				{
					dragClips.Add(clip);
				});
			}
		}
		if (Directory.Exists(placeFolder))
		{
			string[] files = Directory.GetFiles(placeFolder);
			foreach (string filePath2 in files)
			{
				yield return LoadClip(filePath2, delegate(AudioClip clip)
				{
					placeClips.Add(clip);
				});
			}
		}
		if (dragClips.Count > 0)
		{
			dragSoundHandler.dragStartSound = CreateRandomAudioSource(dragClips, "DragStart");
		}
		if (placeClips.Count > 0)
		{
			dragSoundHandler.dragStopSound = CreateRandomAudioSource(placeClips, "DragStop");
		}
	}

	private IEnumerator LoadHoverReactionSounds()
	{
		if (petVoiceHandler == null || petVoiceHandler.regions == null)
		{
			yield break;
		}
		foreach (PetVoiceReactionHandler.VoiceRegion region in petVoiceHandler.regions)
		{
			string path = (string.IsNullOrWhiteSpace(region.name) ? region.targetBone.ToString() : region.name);
			string text = Path.Combine(hoverReactionsFolder, path);
			string path2 = Path.Combine(text, "Voice Clips");
			string layeredClipsFolder = Path.Combine(text, "Layered Voice Clips");
			TryCreateDirectory(text);
			TryCreateDirectory(path2);
			TryCreateDirectory(layeredClipsFolder);
			List<AudioClip> voiceClips = new List<AudioClip>();
			List<AudioClip> layeredClips = new List<AudioClip>();
			if (Directory.Exists(path2))
			{
				string[] files = Directory.GetFiles(path2);
				foreach (string text2 in files)
				{
					switch (Path.GetExtension(text2).ToLower())
					{
					case ".wav":
					case ".mp3":
					case ".ogg":
						yield return LoadClip(text2, delegate(AudioClip clip)
						{
							if (clip != null)
							{
								voiceClips.Add(clip);
							}
						});
						break;
					}
				}
			}
			if (Directory.Exists(layeredClipsFolder))
			{
				string[] files = Directory.GetFiles(layeredClipsFolder);
				foreach (string text3 in files)
				{
					switch (Path.GetExtension(text3).ToLower())
					{
					case ".wav":
					case ".mp3":
					case ".ogg":
						yield return LoadClip(text3, delegate(AudioClip clip)
						{
							if (clip != null)
							{
								layeredClips.Add(clip);
							}
						});
						break;
					}
				}
			}
			if (voiceClips.Count > 0)
			{
				region.voiceClips.Clear();
				region.voiceClips.AddRange(voiceClips);
			}
			if (layeredClips.Count > 0)
			{
				region.layeredVoiceClips.Clear();
				region.layeredVoiceClips.AddRange(layeredClips);
			}
		}
		Debug.Log("[MEModLoader] Hover reaction sounds loaded.");
	}

	private IEnumerator LoadClip(string filePath, Action<AudioClip> onSuccess)
	{
		string text = Path.GetExtension(filePath).ToLower();
		if (text != ".wav" && text != ".mp3" && text != ".ogg")
		{
			yield break;
		}
		string uri = "file://" + filePath;
		using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, GetAudioType(text));
		yield return www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning("[MEModLoader] Failed to load sound: " + filePath + " | " + www.error);
		}
		else
		{
			onSuccess?.Invoke(DownloadHandlerAudioClip.GetContent(www));
		}
	}

	private AudioType GetAudioType(string extension)
	{
		return extension switch
		{
			".mp3" => AudioType.MPEG, 
			".ogg" => AudioType.OGGVORBIS, 
			".wav" => AudioType.WAV, 
			_ => AudioType.UNKNOWN, 
		};
	}

	private AudioSource CreateRandomAudioSource(List<AudioClip> clips, string label)
	{
		GameObject obj = new GameObject("DynamicSoundPlayer_" + label);
		obj.transform.SetParent(base.transform);
		AudioSource audioSource = obj.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		StartCoroutine(RandomizeClipEveryFrame(audioSource, clips));
		return audioSource;
	}

	private IEnumerator RandomizeClipEveryFrame(AudioSource source, List<AudioClip> clips)
	{
		while (true)
		{
			if (!source.isPlaying && clips.Count > 0)
			{
				source.clip = clips[UnityEngine.Random.Range(0, clips.Count)];
			}
			yield return null;
		}
	}

	private IEnumerator ApplyChibiSettings()
	{
		if (!File.Exists(chibiSettingsPath))
		{
			Debug.Log("[MEModLoader] No Chibi settings.json found, skipping.");
			yield break;
		}
		string text = File.ReadAllText(chibiSettingsPath);
		if (!string.IsNullOrEmpty(text))
		{
			ChibiSettingsData chibiSettingsData;
			try
			{
				chibiSettingsData = JsonUtility.FromJson<ChibiSettingsData>(text);
			}
			catch
			{
				Debug.LogWarning("[MEModLoader] Failed to parse Chibi settings.json.");
				yield break;
			}
			if (chibiToggle != null)
			{
				chibiToggle.chibiArmatureScale = chibiSettingsData.chibiArmatureScale;
				chibiToggle.chibiHeadScale = chibiSettingsData.chibiHeadScale;
				chibiToggle.chibiUpperLegScale = chibiSettingsData.chibiUpperLegScale;
			}
			Debug.Log("[MEModLoader] Applied Chibi settings from JSON.");
		}
	}
}
