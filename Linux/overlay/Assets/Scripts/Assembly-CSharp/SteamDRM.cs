using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Steamworks;
using UnityEngine;

public static class SteamDRM
{
	[Serializable]
	private class TokenData
	{
		public string steamId;

		public int appId;

		public long exp;

		public List<int> dlc;
	}

	private static bool initialized;

	private static bool entitled;

	private static int currentAppId;

	private static long expUtcTicks;

	private static HashSet<int> dlc = new HashSet<int>();

	private static string TokenPath => Path.Combine(Application.persistentDataPath, "SteamDRM.token");

	public static bool Initialized => initialized;

	public static bool IsEntitled => entitled;

	public static void Initialize(int appId, int ttlDays = 14)
	{
		if (!initialized || currentAppId != appId)
		{
			currentAppId = appId;
			initialized = true;
			if (!TryInitLive(appId, ttlDays))
			{
				LoadToken();
				entitled = ValidateToken(appId);
			}
		}
	}

	public static bool TryInitLive(int appId, int ttlDays = 14)
	{
		try
		{
			if (!SteamAPI.Init())
			{
				return false;
			}
			if (!SteamUser.BLoggedOn())
			{
				return false;
			}
			if (!SteamApps.BIsSubscribedApp(new AppId_t((uint)appId)))
			{
				return false;
			}
			dlc.Clear();
			int dLCCount = SteamApps.GetDLCCount();
			for (int i = 0; i < dLCCount; i++)
			{
				if (SteamApps.BGetDLCDataByIndex(i, out var pAppID, out var _, out var _, 256) && SteamApps.BIsDlcInstalled(pAppID))
				{
					dlc.Add((int)pAppID.m_AppId);
				}
			}
			expUtcTicks = DateTime.UtcNow.AddDays(ttlDays).Ticks;
			SaveToken(new TokenData
			{
				steamId = SteamUser.GetSteamID().ToString(),
				appId = appId,
				exp = expUtcTicks,
				dlc = new List<int>(dlc)
			});
			entitled = true;
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool HasDLC(int dlcId)
	{
		try
		{
			if (entitled)
			{
				return SteamApps.BIsDlcInstalled(new AppId_t((uint)dlcId)) || dlc.Contains(dlcId);
			}
		}
		catch
		{
		}
		return dlc.Contains(dlcId);
	}

	public static void Invalidate()
	{
		entitled = false;
		expUtcTicks = 0L;
		dlc.Clear();
	}

	private static bool ValidateToken(int appId)
	{
		if (expUtcTicks <= 0)
		{
			return false;
		}
		if (DateTime.UtcNow.Ticks >= expUtcTicks)
		{
			return false;
		}
		if (appId != 0 && currentAppId != 0 && appId != currentAppId)
		{
			return false;
		}
		return true;
	}

	private static void SaveToken(TokenData td)
	{
		string s = JsonUtility.ToJson(td);
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		byte[] key = DeriveKey(currentAppId);
		byte[] iv = DeriveIV(currentAppId);
		byte[] bytes2 = Encrypt(bytes, key, iv);
		try
		{
			File.WriteAllBytes(TokenPath, bytes2);
		}
		catch
		{
		}
	}

	private static void LoadToken()
	{
		expUtcTicks = 0L;
		dlc.Clear();
		try
		{
			if (!File.Exists(TokenPath))
			{
				entitled = false;
				return;
			}
			byte[] data = File.ReadAllBytes(TokenPath);
			byte[] key = DeriveKey(currentAppId);
			byte[] iv = DeriveIV(currentAppId);
			byte[] bytes = Decrypt(data, key, iv);
			TokenData tokenData = JsonUtility.FromJson<TokenData>(Encoding.UTF8.GetString(bytes));
			if (tokenData == null)
			{
				entitled = false;
				return;
			}
			expUtcTicks = tokenData.exp;
			dlc = ((tokenData.dlc != null) ? new HashSet<int>(tokenData.dlc) : new HashSet<int>());
			entitled = ValidateToken(tokenData.appId);
		}
		catch
		{
			expUtcTicks = 0L;
			dlc.Clear();
			entitled = false;
		}
	}

	private static byte[] DeriveKey(int appId)
	{
		string s = Application.companyName + "|" + Application.productName + "|" + SystemInfo.deviceUniqueIdentifier + "|" + Environment.UserName + "|" + appId;
		using SHA256 sHA = SHA256.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(s));
	}

	private static byte[] DeriveIV(int appId)
	{
		string s = SystemInfo.operatingSystem + "|" + Environment.MachineName + "|" + appId;
		using MD5 mD = MD5.Create();
		return mD.ComputeHash(Encoding.UTF8.GetBytes(s));
	}

	private static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
	{
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = iv.AsSpan(0, 16).ToArray();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform cryptoTransform = aes.CreateEncryptor();
		return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
	}

	private static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
	{
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = iv.AsSpan(0, 16).ToArray();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using ICryptoTransform cryptoTransform = aes.CreateDecryptor();
		return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
	}
}
