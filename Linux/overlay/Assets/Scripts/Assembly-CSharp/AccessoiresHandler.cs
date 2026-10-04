using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccessoiresHandler : MonoBehaviour
{
	[Serializable]
	public class AccessoryRule
	{
		public string ruleName;

		public bool isEnabled;

		public HumanBodyBones targetBone;

		public GameObject linkedObject;

		[Range(0f, 1f)]
		public float smoothness;

		public bool steamExclusive;

		public Vector3 positionOffset = Vector3.zero;
	}

	private class BoneTracking
	{
		public Transform bone;

		public GameObject obj;

		public Vector3 currentPosition;

		public Quaternion currentRotation;

		public bool lastActiveState;
	}

	public int steamAppId;

	public int ttlDays = 14;

	public float retrySeconds = 5f;

	public float maxWaitSeconds = 180f;

	public Animator animator;

	public List<AccessoryRule> rules = new List<AccessoryRule>();

	public bool featureEnabled = true;

	private Dictionary<AccessoryRule, BoneTracking> trackingMap = new Dictionary<AccessoryRule, BoneTracking>();

	public static readonly List<AccessoiresHandler> ActiveHandlers = new List<AccessoiresHandler>();

	private void Start()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
		if (!SteamDRM.Initialized)
		{
			SteamDRM.Initialize(steamAppId, ttlDays);
		}
		foreach (AccessoryRule rule in rules)
		{
			if (!(rule.linkedObject == null))
			{
				Transform boneTransform = animator.GetBoneTransform(rule.targetBone);
				if (!(boneTransform == null))
				{
					BoneTracking value = new BoneTracking
					{
						bone = boneTransform,
						obj = rule.linkedObject,
						currentPosition = boneTransform.position,
						currentRotation = boneTransform.rotation,
						lastActiveState = false
					};
					trackingMap[rule] = value;
				}
			}
		}
		StartCoroutine(ReinitLoop());
	}

	private void Update()
	{
		foreach (KeyValuePair<AccessoryRule, BoneTracking> item in trackingMap)
		{
			AccessoryRule key = item.Key;
			BoneTracking value = item.Value;
			bool flag = featureEnabled && key.isEnabled && (!key.steamExclusive || SteamDRM.IsEntitled);
			if (value.obj != null && value.lastActiveState != flag)
			{
				value.obj.SetActive(flag);
				value.lastActiveState = flag;
			}
			if (flag && value.obj != null)
			{
				Vector3 b = value.bone.TransformPoint(key.positionOffset);
				Quaternion rotation = value.bone.rotation;
				value.currentPosition = Vector3.Lerp(value.currentPosition, b, 1f - key.smoothness);
				value.currentRotation = Quaternion.Slerp(value.currentRotation, rotation, 1f - key.smoothness);
				value.obj.transform.position = value.currentPosition;
				value.obj.transform.rotation = value.currentRotation;
			}
		}
	}

	private void OnEnable()
	{
		if (!ActiveHandlers.Contains(this))
		{
			ActiveHandlers.Add(this);
		}
	}

	private void OnDisable()
	{
		ActiveHandlers.Remove(this);
	}

	private IEnumerator ReinitLoop()
	{
		float t = 0f;
		while (!SteamDRM.IsEntitled && t < maxWaitSeconds)
		{
			SteamDRM.TryInitLive(steamAppId, ttlDays);
			yield return new WaitForSeconds(retrySeconds);
			t += retrySeconds;
		}
	}
}
