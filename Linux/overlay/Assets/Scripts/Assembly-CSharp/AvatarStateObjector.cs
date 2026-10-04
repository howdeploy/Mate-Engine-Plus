using System;
using System.Collections.Generic;
using UnityEngine;

public class AvatarStateObjector : MonoBehaviour
{
	[Serializable]
	public class ObjectorRule
	{
		public string stateName;

		public GameObject targetObject;

		[Range(0f, 1f)]
		public float spawnAnimationSpeed = 0.1f;

		[NonSerialized]
		public Vector3 originalScale;

		[NonSerialized]
		public float currentLerp;

		[NonSerialized]
		public bool wasActive;

		[NonSerialized]
		public bool initialized;
	}

	[Header("Avatar State Objector Rules")]
	public List<ObjectorRule> objectorRules = new List<ObjectorRule>();

	private Animator cachedAnimator;

	private AvatarAnimatorController cachedAvatar;

	private GameObject currentModel;

	private Transform modelRoot;

	private void Start()
	{
		GameObject gameObject = GameObject.Find("Model");
		modelRoot = ((gameObject != null) ? gameObject.transform : null);
		RebindToActiveModel();
	}

	private void Update()
	{
		if (modelRoot == null)
		{
			GameObject gameObject = GameObject.Find("Model");
			modelRoot = ((gameObject != null) ? gameObject.transform : null);
			return;
		}
		GameObject gameObject2 = null;
		for (int i = 0; i < modelRoot.childCount; i++)
		{
			Transform child = modelRoot.GetChild(i);
			if (child.gameObject.activeInHierarchy)
			{
				gameObject2 = child.gameObject;
				break;
			}
		}
		if (gameObject2 != currentModel)
		{
			currentModel = gameObject2;
			RebindToActiveModel();
		}
		if (cachedAnimator == null)
		{
			return;
		}
		for (int j = 0; j < objectorRules.Count; j++)
		{
			ObjectorRule objectorRule = objectorRules[j];
			if (objectorRule.targetObject == null)
			{
				continue;
			}
			if (!objectorRule.initialized)
			{
				if (objectorRule.originalScale == Vector3.zero)
				{
					objectorRule.originalScale = objectorRule.targetObject.transform.localScale;
				}
				objectorRule.targetObject.transform.localScale = Vector3.zero;
				objectorRule.targetObject.SetActive(value: false);
				objectorRule.wasActive = false;
				objectorRule.currentLerp = 0f;
				objectorRule.initialized = true;
			}
			bool flag = false;
			if (cachedAnimator.HasParameter(objectorRule.stateName, AnimatorControllerParameterType.Bool))
			{
				flag = cachedAnimator.GetBool(objectorRule.stateName);
			}
			else
			{
				AnimatorStateInfo currentAnimatorStateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
				if (!cachedAnimator.IsInTransition(0) && currentAnimatorStateInfo.IsName(objectorRule.stateName))
				{
					flag = true;
				}
			}
			float target = (flag ? 1f : 0f);
			float num = Mathf.Lerp(10f, 0.25f, objectorRule.spawnAnimationSpeed);
			objectorRule.currentLerp = Mathf.MoveTowards(objectorRule.currentLerp, target, Time.unscaledDeltaTime * num);
			if (!objectorRule.wasActive && objectorRule.currentLerp > 0f)
			{
				objectorRule.targetObject.SetActive(value: true);
				objectorRule.wasActive = true;
			}
			objectorRule.targetObject.transform.localScale = Vector3.Lerp(Vector3.zero, objectorRule.originalScale, objectorRule.currentLerp);
			if (objectorRule.wasActive && objectorRule.currentLerp <= 0f)
			{
				objectorRule.targetObject.SetActive(value: false);
				objectorRule.wasActive = false;
			}
		}
	}

	private void RebindToActiveModel()
	{
		cachedAnimator = null;
		cachedAvatar = null;
		if (!(currentModel == null))
		{
			cachedAvatar = currentModel.GetComponent<AvatarAnimatorController>();
			if (cachedAvatar != null)
			{
				cachedAnimator = cachedAvatar.GetComponent<Animator>();
			}
			for (int i = 0; i < objectorRules.Count; i++)
			{
				objectorRules[i].initialized = false;
			}
		}
	}
}
