using System;
using UnityEngine;

public class MEClothes : MonoBehaviour
{
	[Serializable]
	public class OutfitEntry
	{
		public string name;

		public string tag;

		public GameObject[] gameObjects;
	}

	[Tooltip("Used only to keep the script in builds. This instance will be ignored at runtime.")]
	public bool isScriptLoader;

	[Header("Outfit Entries (Max 8)")]
	public OutfitEntry[] entries = new OutfitEntry[8];

	public void ActivateOutfit(int index)
	{
		if (index < 0 || index >= entries.Length)
		{
			return;
		}
		OutfitEntry outfitEntry = entries[index];
		if (outfitEntry == null || outfitEntry.gameObjects == null)
		{
			return;
		}
		bool flag = IsAnyActive(outfitEntry.gameObjects);
		GameObject[] gameObjects;
		if (!string.IsNullOrEmpty(outfitEntry.tag))
		{
			for (int i = 0; i < entries.Length; i++)
			{
				if (i == index)
				{
					continue;
				}
				OutfitEntry outfitEntry2 = entries[i];
				if (outfitEntry2 == null || outfitEntry2.gameObjects == null || !(outfitEntry2.tag == outfitEntry.tag))
				{
					continue;
				}
				gameObjects = outfitEntry2.gameObjects;
				foreach (GameObject gameObject in gameObjects)
				{
					if (gameObject != null)
					{
						gameObject.SetActive(value: false);
					}
				}
			}
		}
		gameObjects = outfitEntry.gameObjects;
		foreach (GameObject gameObject2 in gameObjects)
		{
			if (gameObject2 != null)
			{
				gameObject2.SetActive(!flag);
			}
		}
	}

	private bool IsAnyActive(GameObject[] targets)
	{
		foreach (GameObject gameObject in targets)
		{
			if (gameObject != null && gameObject.activeSelf)
			{
				return true;
			}
		}
		return false;
	}
}
