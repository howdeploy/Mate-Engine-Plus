using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SFB;
using UniGLTF;
using UniVRM10;
using UnityEngine;
using UnityEngine.UI;
using VRM;

public class VRMLoader : MonoBehaviour
{
	public Button loadVRMButton;

	public GameObject mainModel;

	public GameObject customModelOutput;

	public RuntimeAnimatorController animatorController;

	public GameObject componentTemplatePrefab;

	private GameObject currentModel;

	private bool isLoading;

	private const string LegacyModelPathKey = "SavedPathModel";

	private RuntimeGltfInstance currentGltf;

	private AssetBundle currentBundle;

	private void Start()
	{
		string text = ((SaveLoadHandler.Instance != null) ? SaveLoadHandler.Instance.data.selectedModelPath : null);
		if (string.IsNullOrEmpty(text) && PlayerPrefs.HasKey("SavedPathModel"))
		{
			text = PlayerPrefs.GetString("SavedPathModel");
			if (SaveLoadHandler.Instance != null)
			{
				SaveLoadHandler.Instance.data.selectedModelPath = text;
				SaveLoadHandler.Instance.SaveToDisk();
			}
			PlayerPrefs.DeleteKey("SavedPathModel");
			PlayerPrefs.Save();
		}
		if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data.enableRandomAvatar)
		{
			TryLoadRandomAvatar();
		}
		else if (!string.IsNullOrEmpty(text))
		{
			LoadVRM(text);
		}
	}

	private void TryLoadRandomAvatar()
	{
		List<string> list = new List<string>();
		if (mainModel != null)
		{
			list.Add("__DEFAULT__");
		}
		AvatarLibraryMenu avatarLibraryMenu = UnityEngine.Object.FindFirstObjectByType<AvatarLibraryMenu>();
		if (avatarLibraryMenu != null && avatarLibraryMenu.dlcAvatars != null)
		{
			for (int i = 0; i < avatarLibraryMenu.dlcAvatars.Count; i++)
			{
				GameObject gameObject = avatarLibraryMenu.dlcAvatars[i]?.prefab;
				if (gameObject != null)
				{
					list.Add(gameObject.name);
				}
			}
		}
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "avatars.json");
			if (File.Exists(path))
			{
				List<AvatarLibraryMenu.AvatarEntry> list2 = JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(path));
				if (list2 != null)
				{
					for (int j = 0; j < list2.Count; j++)
					{
						string filePath = list2[j].filePath;
						if (!string.IsNullOrEmpty(filePath))
						{
							list.Add(filePath);
						}
					}
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			ActivateDefaultModel();
			return;
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		string text = list[index];
		if (text == "__DEFAULT__")
		{
			ActivateDefaultModel();
		}
		else
		{
			LoadVRM(text);
		}
	}

	public void OpenFileDialogAndLoadVRM()
	{
		if (!isLoading)
		{
			isLoading = true;
			ExtensionFilter[] extensions = new ExtensionFilter[1]
			{
				new ExtensionFilter("Model Files", "vrm", "me", "prefab")
			};
			string[] array = StandaloneFileBrowser.OpenFilePanel("Select Model File", "", extensions, multiselect: false);
			if (array.Length != 0 && !string.IsNullOrEmpty(array[0]))
			{
				LoadVRM(array[0]);
			}
			isLoading = false;
		}
	}

	public async void LoadVRM(string path)
	{
		if (path.EndsWith(".me", StringComparison.OrdinalIgnoreCase))
		{
			LoadAssetBundleModel(path);
			if (SaveLoadHandler.Instance != null)
			{
				SaveLoadHandler.Instance.data.selectedModelPath = path;
				SaveLoadHandler.Instance.SaveToDisk();
			}
		}
		else if (IsDLCReference(path))
		{
			GameObject gameObject = FindDLCByName(path);
			if (gameObject != null)
			{
				GameObject loadedModel = UnityEngine.Object.Instantiate(gameObject);
				FinalizeLoadedModel(loadedModel, path);
				if (SaveLoadHandler.Instance != null)
				{
					SaveLoadHandler.Instance.data.selectedModelPath = path;
					SaveLoadHandler.Instance.SaveToDisk();
				}
			}
			else
			{
				Debug.LogError("[VRMLoader] DLC Prefab not found: " + path);
			}
		}
		else
		{
			if (!File.Exists(path))
			{
				return;
			}
			try
			{
				byte[] fileData = await Task.Run(() => File.ReadAllBytes(path));
				if (fileData == null || fileData.Length == 0)
				{
					return;
				}
				GameObject loadedModel2 = null;
				try
				{
					Vrm10Data vrm10Data = Vrm10Data.Parse(new GlbFileParser(path).Parse());
					if (vrm10Data != null)
					{
						using Vrm10Importer importer10 = new Vrm10Importer(vrm10Data);
						RuntimeGltfInstance runtimeGltfInstance = await importer10.LoadAsync(new ImmediateCaller());
						if (runtimeGltfInstance.Root != null)
						{
							loadedModel2 = runtimeGltfInstance.Root;
							currentGltf = runtimeGltfInstance;
							loadedModel2.AddComponent<GltfInstanceDisposer>().Bind(runtimeGltfInstance);
						}
					}
				}
				catch
				{
				}
				if (loadedModel2 == null)
				{
					try
					{
						using GltfData gltfData = new GlbBinaryParser(fileData, path).Parse();
						VRMImporterContext importer11 = null;
						try
						{
							importer11 = new VRMImporterContext(new VRMData(gltfData));
							RuntimeGltfInstance runtimeGltfInstance2 = await importer11.LoadAsync(new ImmediateCaller());
							if (runtimeGltfInstance2.Root != null)
							{
								loadedModel2 = runtimeGltfInstance2.Root;
								currentGltf = runtimeGltfInstance2;
								loadedModel2.AddComponent<GltfInstanceDisposer>().Bind(runtimeGltfInstance2);
							}
						}
						finally
						{
							importer11?.Dispose();
						}
					}
					catch
					{
						return;
					}
				}
				if (!(loadedModel2 == null))
				{
					FinalizeLoadedModel(loadedModel2, path);
					if (SaveLoadHandler.Instance != null)
					{
						SaveLoadHandler.Instance.data.selectedModelPath = path;
						SaveLoadHandler.Instance.SaveToDisk();
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("[VRMLoader] Failed to load model: " + ex.Message);
			}
		}
	}

	private void LoadAssetBundleModel(string path)
	{
		AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
		if (assetBundle == null)
		{
			Debug.LogError("[VRMLoader] Failed to load AssetBundle at: " + path);
			return;
		}
		GameObject gameObject = assetBundle.LoadAllAssets<GameObject>().FirstOrDefault();
		if (gameObject == null)
		{
			Debug.LogError("[VRMLoader] No prefab found in AssetBundle.");
			assetBundle.Unload(unloadAllLoadedObjects: true);
		}
		else
		{
			GameObject loadedModel = UnityEngine.Object.Instantiate(gameObject);
			FinalizeLoadedModel(loadedModel, path, assetBundle);
		}
	}

	private void FinalizeLoadedModel(GameObject loadedModel, string path, AssetBundle bundle = null)
	{
		DisableMainModel();
		ClearPreviousCustomModel();
		currentBundle = bundle;
		loadedModel.transform.SetParent(customModelOutput.transform, worldPositionStays: false);
		loadedModel.transform.localPosition = Vector3.zero;
		loadedModel.transform.localRotation = Quaternion.identity;
		loadedModel.transform.localScale = Vector3.one;
		currentModel = loadedModel;
		EnableSkinnedMeshRenderers(currentModel);
		AssignAnimatorController(currentModel);
		InjectComponentsFromPrefab(componentTemplatePrefab, currentModel);
		MEValueChanger mEValueChanger = UnityEngine.Object.FindFirstObjectByType<MEValueChanger>();
		if (mEValueChanger != null)
		{
			mEValueChanger.SendMessage("TryAttachCustomVRM", SendMessageOptions.DontRequireReceiver);
		}
		string text = Path.GetFileNameWithoutExtension(path);
		string author = "Unknown";
		string version = "Unknown";
		string fileType = "Unknown";
		Texture2D texture = null;
		bool flag = path.EndsWith(".me", StringComparison.OrdinalIgnoreCase);
		Vrm10Instance component = loadedModel.GetComponent<Vrm10Instance>();
		if (component != null && component.Vrm != null && component.Vrm.Meta != null)
		{
			text = component.Vrm.Meta.Name ?? text;
			author = ((component.Vrm.Meta.Authors != null && component.Vrm.Meta.Authors.Count > 0) ? component.Vrm.Meta.Authors[0] : "Unknown");
			version = component.Vrm.Meta.Version ?? "Unknown";
			fileType = (flag ? ".ME (VRM1.X)" : "VRM1.X");
			texture = component.Vrm.Meta.Thumbnail;
		}
		else
		{
			VRMMeta component2 = loadedModel.GetComponent<VRMMeta>();
			if (component2 != null && component2.Meta != null)
			{
				VRMMetaObject meta = component2.Meta;
				text = ((!string.IsNullOrEmpty(meta.Title)) ? meta.Title : text);
				author = ((!string.IsNullOrEmpty(meta.Author)) ? meta.Author : "Unknown");
				version = ((!string.IsNullOrEmpty(meta.Version)) ? meta.Version : "Unknown");
				fileType = (flag ? ".ME (VRM0.X)" : "VRM0.X");
				texture = meta.Thumbnail;
			}
		}
		Texture2D texture2D = MakeReadableCopy(texture);
		int totalPolygons = GetTotalPolygons(loadedModel);
		if (!IsDLCReference(path))
		{
			AvatarLibraryMenu.AddAvatarToLibrary(text, author, version, fileType, path, texture2D, totalPolygons);
		}
		if (texture2D != null)
		{
			UnityEngine.Object.Destroy(texture2D);
		}
		AvatarLibraryMenu avatarLibraryMenu = UnityEngine.Object.FindFirstObjectByType<AvatarLibraryMenu>();
		if (avatarLibraryMenu != null)
		{
			avatarLibraryMenu.ReloadAvatars();
		}
		StartCoroutine(DelayedRefreshStats());
		if (MEModLoader.Instance != null)
		{
			MEModLoader.Instance.AssignHandlersForCurrentAvatar(loadedModel);
		}
		StartCoroutine(ReleaseRamAndUnloadAssetsCo());
		SettingsHandlerUtility.ReloadAllSettingsHandlers();
	}

	public Texture2D MakeReadableCopy(Texture texture)
	{
		if (texture == null)
		{
			return null;
		}
		RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0);
		Graphics.Blit(texture, temporary);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
		texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		return texture2D;
	}

	public void ResetModel()
	{
		string path = Path.Combine(Application.persistentDataPath, "VRM");
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
		ClearPreviousCustomModel(skipRawImageCleanup: true);
		EnableMainModel();
		if (SaveLoadHandler.Instance != null)
		{
			SaveLoadHandler.Instance.data.selectedModelPath = "";
			SaveLoadHandler.Instance.SaveToDisk();
		}
		if (MEModLoader.Instance != null && mainModel != null)
		{
			MEModLoader.Instance.AssignHandlersForCurrentAvatar(mainModel);
		}
		StartCoroutine(ReleaseRamAndUnloadAssetsCo());
	}

	private void DisableMainModel()
	{
		if (mainModel != null)
		{
			mainModel.SetActive(value: false);
		}
	}

	private void EnableMainModel()
	{
		if (mainModel != null)
		{
			mainModel.SetActive(value: true);
		}
	}

	private void ClearPreviousCustomModel(bool skipRawImageCleanup = false)
	{
		if (customModelOutput != null)
		{
			foreach (Transform item in customModelOutput.transform)
			{
				if (!(item.gameObject == mainModel))
				{
					CleanupRawImages(item.gameObject);
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
		}
		if (currentBundle != null)
		{
			currentBundle.Unload(unloadAllLoadedObjects: true);
			currentBundle = null;
		}
		currentGltf = null;
		if (!skipRawImageCleanup)
		{
			CleanupAllRawImagesInScene();
		}
	}

	private void EnableSkinnedMeshRenderers(GameObject model)
	{
		SkinnedMeshRenderer[] componentsInChildren = model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = true;
		}
	}

	private void AssignAnimatorController(GameObject model)
	{
		Animator componentInChildren = model.GetComponentInChildren<Animator>();
		if (componentInChildren != null && animatorController != null)
		{
			componentInChildren.runtimeAnimatorController = animatorController;
		}
	}

	private void InjectComponentsFromPrefab(GameObject prefabTemplate, GameObject targetModel)
	{
		if (prefabTemplate == null || targetModel == null)
		{
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(prefabTemplate);
		Animator componentInChildren = targetModel.GetComponentInChildren<Animator>();
		MonoBehaviour[] components = gameObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in components)
		{
			Type type = monoBehaviour.GetType();
			if (targetModel.GetComponent(type) != null)
			{
				continue;
			}
			Component component = targetModel.AddComponent(type);
			CopyComponentValues(monoBehaviour, component);
			if (componentInChildren != null)
			{
				MethodInfo method = type.GetMethod("SetAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					method.Invoke(component, new object[1] { componentInChildren });
				}
				FieldInfo field = type.GetField("animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null && field.FieldType == typeof(Animator))
				{
					field.SetValue(component, componentInChildren);
				}
			}
		}
		UnityEngine.Object.Destroy(gameObject);
	}

	private void CopyComponentValues(Component source, Component destination)
	{
		Type type = source.GetType();
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.IsDefined(typeof(SerializeField), inherit: true) || fieldInfo.IsPublic)
			{
				fieldInfo.SetValue(destination, fieldInfo.GetValue(source));
			}
		}
		foreach (PropertyInfo item in from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			where p.CanWrite && p.GetSetMethod(nonPublic: true) != null
			select p)
		{
			try
			{
				item.SetValue(destination, item.GetValue(source));
			}
			catch
			{
			}
		}
	}

	private IEnumerator DelayedRefreshStats()
	{
		yield return null;
		RuntimeModelStats runtimeModelStats = UnityEngine.Object.FindFirstObjectByType<RuntimeModelStats>();
		if (runtimeModelStats != null)
		{
			runtimeModelStats.RefreshNow();
		}
	}

	public int GetTotalPolygons(GameObject model)
	{
		int num = 0;
		MeshFilter[] componentsInChildren = model.GetComponentsInChildren<MeshFilter>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Mesh sharedMesh = componentsInChildren[i].sharedMesh;
			if (sharedMesh != null)
			{
				num += sharedMesh.triangles.Length / 3;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			Mesh sharedMesh2 = componentsInChildren2[i].sharedMesh;
			if (sharedMesh2 != null)
			{
				num += sharedMesh2.triangles.Length / 3;
			}
		}
		return num;
	}

	public void ActivateDefaultModel()
	{
		ClearPreviousCustomModel(skipRawImageCleanup: true);
		EnableMainModel();
		if (SaveLoadHandler.Instance != null)
		{
			SaveLoadHandler.Instance.data.selectedModelPath = "";
			SaveLoadHandler.Instance.SaveToDisk();
		}
		if (MEModLoader.Instance != null && mainModel != null)
		{
			MEModLoader.Instance.AssignHandlersForCurrentAvatar(mainModel);
		}
		StartCoroutine(ReleaseRamAndUnloadAssetsCo());
		SettingsHandlerUtility.ReloadAllSettingsHandlers();
	}

	private IEnumerator ReleaseRamAndUnloadAssetsCo()
	{
		yield return Resources.UnloadUnusedAssets();
		yield return null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private void CleanupRawImages(GameObject obj)
	{
		if (!(obj == null))
		{
			RawImage[] componentsInChildren = obj.GetComponentsInChildren<RawImage>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].texture = null;
			}
		}
	}

	private void CleanupAllRawImagesInScene()
	{
		RawImage[] array = UnityEngine.Object.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].texture = null;
		}
	}

	private bool IsDLCReference(string path)
	{
		if (!File.Exists(path) && !path.EndsWith(".vrm") && !path.EndsWith(".me"))
		{
			return true;
		}
		return false;
	}

	private GameObject FindDLCByName(string name)
	{
		AvatarLibraryMenu avatarLibraryMenu = UnityEngine.Object.FindFirstObjectByType<AvatarLibraryMenu>();
		if (avatarLibraryMenu == null)
		{
			return null;
		}
		foreach (AvatarLibraryMenu.DLCEntry dlcAvatar in avatarLibraryMenu.dlcAvatars)
		{
			if (dlcAvatar.prefab != null && dlcAvatar.prefab.name == name)
			{
				return dlcAvatar.prefab;
			}
		}
		return null;
	}

	public GameObject GetCurrentModel()
	{
		return currentModel;
	}
}
