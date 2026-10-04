using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using CustomDancePlayer;
using SFB;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MEModHandler : MonoBehaviour
{
	[Serializable]
	private class ModEntry
	{
		public string name;

		public GameObject instance;

		public string localPath;

		public string extractedPath;

		public ModType type;

		public bool enabled;

		public string author;

		public string typeText;

		public AssetBundle retainedBundle;
	}

	[Serializable]
	private class ModInfo
	{
		public string name;

		public string author;

		public string description;

		public string weblink;

		public string buildTarget;

		public string timestamp;
	}

	[Serializable]
	private class ModTypeInfo
	{
		public string type;
	}

	private enum ModType
	{
		MEObject = 0,
		Unity3D = 1,
		MEDance = 2
	}

	[Serializable]
	private class RefPathMap
	{
		public List<string> keys = new List<string>();

		public List<string> values = new List<string>();
	}

	[Serializable]
	private class SceneLinkMap
	{
		public List<string> keys = new List<string>();

		public List<string> values = new List<string>();
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

	public Button loadModButton;

	public Transform modListContainer;

	public GameObject modEntryPrefab;

	private string modFolderPath;

	private readonly List<ModEntry> loadedMods = new List<ModEntry>();

	private static readonly Dictionary<string, GameObject> GlobalInstances = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

	private void Start()
	{
		modFolderPath = Path.Combine(Application.persistentDataPath, "Mods");
		Directory.CreateDirectory(modFolderPath);
		if (loadModButton != null)
		{
			loadModButton.onClick.AddListener(OpenFileDialogAndLoadMod);
		}
		StartCoroutine(BootLoadMods());
	}

	private IEnumerator BootLoadMods()
	{
		yield return null;
		LoadAllModsInFolder();
	}

	private void LoadAllModsInFolder()
	{
		for (int num = modListContainer.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(modListContainer.GetChild(num).gameObject);
		}
		loadedMods.Clear();
		List<string> list = new List<string>();
		try
		{
			foreach (string item in Directory.EnumerateFiles(modFolderPath, "*", SearchOption.AllDirectories))
			{
				string extension = Path.GetExtension(item);
				if (!string.IsNullOrEmpty(extension) && (extension.Equals(".me", StringComparison.OrdinalIgnoreCase) || extension.Equals(".unity3d", StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (text.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
			{
				LoadME(text);
			}
			else if (text.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
			{
				LoadUnity3D(text, addToUI: true, respectSavedState: true);
			}
		}
		AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
		if (avatarDanceHandler != null)
		{
			avatarDanceHandler.RescanMods();
		}
	}

	private void OpenFileDialogAndLoadMod()
	{
		ExtensionFilter[] extensions = new ExtensionFilter[1]
		{
			new ExtensionFilter("MateEngine Files", "me", "unity3d")
		};
		string[] array = StandaloneFileBrowser.OpenFilePanel("Select Mod or Dance Asset", ".", extensions, multiselect: false);
		if (array.Length != 0 && !string.IsNullOrEmpty(array[0]))
		{
			string text = array[0];
			string text2 = Path.Combine(modFolderPath, Path.GetFileName(text));
			try
			{
				File.Copy(text, text2, overwrite: true);
			}
			catch
			{
			}
			if (text2.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
			{
				LoadME(text2);
			}
			else if (text2.EndsWith(".unity3d", StringComparison.OrdinalIgnoreCase))
			{
				LoadUnity3D(text2, addToUI: true, respectSavedState: false);
			}
			AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			if (avatarDanceHandler != null)
			{
				avatarDanceHandler.RescanMods();
			}
		}
	}

	private void LoadUnity3D(string path, bool addToUI, bool respectSavedState)
	{
		string name = Path.GetFileNameWithoutExtension(path);
		int num = loadedMods.FindIndex((ModEntry m) => string.Equals(m.name, name, StringComparison.OrdinalIgnoreCase) && m.type == ModType.Unity3D);
		if (num >= 0)
		{
			loadedMods.RemoveAt(num);
		}
		bool initialState = true;
		if (respectSavedState && SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null && SaveLoadHandler.Instance.data.modStates.TryGetValue(name, out var value))
		{
			initialState = value;
		}
		ModEntry modEntry = new ModEntry
		{
			name = name,
			localPath = path,
			type = ModType.Unity3D,
			instance = null,
			enabled = initialState,
			author = "Author: Unknown",
			typeText = "Type: Unity3D"
		};
		loadedMods.Add(modEntry);
		if (addToUI)
		{
			AddToModListUI(modEntry, initialState);
		}
	}

	private void LoadME(string path)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		string text = Path.Combine(Application.temporaryCachePath, "ME_Cache");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, fileNameWithoutExtension);
		bool flag = true;
		try
		{
			if (Directory.Exists(text2))
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
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
				ZipFile.ExtractToDirectory(path, text2);
				Directory.SetLastWriteTimeUtc(text2, File.GetLastWriteTimeUtc(path));
			}
		}
		catch
		{
			return;
		}
		if (File.Exists(Path.Combine(text2, "dance_meta.json")))
		{
			LoadMEDance(path, text2, fileNameWithoutExtension);
		}
		else
		{
			LoadMEObject(path, text2, fileNameWithoutExtension);
		}
	}

	private void LoadMEDance(string mePath, string extractedDir, string id)
	{
		bool savedStateOrDefault = GetSavedStateOrDefault(id, def: true);
		string text = Directory.GetFiles(extractedDir, "*.bundle", SearchOption.AllDirectories).FirstOrDefault();
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			text = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories).FirstOrDefault((string f) => Path.GetExtension(f).Equals(".bundle", StringComparison.OrdinalIgnoreCase));
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string author = "Author: Unknown";
		string path = Path.Combine(extractedDir, "dance_meta.json");
		if (File.Exists(path))
		{
			try
			{
				DanceMeta danceMeta = JsonUtility.FromJson<DanceMeta>(File.ReadAllText(path));
				string text2 = null;
				if (!string.IsNullOrWhiteSpace(danceMeta.songAuthor))
				{
					text2 = danceMeta.songAuthor;
				}
				else if (!string.IsNullOrWhiteSpace(danceMeta.mmdAuthor))
				{
					text2 = danceMeta.mmdAuthor;
				}
				if (!string.IsNullOrWhiteSpace(text2))
				{
					author = "Author: " + text2;
				}
			}
			catch
			{
			}
		}
		ModEntry modEntry = new ModEntry
		{
			name = id,
			instance = null,
			localPath = mePath,
			extractedPath = extractedDir,
			type = ModType.MEDance,
			enabled = savedStateOrDefault,
			author = author,
			typeText = "Type: Dance"
		};
		loadedMods.Add(modEntry);
		AddToModListUI(modEntry, savedStateOrDefault);
	}

	private void LoadMEObject(string mePath, string extractedDir, string id)
	{
		if (GlobalInstances.TryGetValue(id, out var value) && value != null)
		{
			bool savedStateOrDefault = GetSavedStateOrDefault(id, value.activeSelf);
			value.SetActive(savedStateOrDefault);
			string author = ReadAuthorFromModInfo(extractedDir);
			string typeText = ReadTypeFromModType(extractedDir, "Mod");
			ModEntry modEntry = new ModEntry
			{
				name = id,
				instance = value,
				localPath = mePath,
				extractedPath = extractedDir,
				type = ModType.MEObject,
				enabled = savedStateOrDefault,
				author = author,
				typeText = typeText
			};
			loadedMods.Add(modEntry);
			AddToModListUI(modEntry, savedStateOrDefault);
			return;
		}
		string text = null;
		try
		{
			string[] files = Directory.GetFiles(extractedDir, "*.bundle", SearchOption.AllDirectories);
			int num = 0;
			if (num < files.Length)
			{
				text = files[num];
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return;
		}
		AssetBundle assetBundle = null;
		try
		{
			assetBundle = AssetBundle.LoadFromFile(text);
		}
		catch
		{
		}
		if (assetBundle == null)
		{
			return;
		}
		GameObject gameObject = null;
		try
		{
			gameObject = assetBundle.LoadAsset<GameObject>(id);
		}
		catch
		{
		}
		if (gameObject == null)
		{
			GameObject[] array = assetBundle.LoadAllAssets<GameObject>();
			if (array != null && array.Length != 0)
			{
				gameObject = array[0];
			}
		}
		GameObject gameObject2 = null;
		if (gameObject != null)
		{
			try
			{
				gameObject2 = UnityEngine.Object.Instantiate(gameObject);
			}
			catch
			{
			}
		}
		if (gameObject2 == null)
		{
			return;
		}
		string path = Path.Combine(extractedDir, "reference_paths.json");
		string path2 = Path.Combine(extractedDir, "scene_links.json");
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (File.Exists(path))
			{
				RefPathMap refPathMap = JsonUtility.FromJson<RefPathMap>(File.ReadAllText(path));
				for (int i = 0; i < refPathMap.keys.Count; i++)
				{
					dictionary[refPathMap.keys[i]] = refPathMap.values[i];
				}
			}
		}
		catch
		{
		}
		try
		{
			if (File.Exists(path2))
			{
				SceneLinkMap sceneLinkMap = JsonUtility.FromJson<SceneLinkMap>(File.ReadAllText(path2));
				for (int j = 0; j < sceneLinkMap.keys.Count; j++)
				{
					dictionary2[sceneLinkMap.keys[j]] = sceneLinkMap.values[j];
				}
			}
		}
		catch
		{
		}
		ApplyReferencePaths(gameObject2, dictionary, dictionary2);
		bool savedStateOrDefault2 = GetSavedStateOrDefault(id, def: true);
		gameObject2.SetActive(value: false);
		PreloadAudioClipsFromMEVoicePack(gameObject2);
		gameObject2.SetActive(savedStateOrDefault2);
		gameObject2.name = "ME_" + id;
		GlobalInstances[id] = gameObject2;
		string author2 = ReadAuthorFromModInfo(extractedDir);
		string typeText2 = ReadTypeFromModType(extractedDir, "Mod");
		ModEntry modEntry2 = new ModEntry
		{
			name = id,
			instance = gameObject2,
			localPath = mePath,
			extractedPath = extractedDir,
			type = ModType.MEObject,
			enabled = savedStateOrDefault2,
			author = author2,
			typeText = typeText2,
			retainedBundle = assetBundle
		};
		loadedMods.Add(modEntry2);
		AddToModListUI(modEntry2, savedStateOrDefault2);
	}

	private void PreloadAudioClipsFromMEVoicePack(GameObject root)
	{
		MEVoicePack[] componentsInChildren = root.GetComponentsInChildren<MEVoicePack>(includeInactive: true);
		foreach (MEVoicePack obj in componentsInChildren)
		{
			FieldInfo[] fields = typeof(MEVoicePack).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int j = 0; j < fields.Length; j++)
			{
				Type fieldType = fields[j].FieldType;
				object value = fields[j].GetValue(obj);
				if (value == null)
				{
					continue;
				}
				if (fieldType == typeof(AudioClip))
				{
					AudioClip audioClip = (AudioClip)value;
					if (audioClip != null && audioClip.loadState != AudioDataLoadState.Loaded)
					{
						audioClip.LoadAudioData();
					}
				}
				else
				{
					if (!typeof(IEnumerable).IsAssignableFrom(fieldType) || !(fieldType != typeof(string)))
					{
						continue;
					}
					foreach (object item in (IEnumerable)value)
					{
						AudioClip audioClip2 = item as AudioClip;
						if (audioClip2 != null && audioClip2.loadState != AudioDataLoadState.Loaded)
						{
							audioClip2.LoadAudioData();
						}
					}
				}
			}
		}
	}

	private void ApplyReferencePaths(GameObject root, Dictionary<string, string> refPaths, Dictionary<string, string> sceneLinks)
	{
		MonoBehaviour[] componentsInChildren = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
		foreach (MonoBehaviour monoBehaviour in componentsInChildren)
		{
			if (monoBehaviour == null)
			{
				continue;
			}
			Type type = monoBehaviour.GetType();
			string text = type.Name;
			Dictionary<string, string>[] array = new Dictionary<string, string>[2] { refPaths, sceneLinks };
			for (int j = 0; j < array.Length; j++)
			{
				foreach (KeyValuePair<string, string> item in array[j])
				{
					if (!item.Key.StartsWith(text + ".", StringComparison.Ordinal))
					{
						continue;
					}
					string text2 = item.Key.Substring(text.Length + 1);
					GameObject gameObject = GameObject.Find(item.Value);
					if (gameObject == null)
					{
						continue;
					}
					object obj = monoBehaviour;
					Type type2 = type;
					string[] array2 = text2.Split('.');
					for (int k = 0; k < array2.Length; k++)
					{
						string text3 = array2[k];
						int num = -1;
						if (text3.Contains("["))
						{
							int num2 = text3.IndexOf('[');
							int num3 = text3.IndexOf(']');
							num = int.Parse(text3.Substring(num2 + 1, num3 - num2 - 1));
							text3 = text3.Substring(0, num2);
						}
						FieldInfo field = type2.GetField(text3, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (field == null)
						{
							break;
						}
						if (k == array2.Length - 1)
						{
							if (field.FieldType == typeof(GameObject))
							{
								field.SetValue(obj, gameObject);
							}
							else if (typeof(Component).IsAssignableFrom(field.FieldType))
							{
								Component component = gameObject.GetComponent(field.FieldType);
								if (component != null)
								{
									field.SetValue(obj, component);
								}
							}
							continue;
						}
						object value = field.GetValue(obj);
						if (value == null)
						{
							break;
						}
						if (num >= 0 && value is IList list)
						{
							if (num >= list.Count)
							{
								break;
							}
							obj = list[num];
						}
						else
						{
							obj = value;
						}
						if (obj == null)
						{
							break;
						}
						type2 = obj.GetType();
					}
				}
			}
		}
	}

	private bool GetSavedStateOrDefault(string key, bool def)
	{
		if (SaveLoadHandler.Instance == null || SaveLoadHandler.Instance.data == null)
		{
			return def;
		}
		if (SaveLoadHandler.Instance.data.modStates.TryGetValue(key, out var value))
		{
			return value;
		}
		return def;
	}

	private void AddToModListUI(ModEntry mod, bool initialState)
	{
		if (modEntryPrefab == null || modListContainer == null)
		{
			return;
		}
		GameObject entry = UnityEngine.Object.Instantiate(modEntryPrefab, modListContainer);
		entry.name = "Mod_" + mod.name;
		TMP_Text tMP_Text = FindChildByName<TMP_Text>(entry.transform, "Title");
		if (tMP_Text == null)
		{
			TextMeshProUGUI textMeshProUGUI = FindChildByName<TextMeshProUGUI>(entry.transform, "ModNameText");
			if (textMeshProUGUI != null)
			{
				tMP_Text = textMeshProUGUI;
			}
		}
		if (tMP_Text != null)
		{
			tMP_Text.text = mod.name;
		}
		TMP_Text tMP_Text2 = FindChildByName<TMP_Text>(entry.transform, "Author");
		if (tMP_Text2 != null)
		{
			tMP_Text2.text = (string.IsNullOrWhiteSpace(mod.author) ? "Author: Unknown" : mod.author);
		}
		TMP_Text tMP_Text3 = FindChildByName<TMP_Text>(entry.transform, "Type");
		if (tMP_Text3 != null)
		{
			tMP_Text3.text = (string.IsNullOrWhiteSpace(mod.typeText) ? "Type: Unknown" : mod.typeText);
		}
		RawImage rawImage = FindChildByName<RawImage>(entry.transform, "RawImage");
		LoadThumbToRawImage(rawImage, mod.name);
		Toggle componentInChildren = entry.GetComponentInChildren<Toggle>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.isOn = initialState;
			if (mod.type == ModType.MEObject)
			{
				if (mod.instance != null)
				{
					mod.instance.SetActive(initialState);
				}
				componentInChildren.onValueChanged.AddListener(delegate(bool a)
				{
					if (mod.instance != null)
					{
						mod.instance.SetActive(a);
					}
					PersistState(mod.name, a);
				});
			}
			else
			{
				componentInChildren.onValueChanged.AddListener(delegate(bool a)
				{
					PersistState(mod.name, a);
					AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
					if (avatarDanceHandler != null)
					{
						avatarDanceHandler.RescanMods();
					}
				});
			}
		}
		Button button = FindChildByName<Button>(entry.transform, "Upload");
		if (button != null)
		{
			ModUploadButton component = button.GetComponent<ModUploadButton>();
			Slider progress = FindChildByName<Slider>(entry.transform, "Progress");
			ModUploadHoldHandler component2 = button.GetComponent<ModUploadHoldHandler>();
			if (component2 != null && component2.previewImage == null)
			{
				component2.previewImage = rawImage;
			}
			string thumbPath = GetThumbPath(mod.name);
			if (component != null)
			{
				component.button = button;
				component.filePath = mod.localPath;
				component.displayName = mod.name;
				string text = (mod.author.StartsWith("Author: ") ? mod.author.Substring(8) : mod.author);
				component.author = (string.Equals(text, "Unknown", StringComparison.OrdinalIgnoreCase) ? null : text);
				component.isNSFW = false;
				component.thumbnailPath = (File.Exists(thumbPath) ? thumbPath : null);
				component.progressBar = progress;
			}
			else
			{
				button.onClick.AddListener(delegate
				{
					SteamWorkshopHandler steamWorkshopHandler = UnityEngine.Object.FindFirstObjectByType<SteamWorkshopHandler>();
					if (steamWorkshopHandler != null)
					{
						steamWorkshopHandler.BeginUploadMod(mod.localPath, progress);
					}
				});
			}
		}
		Button button2 = FindChildByName<Button>(entry.transform, "Remove");
		if (!(button2 != null))
		{
			return;
		}
		ModRemoveButton component3 = button2.GetComponent<ModRemoveButton>();
		if (component3 != null)
		{
			component3.button = button2;
			component3.filePath = mod.localPath;
			component3.workshopId = ResolveWorkshopIdForPath(mod.localPath);
		}
		else
		{
			button2.onClick.AddListener(delegate
			{
				RemoveMod(mod, entry);
			});
		}
	}

	private ulong ResolveWorkshopIdForPath(string localPath)
	{
		try
		{
			if (!SteamManager.Initialized)
			{
				return 0uL;
			}
			uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
			if (numSubscribedItems == 0)
			{
				return 0uL;
			}
			PublishedFileId_t[] array = new PublishedFileId_t[numSubscribedItems];
			SteamUGC.GetSubscribedItems(array, numSubscribedItems);
			string fileName = Path.GetFileName(localPath);
			for (int i = 0; i < array.Length; i++)
			{
				if (!SteamUGC.GetItemInstallInfo(array[i], out var _, out var pchFolder, 1024u, out var _) || string.IsNullOrEmpty(pchFolder) || !Directory.Exists(pchFolder))
				{
					continue;
				}
				string[] files = Directory.GetFiles(pchFolder, "*", SearchOption.TopDirectoryOnly);
				for (int j = 0; j < files.Length; j++)
				{
					if (string.Equals(Path.GetFileName(files[j]), fileName, StringComparison.OrdinalIgnoreCase))
					{
						return array[i].m_PublishedFileId;
					}
				}
			}
		}
		catch
		{
		}
		return 0uL;
	}

	private void PersistState(string name, bool a)
	{
		if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null)
		{
			SaveLoadHandler.Instance.data.modStates[name] = a;
			SaveLoadHandler.Instance.SaveToDisk();
		}
	}

	private void RemoveMod(ModEntry mod, GameObject ui)
	{
		if (mod.retainedBundle != null)
		{
			try
			{
				mod.retainedBundle.Unload(unloadAllLoadedObjects: true);
			}
			catch
			{
			}
			mod.retainedBundle = null;
		}
		if (mod.type == ModType.MEObject)
		{
			if (mod.instance != null)
			{
				UnityEngine.Object.Destroy(mod.instance);
			}
			if (GlobalInstances.TryGetValue(mod.name, out var value) && value == mod.instance)
			{
				GlobalInstances.Remove(mod.name);
			}
		}
		try
		{
			if (File.Exists(mod.localPath))
			{
				File.Delete(mod.localPath);
			}
		}
		catch
		{
		}
		try
		{
			if (!string.IsNullOrEmpty(mod.extractedPath) && Directory.Exists(mod.extractedPath))
			{
				Directory.Delete(mod.extractedPath, recursive: true);
			}
		}
		catch
		{
		}
		try
		{
			string thumbPath = GetThumbPath(mod.name);
			if (File.Exists(thumbPath))
			{
				File.Delete(thumbPath);
			}
		}
		catch
		{
		}
		loadedMods.Remove(mod);
		UnityEngine.Object.Destroy(ui);
		AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
		if (avatarDanceHandler != null)
		{
			avatarDanceHandler.RescanMods();
		}
		LoadAllModsInFolder();
	}

	private T FindChildByName<T>(Transform root, string name) where T : Component
	{
		if (root == null || string.IsNullOrEmpty(name))
		{
			return null;
		}
		Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].name == name)
			{
				return componentsInChildren[i].GetComponent<T>();
			}
		}
		return null;
	}

	private Type ResolveType(string name)
	{
		Type type = Type.GetType(name);
		if (type != null)
		{
			return type;
		}
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			type = assemblies[i].GetType(name);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}

	private string ReadAuthorFromModInfo(string dir)
	{
		try
		{
			string path = Path.Combine(dir, "modinfo.json");
			if (File.Exists(path))
			{
				ModInfo modInfo = JsonUtility.FromJson<ModInfo>(File.ReadAllText(path));
				if (modInfo != null && !string.IsNullOrWhiteSpace(modInfo.author))
				{
					return "Author: " + modInfo.author.Trim();
				}
			}
		}
		catch
		{
		}
		return "Author: Unknown";
	}

	private string ReadTypeFromModType(string dir, string fallback)
	{
		try
		{
			string path = Path.Combine(dir, "mod_type.json");
			if (File.Exists(path))
			{
				ModTypeInfo modTypeInfo = JsonUtility.FromJson<ModTypeInfo>(File.ReadAllText(path));
				if (modTypeInfo != null && !string.IsNullOrWhiteSpace(modTypeInfo.type))
				{
					return "Type: " + modTypeInfo.type.Trim();
				}
			}
		}
		catch
		{
		}
		return "Type: " + fallback;
	}

	private string GetThumbPath(string modName)
	{
		return Path.Combine(Application.persistentDataPath, "Thumbnails", modName + "_thumb.png");
	}

	private void LoadThumbToRawImage(RawImage img, string modName)
	{
		if (!(img == null))
		{
			string thumbPath = GetThumbPath(modName);
			if (File.Exists(thumbPath))
			{
				byte[] data = File.ReadAllBytes(thumbPath);
				Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
				texture2D.LoadImage(data);
				img.texture = texture2D;
			}
		}
	}
}
