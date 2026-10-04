using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniVRM10;
using UnityEngine;
using VRM;

public class AvatarBigScreenTouchHandler : MonoBehaviour
{
	[Header("Spring Bone Touch Settings")]
	public float mouseColliderRadius = 0.04f;

	private AvatarBigScreenHandler bigScreenHandler;

	private Animator avatarAnimator;

	private Camera mainCamera;

	private GameObject mouseColliderObj;

	private VRMSpringBoneColliderGroup mouseSpringColliderGroupVRM0;

	private VRM10SpringBoneColliderGroup mouseSpringColliderGroupVRM1;

	private VRM10SpringBoneCollider mouseSpringColliderVRM1;

	private static readonly int HairStrokeHash = Animator.StringToHash("HairStroke");

	private bool hasHairStrokeParam;

	private void Awake()
	{
		bigScreenHandler = GetComponent<AvatarBigScreenHandler>();
		avatarAnimator = GetComponent<Animator>();
		mainCamera = Camera.main;
		hasHairStrokeParam = avatarAnimator != null && AnimatorHasBool(avatarAnimator, "HairStroke");
	}

	private void Update()
	{
		if (bigScreenHandler == null || avatarAnimator == null || mainCamera == null)
		{
			return;
		}
		if (IsBigScreenActive())
		{
			if (Input.GetMouseButton(0))
			{
				HandleSpringBoneTouch();
				if (hasHairStrokeParam)
				{
					avatarAnimator.SetBool(HairStrokeHash, value: true);
				}
			}
			else
			{
				CleanupMouseCollider();
				if (hasHairStrokeParam)
				{
					avatarAnimator.SetBool(HairStrokeHash, value: false);
				}
			}
		}
		else
		{
			CleanupMouseCollider();
			if (hasHairStrokeParam)
			{
				avatarAnimator.SetBool(HairStrokeHash, value: false);
			}
		}
	}

	private bool AnimatorHasBool(Animator anim, string name)
	{
		AnimatorControllerParameter[] parameters = anim.parameters;
		foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
		{
			if (animatorControllerParameter.type == AnimatorControllerParameterType.Bool && animatorControllerParameter.name == name)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDisable()
	{
		if (avatarAnimator != null && hasHairStrokeParam)
		{
			avatarAnimator.SetBool(HairStrokeHash, value: false);
		}
		CleanupMouseCollider();
	}

	private bool IsBigScreenActive()
	{
		FieldInfo field = bigScreenHandler.GetType().GetField("isBigScreenActive", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field != null)
		{
			return (bool)field.GetValue(bigScreenHandler);
		}
		return false;
	}

	private void HandleSpringBoneTouch()
	{
		if (mouseColliderObj == null)
		{
			mouseColliderObj = new GameObject("MouseSpringBoneCollider");
			mouseColliderObj.hideFlags = HideFlags.HideAndDontSave;
			VRMSpringBone[] componentsInChildren = avatarAnimator.GetComponentsInChildren<VRMSpringBone>();
			if (componentsInChildren != null && componentsInChildren.Length != 0)
			{
				mouseSpringColliderGroupVRM0 = mouseColliderObj.AddComponent<VRMSpringBoneColliderGroup>();
				VRMSpringBoneColliderGroup.SphereCollider sphereCollider = new VRMSpringBoneColliderGroup.SphereCollider
				{
					Offset = Vector3.zero,
					Radius = mouseColliderRadius
				};
				mouseSpringColliderGroupVRM0.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[1] { sphereCollider };
				VRMSpringBone[] array = componentsInChildren;
				foreach (VRMSpringBone vRMSpringBone in array)
				{
					List<VRMSpringBoneColliderGroup> list = vRMSpringBone.ColliderGroups?.ToList() ?? new List<VRMSpringBoneColliderGroup>();
					if (!list.Contains(mouseSpringColliderGroupVRM0))
					{
						list.Add(mouseSpringColliderGroupVRM0);
						vRMSpringBone.ColliderGroups = list.ToArray();
					}
				}
			}
			VRM10SpringBoneJoint[] componentsInChildren2 = avatarAnimator.GetComponentsInChildren<VRM10SpringBoneJoint>();
			if (componentsInChildren2 != null && componentsInChildren2.Length != 0)
			{
				mouseSpringColliderGroupVRM1 = mouseColliderObj.AddComponent<VRM10SpringBoneColliderGroup>();
				mouseSpringColliderGroupVRM1.Name = "MouseColliderGroup";
				mouseSpringColliderGroupVRM1.Colliders = new List<VRM10SpringBoneCollider>();
				mouseSpringColliderVRM1 = mouseColliderObj.AddComponent<VRM10SpringBoneCollider>();
				mouseSpringColliderVRM1.ColliderType = VRM10SpringBoneColliderTypes.Sphere;
				mouseSpringColliderVRM1.Offset = Vector3.zero;
				mouseSpringColliderVRM1.Radius = mouseColliderRadius;
				mouseSpringColliderGroupVRM1.Colliders.Add(mouseSpringColliderVRM1);
				Vrm10Instance componentInParent = avatarAnimator.GetComponentInParent<Vrm10Instance>();
				if (componentInParent != null && componentInParent.SpringBone != null && !componentInParent.SpringBone.ColliderGroups.Contains(mouseSpringColliderGroupVRM1))
				{
					componentInParent.SpringBone.ColliderGroups.Add(mouseSpringColliderGroupVRM1);
				}
			}
		}
		Vector3 mousePosition = Input.mousePosition;
		float z = 1f;
		if (bigScreenHandler.attachBone != HumanBodyBones.LastBone)
		{
			Transform boneTransform = avatarAnimator.GetBoneTransform(bigScreenHandler.attachBone);
			if ((bool)boneTransform)
			{
				z = Mathf.Max(0.4f, mainCamera.WorldToScreenPoint(boneTransform.position).z);
			}
		}
		mousePosition.z = z;
		Vector3 position = mainCamera.ScreenToWorldPoint(mousePosition);
		mouseColliderObj.transform.position = position;
	}

	private void CleanupMouseCollider()
	{
		if (!(mouseColliderObj != null))
		{
			return;
		}
		if (mouseSpringColliderGroupVRM0 != null && avatarAnimator != null)
		{
			VRMSpringBone[] componentsInChildren = avatarAnimator.GetComponentsInChildren<VRMSpringBone>();
			foreach (VRMSpringBone vRMSpringBone in componentsInChildren)
			{
				List<VRMSpringBoneColliderGroup> list = vRMSpringBone.ColliderGroups?.ToList() ?? new List<VRMSpringBoneColliderGroup>();
				if (list.Contains(mouseSpringColliderGroupVRM0))
				{
					list.Remove(mouseSpringColliderGroupVRM0);
					vRMSpringBone.ColliderGroups = list.ToArray();
				}
			}
		}
		if (mouseSpringColliderGroupVRM1 != null && avatarAnimator != null)
		{
			Vrm10Instance componentInParent = avatarAnimator.GetComponentInParent<Vrm10Instance>();
			if (componentInParent != null && componentInParent.SpringBone != null && componentInParent.SpringBone.ColliderGroups.Contains(mouseSpringColliderGroupVRM1))
			{
				componentInParent.SpringBone.ColliderGroups.Remove(mouseSpringColliderGroupVRM1);
			}
		}
		Object.Destroy(mouseColliderObj);
		mouseColliderObj = null;
		mouseSpringColliderGroupVRM0 = null;
		mouseSpringColliderGroupVRM1 = null;
		mouseSpringColliderVRM1 = null;
	}
}
