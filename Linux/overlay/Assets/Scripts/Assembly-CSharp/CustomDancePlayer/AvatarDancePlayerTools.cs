using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomDancePlayer
{
	public class AvatarDancePlayerTools : MonoBehaviour
	{
		[Serializable]
		private class FavData
		{
			public List<string> titles = new List<string>();
		}

		public AvatarDanceHandler handler;

		public Transform contentRoot;

		public InputField searchInput;

		public TMP_InputField tmpSearchInput;

		public Toggle showFavoriteToggle;

		public bool ignoreCase = true;

		public string favoritesFileName = "favorite_songs.json";

		public Toggle loopToggle;

		public Toggle shuffleToggle;

		private readonly Dictionary<Transform, string> titleRaw = new Dictionary<Transform, string>();

		private readonly Dictionary<Transform, string> titleNorm = new Dictionary<Transform, string>();

		private readonly Dictionary<Transform, int> entryIndices = new Dictionary<Transform, int>();

		private readonly HashSet<Transform> wired = new HashSet<Transform>();

		private readonly HashSet<string> favorites = new HashSet<string>(StringComparer.Ordinal);

		private int lastChildCount = -1;

		private string lastQuery = "";

		private void Awake()
		{
			if (!handler)
			{
				handler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			}
			if (!contentRoot && (bool)handler)
			{
				contentRoot = handler.contentObject;
			}
			LoadFavorites();
			if (handler)
			{
				handler.ListRebuilt += RefreshList;
			}
		}

		private void OnEnable()
		{
			HookInputs(on: true);
			RefreshList();
		}

		private void OnDisable()
		{
			HookInputs(on: false);
		}

		private void OnDestroy()
		{
			if (handler)
			{
				handler.ListRebuilt -= RefreshList;
			}
		}

		private void Update()
		{
			if (!contentRoot && (bool)handler)
			{
				contentRoot = handler.contentObject;
			}
			if ((bool)contentRoot && contentRoot.childCount != lastChildCount)
			{
				RefreshList();
			}
		}

		private void HookInputs(bool on)
		{
			if ((bool)searchInput)
			{
				if (on)
				{
					searchInput.onValueChanged.AddListener(OnSearchText);
				}
				else
				{
					searchInput.onValueChanged.RemoveListener(OnSearchText);
				}
			}
			if ((bool)tmpSearchInput)
			{
				if (on)
				{
					tmpSearchInput.onValueChanged.AddListener(OnSearchText);
				}
				else
				{
					tmpSearchInput.onValueChanged.RemoveListener(OnSearchText);
				}
			}
			if ((bool)showFavoriteToggle)
			{
				if (on)
				{
					showFavoriteToggle.onValueChanged.AddListener(OnShowFavChanged);
				}
				else
				{
					showFavoriteToggle.onValueChanged.RemoveListener(OnShowFavChanged);
				}
			}
			if ((bool)loopToggle)
			{
				if (on)
				{
					loopToggle.onValueChanged.AddListener(OnLoopChanged);
				}
				else
				{
					loopToggle.onValueChanged.RemoveListener(OnLoopChanged);
				}
			}
			if ((bool)shuffleToggle)
			{
				if (on)
				{
					shuffleToggle.onValueChanged.AddListener(OnShuffleChanged);
				}
				else
				{
					shuffleToggle.onValueChanged.RemoveListener(OnShuffleChanged);
				}
			}
		}

		private void OnLoopChanged(bool v)
		{
			if (!handler)
			{
				handler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			}
			if ((bool)handler)
			{
				handler.loopOn = v;
			}
		}

		private void OnShuffleChanged(bool v)
		{
			if (!handler)
			{
				handler = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			}
			if ((bool)handler)
			{
				handler.shuffleOn = v;
			}
		}

		private void OnSearchText(string s)
		{
			ApplyFilter(s);
		}

		private void OnShowFavChanged(bool _)
		{
			ApplyFilter(lastQuery);
		}

		private void RefreshList()
		{
			if (!contentRoot && handler)
			{
				contentRoot = handler.contentObject;
			}
			ReindexAndWire();
			ApplyFilter(lastQuery);
		}

		private void ReindexAndWire()
		{
			titleRaw.Clear();
			titleNorm.Clear();
			entryIndices.Clear();
			if (!contentRoot)
			{
				return;
			}
			lastChildCount = contentRoot.childCount;
			for (int i = 0; i < contentRoot.childCount; i++)
			{
				Transform t = contentRoot.GetChild(i);
				string raw;
				if (handler)
				{
					if (!handler.TryGetListEntry(t, out int index, out raw))
					{
						continue;
					}
					entryIndices[t] = index;
				}
				else
				{
					raw = ExtractTitle(t);
				}
				string value = Normalize(raw);
				titleRaw[t] = raw;
				titleNorm[t] = value;
				Toggle toggle = FindToggleByExactName(t, "Favorite");
				if ((bool)toggle)
				{
					toggle.SetIsOnWithoutNotify(favorites.Contains(raw));
					if (wired.Add(t))
					{
						// Rows are reused for different tracks after a library refresh.
						toggle.onValueChanged.AddListener(delegate(bool v)
						{
							OnItemFavoriteChanged(t, v);
						});
					}
				}
			}
		}

		private void OnItemFavoriteChanged(Transform item, bool isOn)
		{
			string rawTitle;
			if (handler)
			{
				if (!handler.TryGetListEntry(item, out _, out rawTitle))
				{
					return;
				}
			}
			else if (!titleRaw.TryGetValue(item, out rawTitle))
			{
				return;
			}
			if (!string.IsNullOrEmpty(rawTitle))
			{
				if (isOn)
				{
					favorites.Add(rawTitle);
				}
				else
				{
					favorites.Remove(rawTitle);
				}
				SaveFavorites();
				RefreshList();
			}
		}

		private void ApplyFilter(string query)
		{
			lastQuery = Normalize(query);
			if (!contentRoot)
			{
				return;
			}
			bool flag = (bool)showFavoriteToggle && showFavoriteToggle.isOn;
			bool flag2 = string.IsNullOrEmpty(lastQuery);
			for (int i = 0; i < contentRoot.childCount; i++)
			{
				Transform child = contentRoot.GetChild(i);
				if (!titleRaw.TryGetValue(child, out var value))
				{
					child.gameObject.SetActive(false);
					continue;
				}
				if (!titleNorm.TryGetValue(child, out var value2))
				{
					value2 = Normalize(value);
				}
				bool flag3 = flag2 || value2.Contains(lastQuery);
				bool flag4 = !flag || favorites.Contains(value);
				child.gameObject.SetActive(flag3 && flag4);
			}
			if (!handler)
			{
				return;
			}
			if (flag)
			{
				List<int> list = new List<int>();
				for (int j = 0; j < contentRoot.childCount; j++)
				{
					Transform child2 = contentRoot.GetChild(j);
					if (child2.gameObject.activeSelf && entryIndices.TryGetValue(child2, out int index))
					{
						list.Add(index);
					}
				}
				handler.SetQueueByIndices(list);
			}
			else
			{
				handler.SetQueueByIndices(null);
			}
		}

		private string Normalize(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "";
			}
			s = s.Trim();
			s = s.Replace("\u200b", "").Replace("\u200c", "").Replace("\u200d", "")
				.Replace("\ufeff", "");
			if (!ignoreCase)
			{
				return s;
			}
			return s.ToLowerInvariant();
		}

		private string ExtractTitle(Transform item)
		{
			Text text = FindByExactName<Text>(item, "TitleFallback");
			if ((bool)text && !string.IsNullOrEmpty(text.text))
			{
				return text.text;
			}
			TMP_Text tMP_Text = FindByExactName<TMP_Text>(item, "Title");
			if ((bool)tMP_Text && !string.IsNullOrEmpty(tMP_Text.text))
			{
				return tMP_Text.text;
			}
			Text text2 = FindByNameContains<Text>(item, "titlefallback");
			if ((bool)text2 && !string.IsNullOrEmpty(text2.text))
			{
				return text2.text;
			}
			TMP_Text tMP_Text2 = FindByNameContains<TMP_Text>(item, "title");
			if ((bool)tMP_Text2 && !string.IsNullOrEmpty(tMP_Text2.text))
			{
				return tMP_Text2.text;
			}
			TMP_Text componentInChildren = item.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if ((bool)componentInChildren && !string.IsNullOrEmpty(componentInChildren.text))
			{
				return componentInChildren.text;
			}
			Text componentInChildren2 = item.GetComponentInChildren<Text>(includeInactive: true);
			if ((bool)componentInChildren2 && !string.IsNullOrEmpty(componentInChildren2.text))
			{
				return componentInChildren2.text;
			}
			return "";
		}

		private Toggle FindToggleByExactName(Transform root, string name)
		{
			Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].name == name)
				{
					return componentsInChildren[i].GetComponent<Toggle>();
				}
			}
			return null;
		}

		private T FindByExactName<T>(Transform root, string name) where T : Component
		{
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

		private T FindByNameContains<T>(Transform root, string partLower) where T : Component
		{
			Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].name.ToLowerInvariant().Contains(partLower))
				{
					return componentsInChildren[i].GetComponent<T>();
				}
			}
			return null;
		}

		private void SaveFavorites()
		{
			try
			{
				string contents = JsonUtility.ToJson(new FavData
				{
					titles = new List<string>(favorites)
				}, prettyPrint: true);
				File.WriteAllText(Path.Combine(Application.persistentDataPath, favoritesFileName), contents);
			}
			catch
			{
			}
		}

		private void LoadFavorites()
		{
			try
			{
				string path = Path.Combine(Application.persistentDataPath, favoritesFileName);
				if (!File.Exists(path))
				{
					return;
				}
				FavData favData = JsonUtility.FromJson<FavData>(File.ReadAllText(path));
				favorites.Clear();
				if (favData == null || favData.titles == null)
				{
					return;
				}
				for (int i = 0; i < favData.titles.Count; i++)
				{
					if (!string.IsNullOrEmpty(favData.titles[i]))
					{
						favorites.Add(favData.titles[i]);
					}
				}
			}
			catch
			{
			}
		}
	}
}
