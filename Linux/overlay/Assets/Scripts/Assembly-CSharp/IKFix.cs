using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Animator))]
public class IKFix : MonoBehaviour
{
	[Serializable]
	public class IKFixState
	{
		public string stateName;

		public bool fixFeetIK;
	}

	[Header("IK Master Toggle")]
	public bool enableIK = true;

	[Header("IK Fix States")]
	public List<IKFixState> ikFixStates = new List<IKFixState>();

	[Header("Blend Speed")]
	public float blendSpeed = 5f;

	private Animator animator;

	private float currentIKWeight;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (animator == null || !animator.isActiveAndEnabled)
		{
			return;
		}
		float target = 0f;
		if (enableIK)
		{
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
			foreach (IKFixState ikFixState in ikFixStates)
			{
				if (ikFixState.fixFeetIK && currentAnimatorStateInfo.IsName(ikFixState.stateName))
				{
					target = 1f;
					break;
				}
			}
		}
		currentIKWeight = Mathf.MoveTowards(currentIKWeight, target, Time.deltaTime * blendSpeed);
		if (currentIKWeight > 0f)
		{
			ApplyFootIK(AvatarIKGoal.LeftFoot, currentIKWeight);
			ApplyFootIK(AvatarIKGoal.RightFoot, currentIKWeight);
			return;
		}
		animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
		animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
		animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
		animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
	}

	private void ApplyFootIK(AvatarIKGoal foot, float weight)
	{
		animator.SetIKPositionWeight(foot, weight);
		animator.SetIKRotationWeight(foot, weight);
		animator.SetIKPosition(foot, animator.GetIKPosition(foot));
		animator.SetIKRotation(foot, animator.GetIKRotation(foot));
	}
}
