using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarLibraryMenu : MonoBehaviour
{
	[Serializable]
	public class DLCEntry
	{
		public GameObject prefab;

		public string displayName;

		public string author;

		public string version;

		public string fileType;

		public Texture2D thumbnail;
	}

	[Serializable]
	public class AvatarEntry
	{
		public string displayName;

		public string author;

		public string version;

		public string fileType;

		public string filePath;

		public string thumbnailPath;

		public int polygonCount;

		public bool isSteamWorkshop;

		public ulong steamFileId;

		public bool isNSFW;

		public bool isOwner;
	}

	[Header("Default Model")]
	public GameObject defaultAvatarPrefab;

	public Texture2D defaultAvatarThumbnail;

	public string defaultAvatarDisplayName = "Zome";

	public string defaultAvatarAuthor = "Yorshka";

	public string defaultAvatarVersion = "1.0";

	public string defaultAvatarFileType = "Built-in";

	[Header("UI References")]
	public GameObject avatarItemPrefab;

	public GameObject avatarItemPrefabDLC;

	public Transform contentParent;

	public GameObject libraryPanel;

	[Header("DLC Avatars")]
	public List<DLCEntry> dlcAvatars = new List<DLCEntry>();

	private Coroutine liveUpdateRoutine;

	[SerializeField]
	private float liveUpdateInterval = 3f;

	private List<AvatarEntry> avatarEntries = new List<AvatarEntry>();

	private string avatarsJsonPath => Path.Combine(Application.persistentDataPath, "avatars.json");

	private string thumbnailsFolder => Path.Combine(Application.persistentDataPath, "Thumbnails");

	private void Start()
	{
		if (!Directory.Exists(thumbnailsFolder))
		{
			Directory.CreateDirectory(thumbnailsFolder);
		}
		LoadAvatarList();
		RefreshUI();
	}

	public void OpenLibrary()
	{
		libraryPanel.SetActive(value: true);
		SteamWorkshopAutoLoader steamWorkshopAutoLoader = UnityEngine.Object.FindFirstObjectByType<SteamWorkshopAutoLoader>();
		if (steamWorkshopAutoLoader != null)
		{
			steamWorkshopAutoLoader.RefreshWorkshopAvatars();
		}
		if (liveUpdateRoutine != null)
		{
			StopCoroutine(liveUpdateRoutine);
		}
		liveUpdateRoutine = StartCoroutine(LiveUpdateWhileOpen());
	}

	public void CloseLibrary()
	{
		libraryPanel.SetActive(value: false);
		if (liveUpdateRoutine != null)
		{
			StopCoroutine(liveUpdateRoutine);
			liveUpdateRoutine = null;
		}
	}

	private IEnumerator LiveUpdateWhileOpen()
	{
		SteamWorkshopAutoLoader auto = UnityEngine.Object.FindFirstObjectByType<SteamWorkshopAutoLoader>();
		while (libraryPanel != null && libraryPanel.activeInHierarchy)
		{
			if (auto != null)
			{
				auto.RefreshWorkshopAvatars();
				if (auto.hadChangesLastRun)
				{
					ReloadAvatars();
				}
			}
			yield return new WaitForSeconds(liveUpdateInterval);
		}
	}

	private void LoadAvatarList()
	{
		avatarEntries.Clear();
		if (File.Exists(avatarsJsonPath))
		{
			try
			{
				string value = File.ReadAllText(avatarsJsonPath);
				avatarEntries = JsonConvert.DeserializeObject<List<AvatarEntry>>(value);
			}
			catch (Exception ex)
			{
				Debug.LogError("[AvatarLibraryMenu] Failed to load avatars.json: " + ex.Message);
			}
		}
		try
		{
			string fullPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Steam Workshop"));
			bool flag = false;
			foreach (AvatarEntry avatarEntry in avatarEntries)
			{
				if (!avatarEntry.isOwner)
				{
					string text = (string.IsNullOrEmpty(avatarEntry.filePath) ? "" : Path.GetFullPath(avatarEntry.filePath));
					bool flag2 = !string.IsNullOrEmpty(text) && File.Exists(text) && !text.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase);
					if (!avatarEntry.isSteamWorkshop && avatarEntry.steamFileId == 0 && flag2)
					{
						avatarEntry.isOwner = true;
						flag = true;
					}
				}
			}
			if (flag)
			{
				string contents = JsonConvert.SerializeObject(avatarEntries, Formatting.Indented);
				File.WriteAllText(avatarsJsonPath, contents);
			}
		}
		catch
		{
		}
	}

	private void RefreshUI()
	{
		foreach (Transform item4 in contentParent)
		{
			UnityEngine.Object.Destroy(item4.gameObject);
		}
		if (defaultAvatarPrefab != null)
		{
			GameObject item = UnityEngine.Object.Instantiate((avatarItemPrefabDLC != null) ? avatarItemPrefabDLC : avatarItemPrefab, contentParent);
			SetupDefaultAvatarItem(item);
		}
		foreach (DLCEntry dlcAvatar in dlcAvatars)
		{
			if (!(dlcAvatar.prefab == null))
			{
				GameObject item2 = UnityEngine.Object.Instantiate((avatarItemPrefabDLC != null) ? avatarItemPrefabDLC : avatarItemPrefab, contentParent);
				SetupDLCItem(item2, dlcAvatar);
			}
		}
		foreach (AvatarEntry avatarEntry in avatarEntries)
		{
			GameObject item3 = UnityEngine.Object.Instantiate(avatarItemPrefab, contentParent);
			SetupAvatarItem(item3, avatarEntry);
		}
	}

	private void SetupDefaultAvatarItem(GameObject item)
	{
		RawImage component = item.transform.Find("RawImage").GetComponent<RawImage>();
		TMP_Text component2 = item.transform.Find("Title").GetComponent<TMP_Text>();
		TMP_Text component3 = item.transform.Find("Author").GetComponent<TMP_Text>();
		TMP_Text component4 = item.transform.Find("Version").GetComponent<TMP_Text>();
		TMP_Text component5 = item.transform.Find("File Type").GetComponent<TMP_Text>();
		TMP_Text tMP_Text = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
		Button component6 = item.transform.Find("Button").GetComponent<Button>();
		if (component != null && defaultAvatarThumbnail != null)
		{
			component.texture = defaultAvatarThumbnail;
		}
		if (component2 != null)
		{
			component2.text = "Name: " + defaultAvatarDisplayName;
		}
		if (component3 != null)
		{
			component3.text = "Author: " + defaultAvatarAuthor;
		}
		if (component4 != null)
		{
			component4.text = "Version: " + defaultAvatarVersion;
		}
		if (component5 != null)
		{
			component5.text = "Format: " + defaultAvatarFileType;
		}
		if (tMP_Text != null && defaultAvatarPrefab != null)
		{
			int num = 0;
			MeshFilter[] componentsInChildren = defaultAvatarPrefab.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			foreach (MeshFilter meshFilter in componentsInChildren)
			{
				if (meshFilter.sharedMesh != null)
				{
					num += meshFilter.sharedMesh.triangles.Length / 3;
				}
			}
			SkinnedMeshRenderer[] componentsInChildren2 = defaultAvatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren2)
			{
				if (skinnedMeshRenderer.sharedMesh != null)
				{
					num += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
				}
			}
			tMP_Text.text = "Polygons: " + num;
		}
		component6.onClick.RemoveAllListeners();
		component6.onClick.AddListener(delegate
		{
			VRMLoader vRMLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
			if (vRMLoader != null)
			{
				vRMLoader.ActivateDefaultModel();
			}
		});
	}

	private void SetupDLCItem(GameObject item, DLCEntry dlc)
	{
		RawImage component = item.transform.Find("RawImage").GetComponent<RawImage>();
		TMP_Text component2 = item.transform.Find("Title").GetComponent<TMP_Text>();
		TMP_Text component3 = item.transform.Find("Author").GetComponent<TMP_Text>();
		TMP_Text component4 = item.transform.Find("Version").GetComponent<TMP_Text>();
		TMP_Text component5 = item.transform.Find("File Type").GetComponent<TMP_Text>();
		TMP_Text tMP_Text = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
		Button component6 = item.transform.Find("Button").GetComponent<Button>();
		item.transform.Find("Remove")?.GetComponent<Button>();
		item.transform.Find("Upload")?.GetComponent<Button>();
		Slider slider = item.transform.Find("UploadBar")?.GetComponent<Slider>();
		if (component2 != null)
		{
			component2.text = "Name: " + ((!string.IsNullOrEmpty(dlc.displayName)) ? dlc.displayName : dlc.prefab.name);
		}
		if (component3 != null)
		{
			component3.text = "Author: " + dlc.author;
		}
		if (component4 != null)
		{
			component4.text = "Version: " + dlc.version;
		}
		if (component5 != null)
		{
			component5.text = "Format: " + dlc.fileType;
		}
		int num = 0;
		MeshFilter[] componentsInChildren = dlc.prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		foreach (MeshFilter meshFilter in componentsInChildren)
		{
			if (meshFilter.sharedMesh != null)
			{
				num += meshFilter.sharedMesh.triangles.Length / 3;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = dlc.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren2)
		{
			if (skinnedMeshRenderer.sharedMesh != null)
			{
				num += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
			}
		}
		if (tMP_Text != null)
		{
			tMP_Text.text = "Polygons: " + num;
		}
		if (component != null && dlc.thumbnail != null)
		{
			component.texture = dlc.thumbnail;
		}
		component6.onClick.RemoveAllListeners();
		component6.onClick.AddListener(delegate
		{
			LoadAvatar(dlc.prefab.name);
		});
		if (slider != null)
		{
			slider.gameObject.SetActive(value: false);
		}
	}

	private void SetupAvatarItem(GameObject item, AvatarEntry entry)
	{
		RawImage component = item.transform.Find("RawImage").GetComponent<RawImage>();
		TMP_Text component2 = item.transform.Find("Title").GetComponent<TMP_Text>();
		TMP_Text component3 = item.transform.Find("Author").GetComponent<TMP_Text>();
		TMP_Text component4 = item.transform.Find("Version").GetComponent<TMP_Text>();
		TMP_Text component5 = item.transform.Find("File Type").GetComponent<TMP_Text>();
		TMP_Text tMP_Text = item.transform.Find("Polygons")?.GetComponent<TMP_Text>();
		Button component6 = item.transform.Find("Button").GetComponent<Button>();
		Button component7 = item.transform.Find("Remove").GetComponent<Button>();
		Button button = item.transform.Find("Upload")?.GetComponent<Button>();
		Slider uploadSlider = item.transform.Find("UploadBar")?.GetComponent<Slider>();
		Toggle nsfwToggle = item.transform.Find("NSFW")?.GetComponent<Toggle>();
		if (nsfwToggle != null)
		{
			nsfwToggle.isOn = entry.isNSFW;
			nsfwToggle.onValueChanged.RemoveAllListeners();
			nsfwToggle.onValueChanged.AddListener(delegate(bool val)
			{
				entry.isNSFW = val;
				AvatarEntry avatarEntry2 = avatarEntries.FirstOrDefault((AvatarEntry e) => e.filePath == entry.filePath);
				if (avatarEntry2 != null)
				{
					avatarEntry2.isNSFW = val;
					SaveAvatars();
				}
			});
		}
		if (component2 != null)
		{
			component2.text = "Name: " + entry.displayName;
		}
		if (component3 != null)
		{
			component3.text = "Author: " + entry.author;
		}
		if (component4 != null)
		{
			component4.text = "Version: " + entry.version;
		}
		if (component5 != null)
		{
			component5.text = "Format: " + entry.fileType;
		}
		if (tMP_Text != null)
		{
			tMP_Text.text = "Polygons: " + entry.polygonCount;
		}
		if (component != null && File.Exists(entry.thumbnailPath))
		{
			byte[] data = File.ReadAllBytes(entry.thumbnailPath);
			Texture2D texture2D = new Texture2D(2, 2);
			texture2D.LoadImage(data);
			component.texture = texture2D;
		}
		component6.onClick.RemoveAllListeners();
		component6.onClick.AddListener(delegate
		{
			LoadAvatar(entry.filePath);
		});
		DeleteButtonHoldHandler deleteButtonHoldHandler = component7.GetComponent<DeleteButtonHoldHandler>();
		if (deleteButtonHoldHandler == null)
		{
			deleteButtonHoldHandler = component7.gameObject.AddComponent<DeleteButtonHoldHandler>();
		}
		deleteButtonHoldHandler.entry = entry;
		deleteButtonHoldHandler.labelText = component7.GetComponentInChildren<TMP_Text>();
		deleteButtonHoldHandler.audioSource = item.GetComponentInChildren<AudioSource>();
		if (button != null)
		{
			button.gameObject.SetActive(entry.isOwner);
		}
		if (uploadSlider != null)
		{
			uploadSlider.gameObject.SetActive(value: false);
		}
		if (button != null && button.gameObject.activeSelf)
		{
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate
			{
				if (nsfwToggle != null)
				{
					entry.isNSFW = nsfwToggle.isOn;
				}
				AvatarEntry avatarEntry2 = avatarEntries.FirstOrDefault((AvatarEntry e) => e.filePath == entry.filePath);
				if (avatarEntry2 != null)
				{
					avatarEntry2.isNSFW = entry.isNSFW;
					SaveAvatars();
					SteamWorkshopHandler.Instance.UploadToWorkshop(avatarEntry2, uploadSlider);
				}
			});
			UploadButtonHoldHandler component8 = button.GetComponent<UploadButtonHoldHandler>();
			if (component8 != null)
			{
				AvatarEntry avatarEntry = avatarEntries.FirstOrDefault((AvatarEntry e) => e.filePath == entry.filePath);
				if (avatarEntry != null)
				{
					component8.entry = avatarEntry;
				}
				component8.progressSlider = uploadSlider;
				component8.labelText = button.GetComponentInChildren<TMP_Text>();
			}
		}
		if (uploadSlider != null)
		{
			uploadSlider.gameObject.SetActive(value: false);
		}
	}

	private void LoadAvatar(string path)
	{
		VRMLoader vRMLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		if (vRMLoader != null)
		{
			vRMLoader.LoadVRM(path);
		}
		else
		{
			Debug.LogError("[AvatarLibraryMenu] VRMLoader not found in scene!");
		}
	}

	public static void AddAvatarToLibrary(string displayName, string author, string version, string fileType, string filePath, Texture2D thumbnail, int polygonCount)
	{
		string path = Path.Combine(Application.persistentDataPath, "avatars.json");
		string text = Path.Combine(Application.persistentDataPath, "Thumbnails");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		List<AvatarEntry> list = new List<AvatarEntry>();
		if (File.Exists(path))
		{
			try
			{
				list = JsonConvert.DeserializeObject<List<AvatarEntry>>(File.ReadAllText(path));
			}
			catch
			{
			}
		}
		if (list.Exists((AvatarEntry e) => e.filePath == filePath))
		{
			Debug.Log("[AvatarLibraryMenu] VRM already exists in library, skipping: " + displayName);
			return;
		}
		string path2 = Path.GetFileNameWithoutExtension(filePath) + "_thumb.png";
		string text2 = Path.Combine(text, path2);
		if (thumbnail != null)
		{
			File.WriteAllBytes(text2, thumbnail.EncodeToPNG());
		}
		AvatarEntry item = new AvatarEntry
		{
			displayName = displayName,
			author = author,
			version = version,
			fileType = fileType,
			filePath = filePath,
			thumbnailPath = text2,
			polygonCount = polygonCount,
			isNSFW = false,
			isOwner = true
		};
		list.Add(item);
		string contents = JsonConvert.SerializeObject(list, Formatting.Indented);
		File.WriteAllText(path, contents);
	}

	public void ReloadAvatars()
	{
		LoadAvatarList();
		RefreshUI();
	}

	private void RemoveAvatar(AvatarEntry entryToRemove)
	{
		string path = Path.Combine(Application.persistentDataPath, "avatars.json");
		if (!File.Exists(path))
		{
			return;
		}
		List<AvatarEntry> source = new List<AvatarEntry>();
		try
		{
			source = JsonConvert.DeserializeObject<List<AvatarEntry>>(File.ReadAllText(path));
		}
		catch
		{
		}
		source = source.Where((AvatarEntry e) => e.filePath != entryToRemove.filePath).ToList();
		if (entryToRemove.isSteamWorkshop && File.Exists(entryToRemove.filePath))
		{
			try
			{
				File.Delete(entryToRemove.filePath);
				Debug.Log("[AvatarLibraryMenu] Deleted Workshop model file: " + entryToRemove.filePath);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[AvatarLibraryMenu] Could not delete Workshop model file: " + ex.Message);
			}
		}
		if (File.Exists(entryToRemove.thumbnailPath))
		{
			try
			{
				File.Delete(entryToRemove.thumbnailPath);
			}
			catch
			{
			}
		}
		string contents = JsonConvert.SerializeObject(source, Formatting.Indented);
		File.WriteAllText(path, contents);
		ReloadAvatars();
	}

	private void SaveAvatars()
	{
		string path = Path.Combine(Application.persistentDataPath, "avatars.json");
		string contents = JsonConvert.SerializeObject(avatarEntries, Formatting.Indented);
		File.WriteAllText(path, contents);
	}
}
