using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class RuntimeModelStats : MonoBehaviour
{
	[Header("UI Fields")]
	public TextMeshProUGUI polyText;

	public TextMeshProUGUI boneText;

	public TextMeshProUGUI vrmVersionText;

	public TextMeshProUGUI buildVersionText;

	[Header("Model Root Parent")]
	public Transform customModelRoot;

	[Header("Debug")]
	public bool liveUpdate = true;

	private GameObject currentCustomModel;

	private float updateInterval = 0.5f;

	private float timer;

	private void Start()
	{
		if (buildVersionText != null)
		{
			buildVersionText.text = "MEV: " + Application.version;
		}
		RefreshNow();
	}

	private void Update()
	{
		if (liveUpdate)
		{
			timer += Time.deltaTime;
			if (timer >= updateInterval)
			{
				timer = 0f;
				RefreshNow();
			}
		}
	}

	public void RefreshNow()
	{
		if (customModelRoot == null)
		{
			Debug.LogWarning("[RuntimeModelStats] Custom Model Root is not assigned.");
			return;
		}
		currentCustomModel = customModelRoot.GetComponentsInChildren<Transform>(includeInactive: true).FirstOrDefault((Transform t) => t.name.Contains("CustomVRM") && t.name.Contains("Clone"))?.gameObject;
		if (currentCustomModel == null)
		{
			polyText.text = "Polys: -";
			boneText.text = "Bones: -";
			vrmVersionText.text = "VRM: -";
		}
		else
		{
			polyText.text = "Polys: " + GetPolyCount(currentCustomModel);
			boneText.text = "Bones: " + (HasProperArmature(currentCustomModel) ? "Perfect" : "Failure");
			vrmVersionText.text = "VRM: " + GetVrmVersion(currentCustomModel);
		}
	}

	private int GetPolyCount(GameObject model)
	{
		return (from m in model.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
			where m.sharedMesh != null
			select m).Sum((SkinnedMeshRenderer m) => m.sharedMesh.triangles.Length / 3);
	}

	private bool HasProperArmature(GameObject model)
	{
		Transform transform = FindBoneByName(model.transform, "Hips");
		if (transform != null)
		{
			return transform.parent != model.transform;
		}
		return false;
	}

	private Transform FindBoneByName(Transform root, string name)
	{
		return root.GetComponentsInChildren<Transform>(includeInactive: true).FirstOrDefault((Transform t) => t.name == name);
	}

	private string GetVrmVersion(GameObject model)
	{
		List<string> source = (from m in model.GetComponentsInChildren<Renderer>(includeInactive: true).SelectMany((Renderer r) => r.sharedMaterials)
			where m != null && m.shader != null
			select m.shader.name).Distinct().ToList();
		bool flag = source.Any((string s) => s.Contains("MToon10"));
		bool flag2 = source.Any((string s) => s.Contains("MToon")) && !flag;
		if (flag)
		{
			return "1.X";
		}
		if (flag2)
		{
			return "0.X";
		}
		return "Unknown";
	}
}
