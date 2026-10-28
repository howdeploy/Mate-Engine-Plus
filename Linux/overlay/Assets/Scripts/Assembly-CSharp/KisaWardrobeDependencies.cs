using System;
using System.Collections.Generic;
using UniVRM10;
using UnityEngine;

// Keep the shipped MEClothes serialization unchanged: player scene type trees are stripped.
public class KisaWardrobeDependencies : MonoBehaviour
{
	[Serializable]
	public class Rule
	{
		public int outfitIndex;
		public GameObject[] visibleWhenOff;
		public GameObject[] hiddenWhenOn;
		public VRM10SpringBoneColliderGroup[] colliderGroups;
	}

	public Rule[] rules = new Rule[0];

	private readonly Dictionary<VRM10SpringBoneColliderGroup, List<VRM10SpringBoneCollider>> wardrobeColliders =
		new Dictionary<VRM10SpringBoneColliderGroup, List<VRM10SpringBoneCollider>>();

	private void Start()
	{
		RefreshVisibility();
	}

	public void RefreshVisibility()
	{
		var clothes = GetComponent<MEClothes>();
		if (clothes == null || clothes.isScriptLoader || clothes.entries == null || rules == null) return;
		bool[] enabled = new bool[clothes.entries.Length];
		for (int i = 0; i < clothes.entries.Length; i++)
			if (clothes.entries[i]?.gameObjects != null)
				foreach (var target in clothes.entries[i].gameObjects)
					if (target != null && target.activeSelf) enabled[i] = true;
		// Restore selected garments first; covers are applied afterwards.
		for (int i = 0; i < clothes.entries.Length; i++)
			if (clothes.entries[i]?.gameObjects != null)
				foreach (var target in clothes.entries[i].gameObjects)
					if (target != null) target.SetActive(enabled[i]);
		bool physicsChanged = false;
		foreach (var rule in rules)
		{
			if (rule == null || rule.outfitIndex < 0 || rule.outfitIndex >= enabled.Length) continue;
			bool active = enabled[rule.outfitIndex];
			if (rule.visibleWhenOff != null)
				foreach (var target in rule.visibleWhenOff)
					if (target != null) target.SetActive(!active);
			if (active && rule.hiddenWhenOn != null)
				foreach (var target in rule.hiddenWhenOn)
					if (target != null) target.SetActive(false);
			if (rule.colliderGroups == null) continue;
			foreach (var group in rule.colliderGroups)
			{
				if (group == null) continue;
				if (!wardrobeColliders.TryGetValue(group, out var original))
				{
					original = new List<VRM10SpringBoneCollider>(group.Colliders);
					wardrobeColliders.Add(group, original);
				}
				if (group.Colliders.Count == (active ? original.Count : 0)) continue;
				group.Colliders.Clear();
				if (active) group.Colliders.AddRange(original);
				physicsChanged = true;
			}
		}
		if (physicsChanged)
		{
			var vrm = GetComponentInParent<Vrm10Instance>();
			if (vrm != null && vrm.isActiveAndEnabled) vrm.Runtime.SpringBone.ReconstructSpringBone();
		}
	}
}
