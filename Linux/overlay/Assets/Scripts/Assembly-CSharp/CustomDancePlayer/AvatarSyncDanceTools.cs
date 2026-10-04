using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CustomDancePlayer
{
	public class AvatarSyncDanceTools : MonoBehaviour
	{
		public Toggle syncToggle;

		private FileStream lockStream;

		private string busPath;

		private string busTmpPath;

		private bool isMain;

		private void Awake()
		{
			isMain = GetInstanceIndex() == 0;
			string path = "avatar_dance_play_bus.json";
			AvatarDanceHandler avatarDanceHandler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			if (avatarDanceHandler != null && !string.IsNullOrEmpty(avatarDanceHandler.syncFileName))
			{
				path = avatarDanceHandler.syncFileName;
			}
			string text = Path.Combine(Application.persistentDataPath, "Sync");
			try
			{
				Directory.CreateDirectory(text);
			}
			catch
			{
			}
			busPath = Path.Combine(text, path);
			busTmpPath = busPath + ".tmp";
		}

		private void OnEnable()
		{
			if (syncToggle != null)
			{
				syncToggle.onValueChanged.AddListener(OnToggleChanged);
			}
			ApplyState();
		}

		private void OnDisable()
		{
			if (syncToggle != null)
			{
				syncToggle.onValueChanged.RemoveListener(OnToggleChanged);
			}
			ReleaseLock();
		}

		private void OnDestroy()
		{
			ReleaseLock();
		}

		private void OnToggleChanged(bool _)
		{
			ApplyState();
		}

		private void ApplyState()
		{
			if (isMain)
			{
				if (syncToggle == null || syncToggle.isOn)
				{
					ReleaseLock();
				}
				else
				{
					AcquireLock();
				}
			}
		}

		private void AcquireLock()
		{
			ReleaseLock();
			try
			{
				lockStream = new FileStream(busPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch
			{
				lockStream = null;
			}
		}

		private void ReleaseLock()
		{
			if (lockStream != null)
			{
				try
				{
					lockStream.Dispose();
				}
				catch
				{
				}
				lockStream = null;
			}
			TryCleanTmp();
		}

		private void TryCleanTmp()
		{
			try
			{
				if (File.Exists(busTmpPath))
				{
					File.Delete(busTmpPath);
				}
			}
			catch
			{
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
	}
}
