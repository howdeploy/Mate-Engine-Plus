using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class BlendshapeManager : MonoBehaviour
{
	private class BlendRef
	{
		public SkinnedMeshRenderer smr;

		public int index;

		public string shapeName;

		public string uniqueKey;
	}

	[Header("UI Targets")]
	public Transform contentParent;

	public GameObject blendshapeBlockPrefab;

	[Header("Behaviour")]
	public float rescanInterval = 0.75f;

	public bool onlyUnderActiveAvatar = true;

	private readonly List<GameObject> activeBlocks = new List<GameObject>();

	private List<BlendRef> currentRefs = new List<BlendRef>();

	private string currentSignature = "";

	private string currentAvatarName = "";

	public Button resetAllButton;

	private Dictionary<string, float> blendValues = new Dictionary<string, float>();

	private void OnEnable()
	{
		StartCoroutine(ScanLoop());
	}

	private IEnumerator ScanLoop()
	{
		while (true)
		{
			try
			{
				BuildRefsIfChanged();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[BlendshapeManager] ScanLoop error: " + ex.Message);
			}
			yield return new WaitForSeconds(rescanInterval);
		}
	}

	private void Start()
	{
		if (resetAllButton != null)
		{
			resetAllButton.onClick.RemoveAllListeners();
			resetAllButton.onClick.AddListener(ResetAllBlendshapes);
		}
	}

	public void ResetAllBlendshapes()
	{
		for (int i = 0; i < currentRefs.Count; i++)
		{
			BlendRef blendRef = currentRefs[i];
			blendValues[blendRef.uniqueKey] = 0f;
		}
		ApplyAllToAvatar();
		SaveToDisk();
	}

	private void BuildRefsIfChanged()
	{
		string avatarName;
		Transform transform = ResolveActiveAvatarRoot(out avatarName);
		if (string.IsNullOrEmpty(avatarName))
		{
			avatarName = "DefaultAvatar";
		}
		SkinnedMeshRenderer[] obj = ((onlyUnderActiveAvatar && transform != null) ? transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true) : UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>(includeInactive: true));
		List<BlendRef> list = new List<BlendRef>();
		SkinnedMeshRenderer[] array = obj;
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in array)
		{
			if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null || !skinnedMeshRenderer.gameObject.activeInHierarchy || !skinnedMeshRenderer.enabled)
			{
				continue;
			}
			int blendShapeCount = skinnedMeshRenderer.sharedMesh.blendShapeCount;
			if (blendShapeCount <= 0)
			{
				continue;
			}
			for (int j = 0; j < blendShapeCount; j++)
			{
				string blendShapeName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(j);
				if (!string.IsNullOrEmpty(blendShapeName))
				{
					string uniqueKey = GetTransformPath(skinnedMeshRenderer.transform, transform) + ":" + blendShapeName;
					list.Add(new BlendRef
					{
						smr = skinnedMeshRenderer,
						index = j,
						shapeName = blendShapeName,
						uniqueKey = uniqueKey
					});
				}
			}
		}
		list = list.OrderBy((BlendRef r) => r.uniqueKey, StringComparer.OrdinalIgnoreCase).ToList();
		string a = string.Join("|", list.Select((BlendRef r) => r.uniqueKey));
		bool num = !string.Equals(avatarName, currentAvatarName, StringComparison.Ordinal);
		bool flag = !string.Equals(a, currentSignature, StringComparison.Ordinal);
		if (num || flag)
		{
			currentAvatarName = avatarName;
			currentSignature = a;
			currentRefs = list;
			RebuildUIBlocks();
			LoadFromDisk();
			ApplyAllToAvatar();
		}
	}

	private void RebuildUIBlocks()
	{
		foreach (GameObject activeBlock in activeBlocks)
		{
			if ((bool)activeBlock)
			{
				UnityEngine.Object.Destroy(activeBlock);
			}
		}
		activeBlocks.Clear();
		blendValues.Clear();
		if (currentRefs.Count == 0)
		{
			return;
		}
		int num = Mathf.CeilToInt((float)currentRefs.Count / 9f);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(blendshapeBlockPrefab, contentParent);
			BlendshapeUIBlock component = gameObject.GetComponent<BlendshapeUIBlock>();
			if (component == null)
			{
				Debug.LogError("[BlendshapeManager] Prefab lacks BlendshapeUIBlock component.");
				UnityEngine.Object.Destroy(gameObject);
				continue;
			}
			for (int j = 0; j < 9; j++)
			{
				if (num2 >= currentRefs.Count)
				{
					component.ClearUnusedFrom(j);
					break;
				}
				BlendRef r = currentRefs[num2++];
				float num3 = 0f;
				try
				{
					num3 = r.smr.GetBlendShapeWeight(r.index);
				}
				catch
				{
				}
				if (!blendValues.ContainsKey(r.uniqueKey))
				{
					blendValues[r.uniqueKey] = num3;
				}
				string shapeName = r.shapeName;
				component.SetupSlot(j, shapeName, num3, delegate(float v)
				{
					blendValues[r.uniqueKey] = v;
					try
					{
						r.smr.SetBlendShapeWeight(r.index, v);
					}
					catch
					{
					}
					SaveToDisk();
				});
			}
			activeBlocks.Add(gameObject);
		}
		if (activeBlocks.Count > 0)
		{
			int num4 = currentRefs.Count % 9;
			if (num4 == 0)
			{
				num4 = 9;
			}
			BlendshapeUIBlock component2 = activeBlocks[activeBlocks.Count - 1].GetComponent<BlendshapeUIBlock>();
			if (component2 != null)
			{
				component2.ClearUnusedFrom(num4);
			}
		}
	}

	private void ApplyAllToAvatar()
	{
		foreach (BlendRef currentRef in currentRefs)
		{
			if (blendValues.TryGetValue(currentRef.uniqueKey, out var value))
			{
				try
				{
					currentRef.smr.SetBlendShapeWeight(currentRef.index, value);
				}
				catch
				{
				}
			}
		}
		int num = 0;
		foreach (GameObject activeBlock in activeBlocks)
		{
			BlendshapeUIBlock component = activeBlock.GetComponent<BlendshapeUIBlock>();
			if (component == null)
			{
				continue;
			}
			for (int i = 0; i < 9; i++)
			{
				if (num >= currentRefs.Count)
				{
					component.SetSlotActive(i, active: false);
					continue;
				}
				BlendRef blendRef = currentRefs[num++];
				float value2;
				float valueWithoutNotify = (blendValues.TryGetValue(blendRef.uniqueKey, out value2) ? value2 : 0f);
				if (component.sliders[i] != null)
				{
					component.sliders[i].SetValueWithoutNotify(valueWithoutNotify);
				}
			}
		}
	}

	private string GetSaveFolder()
	{
		string text = Path.Combine(Application.persistentDataPath, "Blendshapes");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		return text;
	}

	private string GetSavePath()
	{
		string text = MakeFileNameSafe(string.IsNullOrEmpty(currentAvatarName) ? "DefaultAvatar" : currentAvatarName);
		return Path.Combine(GetSaveFolder(), text + "_Blendshapes.json");
	}

	private static string MakeFileNameSafe(string name)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			name = name.Replace(oldChar, '_');
		}
		return name;
	}

	private void SaveToDisk()
	{
		try
		{
			string contents = JsonConvert.SerializeObject(blendValues, Formatting.Indented);
			File.WriteAllText(GetSavePath(), contents);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[BlendshapeManager] Save failed: " + ex.Message);
		}
	}

	private void LoadFromDisk()
	{
		string savePath = GetSavePath();
		if (!File.Exists(savePath))
		{
			return;
		}
		try
		{
			Dictionary<string, float> dictionary = JsonConvert.DeserializeObject<Dictionary<string, float>>(File.ReadAllText(savePath));
			if (dictionary != null)
			{
				blendValues = dictionary;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[BlendshapeManager] Load failed: " + ex.Message);
		}
	}

	private Transform ResolveActiveAvatarRoot(out string avatarName)
	{
		avatarName = null;
		VRMLoader vRMLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		if (vRMLoader != null)
		{
			string key = "SavedPathModel";
			if (PlayerPrefs.HasKey(key))
			{
				string text = PlayerPrefs.GetString(key);
				if (!string.IsNullOrEmpty(text))
				{
					avatarName = Path.GetFileNameWithoutExtension(text);
				}
			}
			Type typeFromHandle = typeof(VRMLoader);
			FieldInfo field = typeFromHandle.GetField("customModelOutput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field2 = typeFromHandle.GetField("mainModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			Transform transform = null;
			GameObject gameObject = field?.GetValue(vRMLoader) as GameObject;
			GameObject gameObject2 = field2?.GetValue(vRMLoader) as GameObject;
			if (gameObject != null)
			{
				foreach (Transform item in gameObject.transform)
				{
					if (item.gameObject.activeInHierarchy)
					{
						transform = item;
						break;
					}
				}
			}
			if (transform == null && gameObject2 != null && gameObject2.activeInHierarchy)
			{
				transform = gameObject2.transform;
			}
			if (transform != null && string.IsNullOrEmpty(avatarName))
			{
				avatarName = transform.name;
			}
			return transform;
		}
		List<SkinnedMeshRenderer> list = (from s in UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>(includeInactive: true)
			where s != null && s.gameObject.activeInHierarchy && s.enabled && s.sharedMesh != null && s.sharedMesh.blendShapeCount > 0
			select s).ToList();
		if (list.Count > 0)
		{
			Transform transform3 = null;
			int num = -1;
			foreach (Transform candidate in list.Select((SkinnedMeshRenderer s) => (!(s.rootBone != null)) ? s.transform : s.rootBone))
			{
				int num2 = list.Count((SkinnedMeshRenderer s) => IsAncestor(candidate, s.transform));
				if (num2 > num)
				{
					num = num2;
					transform3 = candidate;
				}
			}
			if (transform3 != null)
			{
				avatarName = transform3.root?.name ?? transform3.name;
				return transform3;
			}
		}
		avatarName = "DefaultAvatar";
		return null;
	}

	private static bool IsAncestor(Transform ancestor, Transform child)
	{
		Transform transform = child;
		while (transform != null)
		{
			if (transform == ancestor)
			{
				return true;
			}
			transform = transform.parent;
		}
		return false;
	}

	private static string GetTransformPath(Transform t, Transform stopAt)
	{
		List<string> list = new List<string>();
		Transform transform = t;
		while (transform != null && transform != stopAt)
		{
			list.Add(transform.name);
			transform = transform.parent;
		}
		list.Reverse();
		return string.Join("/", list);
	}
}
