using System.Collections.Generic;
using UniVRM10;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AvatarMouseTracking : MonoBehaviour
{
	[Header("Mouse Tracking Settings")]
	public bool enableMouseTracking = true;

	public List<TrackingPermission> trackingPermissions = new List<TrackingPermission>();

	[Range(0f, 90f)]
	public float headYawLimit = 45f;

	[Range(0f, 90f)]
	public float headPitchLimit = 30f;

	[Range(1f, 20f)]
	public float headSmoothness = 10f;

	[Range(-90f, 90f)]
	public float spineMinRotation = -15f;

	[Range(-90f, 90f)]
	public float spineMaxRotation = 15f;

	[Range(1f, 50f)]
	public float spineSmoothness = 25f;

	[Range(1f, 10f)]
	public float spineFadeSpeed = 5f;

	[Range(0f, 90f)]
	public float eyeYawLimit = 12f;

	[Range(0f, 90f)]
	public float eyePitchLimit = 12f;

	[Range(1f, 20f)]
	public float eyeSmoothness = 10f;

	[Range(0f, 1f)]
	public float headBlend = 1f;

	[Range(0f, 1f)]
	public float spineBlend = 1f;

	[Range(0f, 1f)]
	public float eyeBlend = 1f;

	private Animator animator;

	private Camera mainCam;

	private Transform headBone;

	private Transform spineBone;

	private Transform chestBone;

	private Transform upperChestBone;

	private Transform leftEyeBone;

	private Transform rightEyeBone;

	private Transform headDriver;

	private Transform spineDriver;

	private Transform leftEyeDriver;

	private Transform rightEyeDriver;

	private Transform eyeCenter;

	private Transform vrmLookAtTarget;

	private Quaternion headInitRot;

	private Quaternion spineInitRot;

	private float spineTrackingWeight;

	private Vrm10Instance vrm10;

	private int currStateHash;

	private int nextStateHash;

	private void Start()
	{
		animator = GetComponent<Animator>();
		mainCam = Camera.main;
		if (!animator || !animator.isHuman)
		{
			enableMouseTracking = false;
			Debug.LogError("Animator not found or not humanoid!");
			return;
		}
		vrm10 = GetComponentInChildren<Vrm10Instance>();
		InitHead();
		InitSpine();
		InitEye();
	}

	private void InitHead()
	{
		headBone = animator.GetBoneTransform(HumanBodyBones.Head);
		if ((bool)headBone)
		{
			headDriver = new GameObject("HeadDriver").transform;
			headDriver.SetParent(headBone.parent, worldPositionStays: false);
			headDriver.localPosition = headBone.localPosition;
			headDriver.localRotation = headBone.localRotation;
			headInitRot = headBone.localRotation;
		}
	}

	private void InitSpine()
	{
		spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
		chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
		upperChestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
		if ((bool)spineBone)
		{
			spineDriver = new GameObject("SpineDriver").transform;
			spineDriver.SetParent(spineBone.parent, worldPositionStays: false);
			spineDriver.localPosition = spineBone.localPosition;
			spineDriver.localRotation = spineBone.localRotation;
			spineInitRot = spineBone.localRotation;
		}
	}

	private void InitEye()
	{
		leftEyeBone = animator.GetBoneTransform(HumanBodyBones.LeftEye);
		rightEyeBone = animator.GetBoneTransform(HumanBodyBones.RightEye);
		if ((bool)vrm10)
		{
			vrmLookAtTarget = new GameObject("VRMLookAtTarget").transform;
			vrmLookAtTarget.SetParent(base.transform, worldPositionStays: false);
			vrm10.LookAtTarget = vrmLookAtTarget;
			vrm10.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;
		}
		if (!leftEyeBone || !rightEyeBone)
		{
			Transform[] componentsInChildren = animator.GetComponentsInChildren<Transform>();
			foreach (Transform transform in componentsInChildren)
			{
				string text = transform.name.ToLower();
				if (!leftEyeBone && (text.Contains("lefteye") || text.Contains("eye.l")))
				{
					leftEyeBone = transform;
				}
				else if (!rightEyeBone && (text.Contains("righteye") || text.Contains("eye.r")))
				{
					rightEyeBone = transform;
				}
			}
		}
		if ((bool)leftEyeBone && (bool)rightEyeBone)
		{
			eyeCenter = new GameObject("EyeCenter").transform;
			eyeCenter.SetParent(leftEyeBone.parent, worldPositionStays: false);
			eyeCenter.position = (leftEyeBone.position + rightEyeBone.position) * 0.5f;
			leftEyeDriver = new GameObject("LeftEyeDriver").transform;
			leftEyeDriver.SetParent(leftEyeBone.parent, worldPositionStays: false);
			leftEyeDriver.localPosition = leftEyeBone.localPosition;
			leftEyeDriver.localRotation = leftEyeBone.localRotation;
			rightEyeDriver = new GameObject("RightEyeDriver").transform;
			rightEyeDriver.SetParent(rightEyeBone.parent, worldPositionStays: false);
			rightEyeDriver.localPosition = rightEyeBone.localPosition;
			rightEyeDriver.localRotation = rightEyeBone.localRotation;
		}
	}

	private void LateUpdate()
	{
		if (enableMouseTracking && (bool)mainCam && (bool)animator)
		{
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			AnimatorStateInfo nextAnimatorStateInfo = animator.GetNextAnimatorStateInfo(0);
			if (animator.IsInTransition(0))
			{
				nextStateHash = nextAnimatorStateInfo.shortNameHash;
			}
			else
			{
				currStateHash = currentAnimatorStateInfo.shortNameHash;
				nextStateHash = 0;
			}
			if (IsAllowed("Head"))
			{
				DoHead();
			}
			DoSpine();
			if (IsAllowed("Eye"))
			{
				DoEye();
			}
		}
	}

	private bool IsAllowed(string f)
	{
		bool? flag = null;
		bool? flag2 = null;
		foreach (TrackingPermission trackingPermission in trackingPermissions)
		{
			if (trackingPermission.isParameter && animator.GetBool(trackingPermission.stateOrParameterName))
			{
				return Get(trackingPermission, f);
			}
			int num = Animator.StringToHash(trackingPermission.stateOrParameterName);
			if (currStateHash == num)
			{
				flag = Get(trackingPermission, f);
			}
			if (animator.IsInTransition(0) && nextStateHash == num)
			{
				flag2 = Get(trackingPermission, f);
			}
		}
		if (animator.IsInTransition(0) && flag2.HasValue)
		{
			return flag2.Value;
		}
		return flag == true;
	}

	private bool Get(TrackingPermission e, string f)
	{
		if (!(f == "Head"))
		{
			if (!(f == "Spine"))
			{
				return e.allowEye;
			}
			return e.allowSpine;
		}
		return e.allowHead;
	}

	private void DoHead()
	{
		if ((bool)headBone && (bool)headDriver)
		{
			Vector3 mousePosition = Input.mousePosition;
			Vector3 normalized = (mainCam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCam.nearClipPlane)) - headDriver.position).normalized;
			Vector3 vector = headDriver.parent.InverseTransformDirection(normalized);
			float y = Mathf.Clamp(Mathf.Atan2(vector.x, vector.z) * 57.29578f, 0f - headYawLimit, headYawLimit);
			float num = Mathf.Clamp(Mathf.Asin(vector.y) * 57.29578f, 0f - headPitchLimit, headPitchLimit);
			headDriver.localRotation = Quaternion.Slerp(headDriver.localRotation, Quaternion.Euler(0f - num, y, 0f), Time.deltaTime * headSmoothness);
			Quaternion localRotation = headBone.localRotation;
			Quaternion quaternion = headDriver.localRotation * Quaternion.Inverse(headInitRot);
			headBone.localRotation = Quaternion.Slerp(localRotation, quaternion * localRotation, headBlend);
		}
	}

	private void DoSpine()
	{
		if ((bool)spineBone && (bool)spineDriver)
		{
			float target = (IsAllowed("Spine") ? 1f : 0f);
			spineTrackingWeight = Mathf.MoveTowards(spineTrackingWeight, target, Time.deltaTime * spineFadeSpeed);
			float t = Mathf.Clamp01(Input.mousePosition.x / (float)Screen.width);
			float num = Mathf.Lerp(spineMinRotation, spineMaxRotation, t);
			spineDriver.localRotation = Quaternion.Slerp(spineDriver.localRotation, Quaternion.Euler(0f, 0f - num, 0f), Time.deltaTime * spineSmoothness);
			Quaternion localRotation = spineBone.localRotation;
			Quaternion b = spineDriver.localRotation * Quaternion.Inverse(spineInitRot);
			float num2 = spineTrackingWeight * spineBlend;
			Quaternion quaternion = Quaternion.Slerp(Quaternion.identity, b, num2);
			spineBone.localRotation = quaternion * localRotation;
			if ((bool)chestBone)
			{
				chestBone.localRotation = Quaternion.Slerp(Quaternion.identity, b, 0.8f * num2) * chestBone.localRotation;
			}
			if ((bool)upperChestBone)
			{
				upperChestBone.localRotation = Quaternion.Slerp(Quaternion.identity, b, 0.6f * num2) * upperChestBone.localRotation;
			}
		}
	}

	private void DoEye()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 vector = mainCam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCam.nearClipPlane));
		if ((bool)vrm10 && (bool)vrmLookAtTarget)
		{
			vrmLookAtTarget.position = vector;
			Transform transform = vrmLookAtTarget.parent ?? base.transform;
			(float Yaw, float Pitch) tuple = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).CalcYawPitch(vector);
			float item = tuple.Yaw;
			float item2 = tuple.Pitch;
			float y = Mathf.Clamp(0f - item, 0f - eyeYawLimit, eyeYawLimit);
			float num = Mathf.Clamp(item2, 0f - eyePitchLimit, eyePitchLimit);
			Vector3 forward = vrmLookAtTarget.forward;
			Vector3 b = Quaternion.Euler(0f - num, y, 0f) * Vector3.forward;
			Vector3 forward2 = Vector3.Slerp(forward, b, Time.deltaTime * eyeSmoothness);
			vrmLookAtTarget.rotation = Quaternion.LookRotation(forward2);
		}
		else if ((bool)leftEyeBone && (bool)rightEyeBone && (bool)eyeCenter)
		{
			eyeCenter.position = (leftEyeBone.position + rightEyeBone.position) * 0.5f;
			Vector3 normalized = (vector - eyeCenter.position).normalized;
			Vector3 vector2 = eyeCenter.parent.InverseTransformDirection(normalized);
			float y2 = Mathf.Clamp(Mathf.Atan2(vector2.x, vector2.z) * 57.29578f, 0f - eyeYawLimit, eyeYawLimit);
			Quaternion b2 = Quaternion.Euler(0f - Mathf.Clamp(Mathf.Asin(vector2.y) * 57.29578f, 0f - eyePitchLimit, eyePitchLimit), y2, 0f);
			leftEyeDriver.localRotation = Quaternion.Slerp(leftEyeDriver.localRotation, b2, Time.deltaTime * eyeSmoothness);
			rightEyeDriver.localRotation = Quaternion.Slerp(rightEyeDriver.localRotation, b2, Time.deltaTime * eyeSmoothness);
			leftEyeBone.localRotation = Quaternion.Slerp(leftEyeBone.localRotation, leftEyeDriver.localRotation, eyeBlend);
			rightEyeBone.localRotation = Quaternion.Slerp(rightEyeBone.localRotation, rightEyeDriver.localRotation, eyeBlend);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(headDriver?.gameObject);
		Object.Destroy(spineDriver?.gameObject);
		Object.Destroy(leftEyeDriver?.gameObject);
		Object.Destroy(rightEyeDriver?.gameObject);
		Object.Destroy(eyeCenter?.gameObject);
		Object.Destroy(vrmLookAtTarget?.gameObject);
	}
}
