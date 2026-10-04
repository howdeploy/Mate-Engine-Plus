using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamWorkshopHandler : MonoBehaviour
{
	private class DanceMeta
	{
		public string songName;

		public string songAuthor;

		public string mmdAuthor;

		public float songLength;

		public string placeholderClipName;
	}

	private class ModInfo
	{
		public string author;

		public string description;
	}

	private static readonly AppId_t appId = new AppId_t(3625270u);

	private Coroutine activeProgressRoutine;

	public static SteamWorkshopHandler Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public void UploadToWorkshop(AvatarLibraryMenu.AvatarEntry entry, Slider progressBar = null)
	{
		if (!SteamManager.Initialized || !File.Exists(entry.filePath))
		{
			return;
		}
		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(value: true);
			progressBar.value = 0f;
		}
		string text = Path.Combine(Application.temporaryCachePath, "WorkshopUpload");
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Directory.CreateDirectory(text);
		string contentDir = Path.Combine(text, "Content");
		Directory.CreateDirectory(contentDir);
		string destFileName = Path.Combine(contentDir, Path.GetFileName(entry.filePath));
		File.Copy(entry.filePath, destFileName, overwrite: true);
		contentDir = contentDir.Replace("\\", "/");
		string copiedThumbnailPath = null;
		if (File.Exists(entry.thumbnailPath))
		{
			copiedThumbnailPath = Path.Combine(contentDir, Path.GetFileName(entry.thumbnailPath));
			File.Copy(entry.thumbnailPath, copiedThumbnailPath, overwrite: true);
			copiedThumbnailPath = copiedThumbnailPath.Replace("\\", "/");
		}
		try
		{
			string contents = JsonConvert.SerializeObject(new { entry.displayName, entry.author, entry.version, entry.fileType, entry.polygonCount, entry.isNSFW }, Formatting.Indented);
			File.WriteAllText(Path.Combine(contentDir, "metadata.json"), contents);
		}
		catch
		{
		}
		if (entry.steamFileId != 0L)
		{
			UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(entry.steamFileId));
			ApplyUpdateSettingsAvatar(entry, contentDir, copiedThumbnailPath, handle);
			SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(handle, "Updated avatar via Avatar Library");
			CallResult<SubmitItemUpdateResult_t> callResult = CallResult<SubmitItemUpdateResult_t>.Create();
			if (progressBar != null && Instance != null)
			{
				activeProgressRoutine = Instance.StartCoroutine(Instance.ProgressRoutine(progressBar));
			}
			callResult.Set(hAPICall, delegate(SubmitItemUpdateResult_t submitResult, bool submitFailure)
			{
				FinalizeUpload(submitResult, progressBar);
				if (submitResult.m_eResult == EResult.k_EResultOK)
				{
					OpenWorkshopPage(entry.steamFileId);
				}
			});
			return;
		}
		SteamAPICall_t hAPICall2 = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeFirst);
		CallResult<CreateItemResult_t>.Create().Set(hAPICall2, delegate(CreateItemResult_t result, bool bIOFailure)
		{
			if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
			{
				if (progressBar != null)
				{
					progressBar.gameObject.SetActive(value: false);
				}
			}
			else
			{
				ulong newFileId = result.m_nPublishedFileId.m_PublishedFileId;
				entry.steamFileId = newFileId;
				UGCUpdateHandle_t handle2 = SteamUGC.StartItemUpdate(appId, result.m_nPublishedFileId);
				ApplyUpdateSettingsAvatar(entry, contentDir, copiedThumbnailPath, handle2);
				SteamAPICall_t hAPICall3 = SteamUGC.SubmitItemUpdate(handle2, "Initial upload from Avatar Library");
				CallResult<SubmitItemUpdateResult_t> callResult2 = CallResult<SubmitItemUpdateResult_t>.Create();
				if (progressBar != null && Instance != null)
				{
					activeProgressRoutine = Instance.StartCoroutine(Instance.ProgressRoutine(progressBar));
				}
				callResult2.Set(hAPICall3, delegate(SubmitItemUpdateResult_t submitResult, bool submitFailure)
				{
					FinalizeUpload(submitResult, progressBar);
					if (submitResult.m_eResult == EResult.k_EResultOK)
					{
						SaveSteamFileIdAvatar(entry);
						OpenWorkshopPage(newFileId);
					}
				});
			}
		});
	}

	public void BeginUploadMod(string filePath, Slider progressBar = null)
	{
		UploadMod(filePath, null, null, isNSFW: false, null, 0uL, progressBar);
	}

	public void UploadMod(string filePath, string displayName, string author, bool isNSFW, string thumbnailPath, ulong existingWorkshopId, Slider progressBar = null)
	{
		if (!SteamManager.Initialized || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return;
		}
		if (progressBar != null)
		{
			progressBar.gameObject.SetActive(value: true);
			progressBar.value = 0f;
		}
		string text = Path.Combine(Application.temporaryCachePath, "WorkshopUpload_Mod");
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Directory.CreateDirectory(text);
		string contentDir = Path.Combine(text, "Content");
		Directory.CreateDirectory(contentDir);
		string destFileName = Path.Combine(contentDir, Path.GetFileName(filePath));
		File.Copy(filePath, destFileName, overwrite: true);
		contentDir = contentDir.Replace("\\", "/");
		string copiedThumbnailPath = null;
		if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
		{
			copiedThumbnailPath = Path.Combine(contentDir, Path.GetFileName(thumbnailPath));
			File.Copy(thumbnailPath, copiedThumbnailPath, overwrite: true);
			copiedThumbnailPath = copiedThumbnailPath.Replace("\\", "/");
		}
		if (!string.IsNullOrEmpty(copiedThumbnailPath) && File.Exists(copiedThumbnailPath))
		{
			string text2 = Path.Combine(contentDir, "thumb.png").Replace("\\", "/");
			string text3 = copiedThumbnailPath.Replace("\\", "/");
			if (!string.Equals(text3, text2, StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
				}
				catch
				{
				}
				File.Copy(text3, text2, overwrite: true);
			}
			copiedThumbnailPath = text2;
			try
			{
				string path = Path.Combine(contentDir, "metadata.json");
				string text4 = File.ReadAllText(path);
				File.WriteAllText(path, text4.TrimEnd('}', ' ', '\n', '\r') + ",\n  \"thumbnail\":\"thumb.png\"\n}");
			}
			catch
			{
			}
		}
		bool flag = false;
		string text5 = author;
		if (!string.IsNullOrEmpty(text5))
		{
			string a = text5.Trim();
			if (string.Equals(a, "Unknown", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "Author: Unknown", StringComparison.OrdinalIgnoreCase))
			{
				text5 = null;
			}
		}
		string text6 = null;
		string text7 = null;
		string extension = Path.GetExtension(filePath);
		if (extension.Equals(".me", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				using FileStream stream = File.OpenRead(filePath);
				using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
				flag = zipArchive.Entries.Any((ZipArchiveEntry e) => string.Equals(e.FullName, "dance_meta.json", StringComparison.OrdinalIgnoreCase));
				ZipArchiveEntry zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry e) => string.Equals(e.FullName, "mod_type.json", StringComparison.OrdinalIgnoreCase));
				if (zipArchiveEntry != null)
				{
					using Stream stream2 = zipArchiveEntry.Open();
					using StreamReader streamReader = new StreamReader(stream2);
					string value = streamReader.ReadToEnd();
					try
					{
						Dictionary<string, string> dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
						if (dictionary != null && dictionary.TryGetValue("type", out var value2))
						{
							string text8 = (value2 ?? "").Trim().ToLowerInvariant();
							switch (text8)
							{
							case "mod":
							case "sound":
							case "particle":
							case "animation":
							case "misc":
								text6 = char.ToUpperInvariant(text8[0]) + text8.Substring(1);
								break;
							}
						}
					}
					catch
					{
					}
				}
				ZipArchiveEntry zipArchiveEntry2 = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry e) => string.Equals(e.FullName, "modinfo.json", StringComparison.OrdinalIgnoreCase));
				if (zipArchiveEntry2 != null)
				{
					using Stream stream3 = zipArchiveEntry2.Open();
					using StreamReader streamReader2 = new StreamReader(stream3);
					string value3 = streamReader2.ReadToEnd();
					try
					{
						ModInfo modInfo = JsonConvert.DeserializeObject<ModInfo>(value3);
						if (modInfo != null)
						{
							if (string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(modInfo.author))
							{
								text5 = modInfo.author;
							}
							if (!string.IsNullOrWhiteSpace(modInfo.description))
							{
								text7 = modInfo.description;
							}
						}
					}
					catch
					{
					}
				}
				ZipArchiveEntry zipArchiveEntry3 = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry e) => string.Equals(e.FullName, "dance_meta.json", StringComparison.OrdinalIgnoreCase));
				if (zipArchiveEntry3 != null)
				{
					using MemoryStream memoryStream = new MemoryStream();
					using Stream stream4 = zipArchiveEntry3.Open();
					stream4.CopyTo(memoryStream);
					string value4 = Encoding.UTF8.GetString(memoryStream.ToArray());
					try
					{
						DanceMeta danceMeta = JsonConvert.DeserializeObject<DanceMeta>(value4);
						if (string.IsNullOrWhiteSpace(text5))
						{
							text5 = ((!string.IsNullOrWhiteSpace(danceMeta.songAuthor)) ? danceMeta.songAuthor : danceMeta.mmdAuthor);
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		else if (extension.Equals(".unity3d", StringComparison.OrdinalIgnoreCase))
		{
			flag = true;
		}
		string title = (string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(filePath) : displayName);
		string desc = ((!string.IsNullOrWhiteSpace(text7)) ? text7 : "Uploaded via MateEngine Mod Manager");
		if (!string.IsNullOrWhiteSpace(text5) && !string.Equals(text5.Trim(), "Unknown", StringComparison.OrdinalIgnoreCase))
		{
			desc = desc + "\nAuthor: " + text5.Trim();
		}
		List<string> tags = new List<string> { "Mods" };
		if (flag)
		{
			tags.Add("Dance");
		}
		else
		{
			tags.Add(string.IsNullOrEmpty(text6) ? "Mod" : text6);
		}
		if (isNSFW)
		{
			tags.Add("NSFW");
		}
		try
		{
			var value5 = new
			{
				title = title,
				author = text5,
				description = text7,
				isDance = flag,
				isNSFW = isNSFW,
				originalFile = Path.GetFileName(filePath)
			};
			File.WriteAllText(Path.Combine(contentDir, "metadata.json"), JsonConvert.SerializeObject(value5, Formatting.Indented));
		}
		catch
		{
		}
		if (existingWorkshopId != 0L)
		{
			UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(existingWorkshopId));
			ApplyUpdateSettingsMod(title, desc, tags, contentDir, copiedThumbnailPath, handle);
			SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(handle, "Updated mod via Mod Manager");
			CallResult<SubmitItemUpdateResult_t> callResult = CallResult<SubmitItemUpdateResult_t>.Create();
			if (progressBar != null && Instance != null)
			{
				activeProgressRoutine = Instance.StartCoroutine(Instance.ProgressRoutine(progressBar));
			}
			callResult.Set(hAPICall, delegate(SubmitItemUpdateResult_t submitResult, bool submitFailure)
			{
				FinalizeUpload(submitResult, progressBar);
				if (submitResult.m_eResult == EResult.k_EResultOK)
				{
					OpenWorkshopPage(existingWorkshopId);
				}
			});
			return;
		}
		SteamAPICall_t hAPICall2 = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeFirst);
		CallResult<CreateItemResult_t>.Create().Set(hAPICall2, delegate(CreateItemResult_t result, bool bIOFailure)
		{
			if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
			{
				if (progressBar != null)
				{
					progressBar.gameObject.SetActive(value: false);
				}
			}
			else
			{
				ulong newFileId = result.m_nPublishedFileId.m_PublishedFileId;
				UGCUpdateHandle_t handle2 = SteamUGC.StartItemUpdate(appId, result.m_nPublishedFileId);
				ApplyUpdateSettingsMod(title, desc, tags, contentDir, copiedThumbnailPath, handle2);
				SteamAPICall_t hAPICall3 = SteamUGC.SubmitItemUpdate(handle2, "Initial mod upload");
				CallResult<SubmitItemUpdateResult_t> callResult2 = CallResult<SubmitItemUpdateResult_t>.Create();
				if (progressBar != null && Instance != null)
				{
					activeProgressRoutine = Instance.StartCoroutine(Instance.ProgressRoutine(progressBar));
				}
				callResult2.Set(hAPICall3, delegate(SubmitItemUpdateResult_t submitResult, bool submitFailure)
				{
					FinalizeUpload(submitResult, progressBar);
					if (submitResult.m_eResult == EResult.k_EResultOK)
					{
						OpenWorkshopPage(newFileId);
					}
				});
			}
		});
	}

	private void ApplyUpdateSettingsAvatar(AvatarLibraryMenu.AvatarEntry entry, string contentDir, string thumbnailPath, UGCUpdateHandle_t handle)
	{
		SteamUGC.SetItemTitle(handle, entry.displayName ?? "Untitled Avatar");
		SteamUGC.SetItemDescription(handle, $"Uploaded via MateEngine\nAuthor: {entry.author}\nFormat: {entry.fileType}\nPolygons: {entry.polygonCount}");
		SteamUGC.SetItemVisibility(handle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
		List<string> list = new List<string> { "Avatar" };
		if (entry.fileType.Contains("1.X"))
		{
			list.Add("VRM1");
		}
		else
		{
			list.Add("VRM0");
		}
		if (entry.isNSFW)
		{
			list.Add("NSFW");
		}
		SteamUGC.SetItemTags(handle, list);
		SteamUGC.SetItemContent(handle, contentDir);
		if (!string.IsNullOrEmpty(thumbnailPath))
		{
			SteamUGC.SetItemPreview(handle, thumbnailPath);
		}
	}

	private void ApplyUpdateSettingsMod(string title, string description, List<string> tags, string contentDir, string thumbnailPath, UGCUpdateHandle_t handle)
	{
		SteamUGC.SetItemTitle(handle, string.IsNullOrWhiteSpace(title) ? "Untitled Mod" : title);
		SteamUGC.SetItemDescription(handle, string.IsNullOrWhiteSpace(description) ? "MateEngine Mod" : description);
		SteamUGC.SetItemVisibility(handle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
		SteamUGC.SetItemTags(handle, tags);
		SteamUGC.SetItemContent(handle, contentDir);
		if (!string.IsNullOrEmpty(thumbnailPath))
		{
			SteamUGC.SetItemPreview(handle, thumbnailPath);
		}
	}

	private void FinalizeUpload(SubmitItemUpdateResult_t result, Slider slider)
	{
		if (activeProgressRoutine != null && Instance != null)
		{
			Instance.StopCoroutine(activeProgressRoutine);
		}
		if (slider != null)
		{
			slider.value = 100f;
			slider.gameObject.SetActive(value: false);
		}
	}

	private void SaveSteamFileIdAvatar(AvatarLibraryMenu.AvatarEntry updatedEntry)
	{
		string path = Path.Combine(Application.persistentDataPath, "avatars.json");
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			List<AvatarLibraryMenu.AvatarEntry> list = JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(path));
			foreach (AvatarLibraryMenu.AvatarEntry item in list)
			{
				if (item.filePath == updatedEntry.filePath)
				{
					item.steamFileId = updatedEntry.steamFileId;
					string fullPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Steam Workshop"));
					string text = (string.IsNullOrEmpty(item.filePath) ? "" : Path.GetFullPath(item.filePath));
					if (!(item.isSteamWorkshop = !string.IsNullOrEmpty(text) && text.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase)))
					{
						item.isOwner = true;
					}
					break;
				}
			}
			File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
		}
		catch
		{
		}
	}

	private void OpenWorkshopPage(ulong fileId)
	{
		Application.OpenURL($"https://steamcommunity.com/sharedfiles/filedetails/?id={fileId}");
	}

	private IEnumerator ProgressRoutine(Slider slider)
	{
		float duration = 10f;
		float elapsed = 0f;
		slider.minValue = 0f;
		slider.maxValue = 100f;
		slider.value = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			slider.value = Mathf.Clamp01(elapsed / duration) * 100f;
			yield return null;
		}
		slider.value = 100f;
	}

	public void UnsubscribeAndDelete(PublishedFileId_t fileId)
	{
		if (SteamManager.Initialized)
		{
			SteamUGC.UnsubscribeItem(fileId);
		}
	}
}
