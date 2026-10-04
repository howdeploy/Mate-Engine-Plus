using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CustomDancePlayer;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;

public class SteamWorkshopAutoLoader : MonoBehaviour
{
	private struct WorkResult
	{
		public bool AvatarsChanged;

		public bool ModsChanged;
	}

	private const string WorkshopFolderName = "Steam Workshop";

	private readonly List<string> allowedExtensions = new List<string> { ".vrm", ".me", ".unity3d" };

	private AvatarLibraryMenu library;

	private Callback<DownloadItemResult_t> downloadCallback;

	private Callback<RemoteStoragePublishedFileSubscribed_t> subscribedCallback;

	private Callback<RemoteStoragePublishedFileUnsubscribed_t> unsubscribedCallback;

	private bool isRefreshing;

	private bool pendingRefresh;

	private string workshopFolderPath => Path.Combine(Application.persistentDataPath, "Steam Workshop");

	private string modsFolderPath => Path.Combine(Application.persistentDataPath, "Mods");

	public bool hadChangesLastRun { get; private set; }

	private string modsMapPath => Path.Combine(Application.persistentDataPath, "mods_workshop_map.json");

	private void Awake()
	{
		if (SteamManager.Initialized)
		{
			downloadCallback = Callback<DownloadItemResult_t>.Create(OnWorkshopItemDownloaded);
			subscribedCallback = Callback<RemoteStoragePublishedFileSubscribed_t>.Create(OnWorkshopItemSubscribed);
			unsubscribedCallback = Callback<RemoteStoragePublishedFileUnsubscribed_t>.Create(OnWorkshopItemUnsubscribed);
		}
	}

	private void Start()
	{
		if (SteamManager.Initialized)
		{
			library = UnityEngine.Object.FindFirstObjectByType<AvatarLibraryMenu>();
			Directory.CreateDirectory(workshopFolderPath);
			Directory.CreateDirectory(modsFolderPath);
			RefreshWorkshopItems();
		}
	}

	public void RefreshWorkshopAvatars()
	{
		RefreshWorkshopItems();
	}

	private void OnWorkshopItemSubscribed(RemoteStoragePublishedFileSubscribed_t data)
	{
		SteamUGC.DownloadItem(data.m_nPublishedFileId, bHighPriority: true);
		RefreshWorkshopItems();
	}

	private void OnWorkshopItemUnsubscribed(RemoteStoragePublishedFileUnsubscribed_t data)
	{
		RefreshWorkshopItems();
	}

	private void OnWorkshopItemDownloaded(DownloadItemResult_t result)
	{
		if (result.m_eResult == EResult.k_EResultOK)
		{
			RefreshWorkshopItems();
		}
	}

	public void RefreshWorkshopItems()
	{
		if (SteamManager.Initialized)
		{
			if (isRefreshing)
			{
				pendingRefresh = true;
			}
			else
			{
				StartCoroutine(RefreshRoutine());
			}
		}
	}

	private IEnumerator RefreshRoutine()
	{
		isRefreshing = true;
		pendingRefresh = false;
		hadChangesLastRun = false;
		List<PublishedFileId_t> subscribed = new List<PublishedFileId_t>();
		uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
		if (numSubscribedItems != 0)
		{
			PublishedFileId_t[] array = new PublishedFileId_t[numSubscribedItems];
			SteamUGC.GetSubscribedItems(array, numSubscribedItems);
			subscribed.AddRange(array);
		}
		List<(PublishedFileId_t id, string installPath, bool needsUpdate)> snapshot = new List<(PublishedFileId_t, string, bool)>();
		for (int i = 0; i < subscribed.Count; i++)
		{
			PublishedFileId_t fileId = subscribed[i];
			uint itemState = SteamUGC.GetItemState(fileId);
			bool num = (itemState & 4) != 0;
			bool needsUpdate = (itemState & 8) != 0;
			if (!num || needsUpdate)
			{
				SteamUGC.DownloadItem(fileId, bHighPriority: true);
			}
			bool installed = false;
			string installPath = null;
			for (float timeout = 10f; timeout > 0f; timeout -= 0.1f)
			{
				installed = SteamUGC.GetItemInstallInfo(fileId, out var _, out installPath, 2048u, out var _);
				if (installed && !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
				{
					break;
				}
				yield return new WaitForSeconds(0.1f);
			}
			if (installed && !string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
			{
				snapshot.Add((fileId, installPath, needsUpdate));
			}
			yield return null;
		}
		string persistentPath = Application.persistentDataPath;
		string tempCachePath = Application.temporaryCachePath;
		string workshopDir = workshopFolderPath;
		string modsDir = modsFolderPath;
		Task<WorkResult> task = Task.Run(delegate
		{
			WorkResult result2 = default(WorkResult);
			HashSet<ulong> subscribedIds = new HashSet<ulong>(snapshot.Select(((PublishedFileId_t id, string installPath, bool needsUpdate) s) => s.id.m_PublishedFileId));
			bool num2 = CleanupUnsubscribedWorkshopMods(subscribedIds, modsDir, tempCachePath, persistentPath);
			bool flag = CleanupUnsubscribedWorkshopAvatars(subscribedIds, workshopDir, persistentPath);
			List<AvatarLibraryMenu.AvatarEntry> list = ReadAvatarEntries(persistentPath);
			bool flag2 = false;
			bool modsChanged = num2;
			foreach (var item in snapshot)
			{
				string[] source = Array.Empty<string>();
				try
				{
					source = Directory.GetFiles(item.installPath, "*", SearchOption.TopDirectoryOnly);
				}
				catch
				{
				}
				string text = source.FirstOrDefault((string f) => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
				if (!string.IsNullOrEmpty(text))
				{
					switch (Path.GetExtension(text).ToLowerInvariant())
					{
					case ".unity3d":
					{
						bool num3 = CopyToMods(text, item.needsUpdate, modsDir);
						TryRecordModMapping(Path.Combine(modsDir, Path.GetFileName(text)), item.id.m_PublishedFileId, persistentPath);
						if (num3)
						{
							modsChanged = true;
						}
						break;
					}
					case ".me":
						if (IsDanceME(text))
						{
							bool num4 = CopyToMods(text, item.needsUpdate, modsDir);
							TryRecordModMapping(Path.Combine(modsDir, Path.GetFileName(text)), item.id.m_PublishedFileId, persistentPath);
							TryCopyThumbFromME(text, persistentPath);
							if (num4)
							{
								modsChanged = true;
							}
						}
						else if (HandleAvatarFile(item.id, text, item.needsUpdate, list, workshopDir, persistentPath))
						{
							flag2 = true;
						}
						break;
					case ".vrm":
						if (HandleAvatarFile(item.id, text, item.needsUpdate, list, workshopDir, persistentPath))
						{
							flag2 = true;
						}
						break;
					}
				}
			}
			if (flag2 || flag)
			{
				SaveAvatarEntries(list, persistentPath);
			}
			result2.AvatarsChanged = flag2 || flag;
			result2.ModsChanged = modsChanged;
			return result2;
		});
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (!task.IsFaulted)
		{
			WorkResult result = task.Result;
			if (result.AvatarsChanged && library != null)
			{
				library.ReloadAvatars();
			}
			if (result.ModsChanged)
			{
				NotifyMods();
			}
			hadChangesLastRun = result.AvatarsChanged || result.ModsChanged;
		}
		isRefreshing = false;
		if (pendingRefresh)
		{
			RefreshWorkshopItems();
		}
	}

	private bool HandleAvatarFile(PublishedFileId_t fileId, string sourcePath, bool needsUpdate, List<AvatarLibraryMenu.AvatarEntry> avatarEntries, string workshopDir, string persistentPath)
	{
		string fileName = Path.GetFileName(sourcePath);
		string targetPath = Path.Combine(workshopDir, fileName);
		AvatarLibraryMenu.AvatarEntry avatarEntry = avatarEntries.FirstOrDefault((AvatarLibraryMenu.AvatarEntry e) => e.filePath == targetPath);
		if (avatarEntry != null && avatarEntry.steamFileId != fileId.m_PublishedFileId)
		{
			targetPath = Path.Combine(workshopDir, $"{fileId.m_PublishedFileId}_{fileName}");
		}
		bool flag = CopyFileIfNeeded(sourcePath, targetPath, needsUpdate);
		bool flag2 = avatarEntries.Any((AvatarLibraryMenu.AvatarEntry e) => e.filePath == targetPath);
		string path = Path.GetDirectoryName(sourcePath) ?? "";
		string text = Path.GetFileNameWithoutExtension(sourcePath);
		string text2 = "Workshop";
		string text3 = "1.0";
		string text4 = (sourcePath.EndsWith(".me", StringComparison.OrdinalIgnoreCase) ? ".ME" : "VRM");
		int num = 0;
		bool flag3 = false;
		string path2 = Path.Combine(path, "metadata.json");
		if (File.Exists(path2))
		{
			try
			{
				Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path2));
				if (dictionary != null)
				{
					if (dictionary.TryGetValue("displayName", out var value))
					{
						text = value?.ToString() ?? text;
					}
					if (dictionary.TryGetValue("author", out var value2))
					{
						text2 = value2?.ToString() ?? text2;
					}
					if (dictionary.TryGetValue("version", out var value3))
					{
						text3 = value3?.ToString() ?? text3;
					}
					if (dictionary.TryGetValue("fileType", out var value4))
					{
						text4 = value4?.ToString() ?? text4;
					}
					if (dictionary.TryGetValue("polygonCount", out var value5))
					{
						num = Convert.ToInt32(value5);
					}
					if (dictionary.TryGetValue("isNSFW", out var value6))
					{
						flag3 = Convert.ToBoolean(value6);
					}
				}
			}
			catch
			{
			}
		}
		string text5 = Path.Combine(persistentPath, "Thumbnails");
		try
		{
			Directory.CreateDirectory(text5);
		}
		catch
		{
		}
		string path3 = Path.GetFileNameWithoutExtension(targetPath) + "_thumb.png";
		string text6 = Path.Combine(path, Path.GetFileNameWithoutExtension(sourcePath) + "_thumb.png");
		string text7 = Path.Combine(Path.GetDirectoryName(targetPath) ?? "", Path.GetFileNameWithoutExtension(targetPath) + "_thumb.png");
		string text8 = (File.Exists(text6) ? text6 : text7);
		string text9 = "";
		if (File.Exists(text8))
		{
			try
			{
				string text10 = Path.Combine(text5, Path.GetFileName(path3));
				CopyFileOverwrite(text8, text10);
				text9 = text10;
			}
			catch
			{
			}
		}
		if (!flag2)
		{
			AvatarLibraryMenu.AvatarEntry item = new AvatarLibraryMenu.AvatarEntry
			{
				displayName = text,
				author = text2,
				version = text3,
				fileType = text4,
				filePath = targetPath,
				thumbnailPath = text9,
				polygonCount = num,
				isSteamWorkshop = true,
				steamFileId = fileId.m_PublishedFileId,
				isNSFW = flag3,
				isOwner = false
			};
			avatarEntries.Add(item);
			return true;
		}
		AvatarLibraryMenu.AvatarEntry avatarEntry2 = avatarEntries.First((AvatarLibraryMenu.AvatarEntry e) => e.filePath == targetPath);
		bool flag4 = false;
		if (avatarEntry2.displayName != text)
		{
			avatarEntry2.displayName = text;
			flag4 = true;
		}
		if (avatarEntry2.author != text2)
		{
			avatarEntry2.author = text2;
			flag4 = true;
		}
		if (avatarEntry2.version != text3)
		{
			avatarEntry2.version = text3;
			flag4 = true;
		}
		if (avatarEntry2.fileType != text4)
		{
			avatarEntry2.fileType = text4;
			flag4 = true;
		}
		if (avatarEntry2.polygonCount != num)
		{
			avatarEntry2.polygonCount = num;
			flag4 = true;
		}
		if (avatarEntry2.isNSFW != flag3)
		{
			avatarEntry2.isNSFW = flag3;
			flag4 = true;
		}
		if (!string.IsNullOrEmpty(text9) && avatarEntry2.thumbnailPath != text9)
		{
			avatarEntry2.thumbnailPath = text9;
			flag4 = true;
		}
		if (avatarEntry2.steamFileId == 0L)
		{
			avatarEntry2.isSteamWorkshop = true;
			avatarEntry2.steamFileId = fileId.m_PublishedFileId;
			flag4 = true;
		}
		if (flag4 || flag)
		{
			return true;
		}
		return false;
	}

	private bool IsDanceME(string mePath)
	{
		try
		{
			using FileStream stream = new FileStream(mePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			bool num = zipArchive.Entries.Any((ZipArchiveEntry e) => string.Equals(e.FullName, "dance_meta.json", StringComparison.OrdinalIgnoreCase));
			bool flag = zipArchive.Entries.Any((ZipArchiveEntry e) => e.FullName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase));
			return num || flag;
		}
		catch
		{
			return false;
		}
	}

	private bool CopyToMods(string sourcePath, bool needsUpdate, string modsDir)
	{
		try
		{
			Directory.CreateDirectory(modsDir);
			string text = Path.Combine(modsDir, Path.GetFileName(sourcePath));
			if (!File.Exists(text) || needsUpdate)
			{
				CopyFileOverwrite(sourcePath, text);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool CopyFileIfNeeded(string source, string target, bool needsUpdate)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(target) ?? "");
			if (!File.Exists(target) || needsUpdate)
			{
				CopyFileOverwrite(source, target);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private void CopyFileOverwrite(string source, string target)
	{
		try
		{
			using FileStream fileStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, useAsync: true);
			using FileStream destination = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 131072, useAsync: true);
			fileStream.CopyTo(destination);
		}
		catch
		{
		}
	}

	private void TryRecordModMapping(string targetPath, ulong fileId, string persistentPath)
	{
		try
		{
			Dictionary<string, ulong> dictionary = LoadModWorkshopMap(persistentPath);
			string fileName = Path.GetFileName(targetPath);
			dictionary[fileName] = fileId;
			SaveModWorkshopMap(dictionary, persistentPath);
		}
		catch
		{
		}
	}

	private Dictionary<string, ulong> LoadModWorkshopMap(string persistentPath)
	{
		try
		{
			string path = Path.Combine(persistentPath, "mods_workshop_map.json");
			if (File.Exists(path))
			{
				return JsonConvert.DeserializeObject<Dictionary<string, ulong>>(File.ReadAllText(path)) ?? new Dictionary<string, ulong>();
			}
		}
		catch
		{
		}
		return new Dictionary<string, ulong>();
	}

	private void SaveModWorkshopMap(Dictionary<string, ulong> map, string persistentPath)
	{
		try
		{
			File.WriteAllText(Path.Combine(persistentPath, "mods_workshop_map.json"), JsonConvert.SerializeObject(map, Formatting.Indented));
		}
		catch
		{
		}
	}

	private bool CleanupUnsubscribedWorkshopMods(HashSet<ulong> subscribedIds, string modsDir, string tempCachePath, string persistentPath)
	{
		Dictionary<string, ulong> dictionary = LoadModWorkshopMap(persistentPath);
		bool flag = false;
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, ulong> item in dictionary)
		{
			if (subscribedIds.Contains(item.Value))
			{
				continue;
			}
			string path = Path.Combine(modsDir, item.Key);
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch
			{
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.Key);
			string path2 = Path.Combine(tempCachePath, "ME_Cache", fileNameWithoutExtension);
			try
			{
				if (Directory.Exists(path2))
				{
					Directory.Delete(path2, recursive: true);
				}
			}
			catch
			{
			}
			string path3 = Path.Combine(persistentPath, "Thumbnails", fileNameWithoutExtension + "_thumb.png");
			try
			{
				if (File.Exists(path3))
				{
					File.Delete(path3);
				}
			}
			catch
			{
			}
			list.Add(item.Key);
			flag = true;
		}
		for (int i = 0; i < list.Count; i++)
		{
			dictionary.Remove(list[i]);
		}
		if (flag)
		{
			SaveModWorkshopMap(dictionary, persistentPath);
		}
		return flag;
	}

	private bool CleanupUnsubscribedWorkshopAvatars(HashSet<ulong> subscribedIds, string workshopDir, string persistentPath)
	{
		List<AvatarLibraryMenu.AvatarEntry> list = ReadAvatarEntries(persistentPath);
		bool flag = false;
		string fullPath = Path.GetFullPath(workshopDir);
		string fullPath2 = Path.GetFullPath(Path.Combine(persistentPath, "Thumbnails"));
		for (int num = list.Count - 1; num >= 0; num--)
		{
			AvatarLibraryMenu.AvatarEntry avatarEntry = list[num];
			if (avatarEntry.isSteamWorkshop && avatarEntry.steamFileId != 0L && !subscribedIds.Contains(avatarEntry.steamFileId))
			{
				string text = (string.IsNullOrEmpty(avatarEntry.filePath) ? "" : Path.GetFullPath(avatarEntry.filePath));
				string text2 = (string.IsNullOrEmpty(avatarEntry.thumbnailPath) ? "" : Path.GetFullPath(avatarEntry.thumbnailPath));
				if (!string.IsNullOrEmpty(text) && text.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						if (File.Exists(text))
						{
							File.Delete(text);
						}
					}
					catch
					{
					}
					try
					{
						if (!string.IsNullOrEmpty(text2) && text2.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase) && File.Exists(text2))
						{
							File.Delete(text2);
						}
					}
					catch
					{
					}
					list.RemoveAt(num);
					flag = true;
				}
				else
				{
					avatarEntry.isSteamWorkshop = false;
					flag = true;
				}
			}
		}
		if (flag)
		{
			SaveAvatarEntries(list, persistentPath);
		}
		return flag;
	}

	private List<AvatarLibraryMenu.AvatarEntry> ReadAvatarEntries(string persistentPath)
	{
		string path = Path.Combine(persistentPath, "avatars.json");
		if (!File.Exists(path))
		{
			return new List<AvatarLibraryMenu.AvatarEntry>();
		}
		try
		{
			return JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(path)) ?? new List<AvatarLibraryMenu.AvatarEntry>();
		}
		catch
		{
			return new List<AvatarLibraryMenu.AvatarEntry>();
		}
	}

	private void SaveAvatarEntries(List<AvatarLibraryMenu.AvatarEntry> entries, string persistentPath)
	{
		string path = Path.Combine(persistentPath, "avatars.json");
		try
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(entries, Formatting.Indented));
		}
		catch
		{
		}
	}

	private void TryCopyThumbFromME(string mePath, string persistentPath)
	{
		try
		{
			using FileStream stream = File.OpenRead(mePath);
			using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			ZipArchiveEntry entry = zipArchive.GetEntry("thumb.png");
			if (entry == null)
			{
				return;
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(mePath);
			string text = Path.Combine(persistentPath, "Thumbnails");
			Directory.CreateDirectory(text);
			string path = Path.Combine(text, fileNameWithoutExtension + "_thumb.png");
			using Stream stream2 = entry.Open();
			using FileStream destination = File.Create(path);
			stream2.CopyTo(destination);
		}
		catch
		{
		}
	}

	private void NotifyMods()
	{
		StartCoroutine(NotifyModsNextFrame());
	}

	private IEnumerator NotifyModsNextFrame()
	{
		yield return null;
		MEModHandler mEModHandler = UnityEngine.Object.FindFirstObjectByType<MEModHandler>();
		if (mEModHandler != null)
		{
			MethodInfo method = typeof(MEModHandler).GetMethod("LoadAllModsInFolder", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
			{
				method.Invoke(mEModHandler, null);
			}
		}
		AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
		if (avatarDanceHandler != null)
		{
			avatarDanceHandler.RescanMods();
		}
	}
}
