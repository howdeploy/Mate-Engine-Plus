using System;
using System.IO;
using CustomDancePlayer;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class ModRemoveButton : MonoBehaviour
{
	public Button button;

	public string filePath;

	public ulong workshopId;

	private void Awake()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		if (button != null)
		{
			button.onClick.AddListener(OnClick);
		}
	}

	private void OnClick()
	{
		ulong num = workshopId;
		if (num == 0L)
		{
			num = ResolveWorkshopIdForPath(filePath);
		}
		if (num != 0L && SteamManager.Initialized)
		{
			SteamWorkshopHandler instance = SteamWorkshopHandler.Instance;
			if (instance != null)
			{
				instance.UnsubscribeAndDelete(new PublishedFileId_t(num));
			}
		}
		if (!string.IsNullOrEmpty(filePath))
		{
			try
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
			catch
			{
			}
		}
		MEModHandler mEModHandler = UnityEngine.Object.FindFirstObjectByType<MEModHandler>();
		if (mEModHandler != null)
		{
			mEModHandler.SendMessage("LoadAllModsInFolder", SendMessageOptions.DontRequireReceiver);
		}
		AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
		if (avatarDanceHandler != null)
		{
			avatarDanceHandler.RescanMods();
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
}
