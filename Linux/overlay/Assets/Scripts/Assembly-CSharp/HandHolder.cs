using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HandHolder : MonoBehaviour
{
	[Header("World-Space Interaction")]
	public float screenInteractionRadius = 0.2f;

	public Color screenInteractionRadiusColor = new Color(0.2f, 0.7f, 1f, 0.2f);

	public float preZoneMargin = 0.1f;

	public Color preZoneMarginColor = new Color(0.1f, 0.5f, 1f, 0.15f);

	[Header("Big Screen Scaling")]
	[Range(0f, 100f)]
	public float bigScreenRadiusScale = 100f;

	public float followSpeed = 10f;

	[Header("Hand Tracking Settings")]
	public float maxIKWeight = 1f;

	[Header("Hand Tracking Settings")]
	public float blendInTime = 1f;

	[Header("Hand Tracking Settings")]
	public float blendOutTime = 1f;

	public float maxHandDistance = 0.8f;

	public float minForwardOffset = 0.2f;

	public float verticalOffset = 0.05f;

	public float elbowHintDistance = 0.25f;

	public float elbowHintBackOffset = 0.1f;

	public float elbowHintHeightOffset = -0.05f;

	public string[] allowedStates = new string[2] { "Idle", "HoverReaction" };

	public Animator avatarAnimator;

	public bool showDebugGizmos = true;

	public bool enableHandHolding = true;

	private Camera mainCam;

	private Transform leftHand;

	private Transform rightHand;

	private Transform chest;

	private Transform leftShoulder;

	private Transform rightShoulder;

	private Vector3 leftTargetPos;

	private Vector3 rightTargetPos;

	private float leftIKWeight;

	private float rightIKWeight;

	private bool leftIsActive;

	private bool rightIsActive;

	private void Start()
	{
		mainCam = Camera.main;
		CacheTransforms();
	}

	private void CacheTransforms()
	{
		leftHand = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
		rightHand = avatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);
		chest = avatarAnimator.GetBoneTransform(HumanBodyBones.Chest) ?? avatarAnimator.GetBoneTransform(HumanBodyBones.Spine);
		leftShoulder = avatarAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
		rightShoulder = avatarAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
	}

	public void SetAnimator(Animator newAnimator)
	{
		avatarAnimator = newAnimator;
		CacheTransforms();
	}

	private void Update()
	{
		if (!enableHandHolding || !IsValid())
		{
			leftIKWeight = Mathf.MoveTowards(leftIKWeight, 0f, Time.deltaTime / blendOutTime);
			rightIKWeight = Mathf.MoveTowards(rightIKWeight, 0f, Time.deltaTime / blendOutTime);
			return;
		}
		if (MenuActions.IsHandTrackingBlocked())
		{
			leftIKWeight = Mathf.MoveTowards(leftIKWeight, 0f, Time.deltaTime / blendOutTime);
			rightIKWeight = Mathf.MoveTowards(rightIKWeight, 0f, Time.deltaTime / blendOutTime);
			return;
		}
		if (!IsInAllowedState())
		{
			leftIKWeight = Mathf.MoveTowards(leftIKWeight, 0f, Time.deltaTime / blendOutTime);
			rightIKWeight = Mathf.MoveTowards(rightIKWeight, 0f, Time.deltaTime / blendOutTime);
			return;
		}
		float num = ComputeWorldWeight(leftHand);
		float num2 = ComputeWorldWeight(rightHand);
		if (num > num2)
		{
			leftIsActive = num > 0f;
			rightIsActive = false;
			num2 = 0f;
		}
		else
		{
			rightIsActive = num2 > 0f;
			leftIsActive = false;
			num = 0f;
		}
		leftIKWeight = Mathf.MoveTowards(leftIKWeight, num, Time.deltaTime / ((num > leftIKWeight) ? blendInTime : blendOutTime));
		rightIKWeight = Mathf.MoveTowards(rightIKWeight, num2, Time.deltaTime / ((num2 > rightIKWeight) ? blendInTime : blendOutTime));
		Vector3 projectedMouseTarget = GetProjectedMouseTarget();
		if (leftIsActive)
		{
			leftTargetPos = Vector3.Lerp(leftTargetPos, projectedMouseTarget, Time.deltaTime * followSpeed);
		}
		if (rightIsActive)
		{
			rightTargetPos = Vector3.Lerp(rightTargetPos, projectedMouseTarget, Time.deltaTime * followSpeed);
		}
	}

	private float ComputeWorldWeight(Transform hand)
	{
		if (!hand)
		{
			return 0f;
		}
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.z = mainCam.WorldToScreenPoint(hand.position).z;
		Vector3 b = mainCam.ScreenToWorldPoint(mousePosition);
		float magnitude = hand.lossyScale.magnitude;
		float num = 1f;
		if (avatarAnimator != null && avatarAnimator.GetBool("isBigScreen"))
		{
			num = bigScreenRadiusScale * 0.01f;
		}
		float num2 = screenInteractionRadius * magnitude * num;
		float num3 = (screenInteractionRadius + preZoneMargin) * magnitude * num;
		float num4 = Vector3.Distance(hand.position, b);
		if (num4 <= num2)
		{
			return maxIKWeight;
		}
		if (num4 >= num3)
		{
			return 0f;
		}
		return Mathf.Lerp(maxIKWeight, 0f, (num4 - num2) / (num3 - num2));
	}

	private bool IsInAllowedState()
	{
		AnimatorStateInfo currentAnimatorStateInfo = avatarAnimator.GetCurrentAnimatorStateInfo(0);
		for (int i = 0; i < allowedStates.Length; i++)
		{
			if (currentAnimatorStateInfo.IsName(allowedStates[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (!(avatarAnimator == null))
		{
			if (!IsValid())
			{
				avatarAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
				avatarAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
				avatarAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
				avatarAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
				avatarAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
				avatarAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
			}
			else
			{
				Quaternion rotation = Quaternion.LookRotation(avatarAnimator.transform.forward, avatarAnimator.transform.up);
				ApplyIK(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftIKWeight, leftTargetPos, leftShoulder, isLeft: true, rotation);
				ApplyIK(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightIKWeight, rightTargetPos, rightShoulder, isLeft: false, rotation);
			}
		}
	}

	private void ApplyIK(AvatarIKGoal hand, AvatarIKHint elbow, float weight, Vector3 targetPos, Transform shoulder, bool isLeft, Quaternion rotation)
	{
		avatarAnimator.SetIKPositionWeight(hand, weight);
		avatarAnimator.SetIKRotationWeight(hand, weight);
		avatarAnimator.SetIKHintPositionWeight(elbow, weight);
		if (!(weight <= 0f))
		{
			avatarAnimator.SetIKPosition(hand, targetPos);
			avatarAnimator.SetIKRotation(hand, rotation);
			avatarAnimator.SetIKHintPosition(elbow, GetElbowHint(shoulder, targetPos, isLeft));
		}
	}

	private Vector3 GetElbowHint(Transform shoulder, Vector3 target, bool isLeft)
	{
		Vector3 vector = Vector3.Cross((target - shoulder.position).normalized, avatarAnimator.transform.up).normalized;
		if (!isLeft)
		{
			vector = -vector;
		}
		return shoulder.position + vector * elbowHintDistance - avatarAnimator.transform.forward * elbowHintBackOffset + avatarAnimator.transform.up * elbowHintHeightOffset;
	}

	private Vector3 GetProjectedMouseTarget()
	{
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.z = mainCam.WorldToScreenPoint(chest.position).z;
		Vector3 position = mainCam.ScreenToWorldPoint(mousePosition);
		Vector3 position2 = avatarAnimator.transform.InverseTransformPoint(position);
		position2.z = Mathf.Clamp(position2.z, minForwardOffset, maxHandDistance);
		position2.y += verticalOffset;
		return avatarAnimator.transform.TransformPoint(position2);
	}

	private bool IsValid()
	{
		if ((bool)avatarAnimator && (bool)leftHand && (bool)rightHand && (bool)chest && (bool)leftShoulder)
		{
			return rightShoulder;
		}
		return false;
	}
}
