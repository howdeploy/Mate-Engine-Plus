using System;
using System.Collections.Generic;
using UnityEngine;

public class AvatarParticleHandler : MonoBehaviour
{
	[Serializable]
	public class ParticleRule
	{
		public string themeTag = "Dance Trail Blue";

		public List<string> stateOrParameterNames = new List<string>();

		public bool useParameter;

		public HumanBodyBones targetBone;

		public List<GameObject> linkedObjects = new List<GameObject>();
	}

	private struct RuleCache
	{
		public string themeTag;

		public Transform bone;

		public GameObject[] objects;

		public int[] paramIndices;

		public bool useParameter;

		public List<string> stateNameList;
	}

	public Animator animator;

	public List<ParticleRule> rules = new List<ParticleRule>();

	public bool featureEnabled = true;

	[Header("Theme")]
	public string selectedTheme = "Dance Trail Blue";

	private RuleCache[] cache = Array.Empty<RuleCache>();

	private AnimatorControllerParameter[] animParams;

	private void Start()
	{
		if ((object)animator == null)
		{
			animator = GetComponent<Animator>();
		}
		if (!animator)
		{
			return;
		}
		animParams = animator.parameters;
		List<RuleCache> list = new List<RuleCache>(rules.Count);
		foreach (ParticleRule rule in rules)
		{
			Transform boneTransform = animator.GetBoneTransform(rule.targetBone);
			if (!boneTransform)
			{
				continue;
			}
			List<GameObject> list2 = rule.linkedObjects.FindAll((GameObject o) => o != null);
			foreach (GameObject item in list2)
			{
				item.SetActive(value: false);
			}
			List<int> list3 = new List<int>();
			if (rule.useParameter)
			{
				foreach (string stateOrParameterName in rule.stateOrParameterNames)
				{
					for (int num = 0; num < animParams.Length; num++)
					{
						if (animParams[num].type == AnimatorControllerParameterType.Bool && animParams[num].name == stateOrParameterName)
						{
							list3.Add(num);
							break;
						}
					}
				}
			}
			list.Add(new RuleCache
			{
				themeTag = rule.themeTag,
				bone = boneTransform,
				objects = list2.ToArray(),
				paramIndices = list3.ToArray(),
				useParameter = rule.useParameter,
				stateNameList = new List<string>(rule.stateOrParameterNames)
			});
		}
		cache = list.ToArray();
	}

	private void Update()
	{
		if (!featureEnabled || !animator)
		{
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		for (int i = 0; i < cache.Length; i++)
		{
			RuleCache ruleCache = cache[i];
			bool num = ruleCache.themeTag == selectedTheme;
			bool flag = false;
			if (num)
			{
				if (ruleCache.useParameter && ruleCache.paramIndices != null && ruleCache.paramIndices.Length != 0)
				{
					int[] paramIndices = ruleCache.paramIndices;
					foreach (int num2 in paramIndices)
					{
						if (num2 >= 0 && animator.GetBool(animParams[num2].name))
						{
							flag = true;
							break;
						}
					}
				}
				else
				{
					foreach (string stateName in ruleCache.stateNameList)
					{
						if (currentAnimatorStateInfo.IsName(stateName))
						{
							flag = true;
							break;
						}
					}
				}
			}
			Transform bone = ruleCache.bone;
			GameObject[] objects = ruleCache.objects;
			foreach (GameObject gameObject in objects)
			{
				if ((bool)gameObject)
				{
					gameObject.SetActive(flag);
					if (flag)
					{
						gameObject.transform.SetPositionAndRotation(bone.position, bone.rotation);
					}
				}
			}
		}
	}

	public void SetTheme(string tag)
	{
		selectedTheme = tag;
	}
}
