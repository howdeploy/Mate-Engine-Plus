using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class AvatarSwayController : MonoBehaviour
{
	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	[Header("References")]
	public Animator animator;

	public string draggingParam = "isDragging";

	public string windowSitParam = "isWindowSit";

	[Header("Space")]
	public bool useLocalRotation = true;

	public Transform globalReference;

	[Header("Input")]
	public bool useWindowVelocity = true;

	public bool fallbackToMouse = true;

	public float mouseSensitivity = 0.6f;

	public bool invertHorizontal;

	public bool invertVertical;

	[Header("Sway Physics")]
	public float horizontalVelocityToLean = 0.25f;

	public float verticalVelocityToPitch = 0.15f;

	public float maxLeanZ = 25f;

	public float maxLeanX = 12f;

	public float springFrequency = 2.6f;

	public float dampingRatio = 0.35f;

	public float blendSpeed = 8f;

	[Header("Limb Additive")]
	[Range(0f, 1f)]
	public float armsAdditive;

	[Range(0f, 1f)]
	public float legsAdditive;

	public bool invertArms;

	public bool invertLegs;

	public float armsMaxZ = 18f;

	public float armsMaxX = 8f;

	public float legsMaxZ = 12f;

	public float legsMaxX = 6f;

	public float limbLag = 6f;

	[Header("State Whitelist")]
	public bool useAllowedStatesWhitelist;

	public string[] allowedStates = new string[1] { "Drag" };

	public int stateLayerIndex;

	[Header("Stability")]
	public bool neutralizeEveryUpdate = true;

	public bool disableWhileWindowSit = true;

	private Animator anim;

	private int draggingHash;

	private int windowSitHash;

	private Animator cachedForBones;

	private Transform hips;

	private Transform leftUpperArm;

	private Transform rightUpperArm;

	private Transform leftUpperLeg;

	private Transform rightUpperLeg;

	private float leanZ;

	private float leanZVel;

	private float leanX;

	private float leanXVel;

	private float effectWeight;

	private float limbZ;

	private float limbX;

	private Vector2 filteredDelta;

	private Vector2 prevMousePos;

	private Quaternion lastHipAddLocal = Quaternion.identity;

	private Quaternion lastArmLAddLocal = Quaternion.identity;

	private Quaternion lastArmRAddLocal = Quaternion.identity;

	private Quaternion lastLegLAddLocal = Quaternion.identity;

	private Quaternion lastLegRAddLocal = Quaternion.identity;

	private Quaternion lastHipAddWorld = Quaternion.identity;

	private Quaternion lastArmLAddWorld = Quaternion.identity;

	private Quaternion lastArmRAddWorld = Quaternion.identity;

	private Quaternion lastLegLAddWorld = Quaternion.identity;

	private Quaternion lastLegRAddWorld = Quaternion.identity;

	private IntPtr hwnd;

	private Vector2Int prevWinPos;

	private void Awake()
	{
		draggingHash = Animator.StringToHash(draggingParam);
		windowSitHash = Animator.StringToHash(windowSitParam);
		prevMousePos = Input.mousePosition;
		hwnd = WindowManager.Instance.UnityWindow;
		if (hwnd != IntPtr.Zero)
		{
			prevWinPos = GetWindowPosition(hwnd);
		}
	}

	private void OnDisable()
	{
		ClearPreviousAdditivesIfAny();
	}

	private void Update()
	{
		EnsureAnimatorAndBones();
		if ((bool)anim && (bool)hips)
		{
			if (neutralizeEveryUpdate)
			{
				ClearPreviousAdditivesIfAny();
			}
			bool flag = anim.GetBool(draggingHash);
			bool flag2 = anim.GetBool(windowSitHash);
			bool flag3 = IsInAllowedState();
			bool flag4 = flag && flag3 && !(disableWhileWindowSit && flag2);
			float deltaTime = Time.deltaTime;
			Vector2 vector = Vector2.zero;
			if (useWindowVelocity && hwnd != IntPtr.Zero && flag4)
			{
				Vector2Int windowPosition = GetWindowPosition(hwnd);
				Vector2Int vector2Int = windowPosition - prevWinPos;
				prevWinPos = windowPosition;
				vector = new Vector2(vector2Int.x, vector2Int.y);
			}
			if (vector == Vector2.zero && fallbackToMouse && flag)
			{
				Vector2 vector2 = Input.mousePosition;
				Vector2 vector3 = (vector2 - prevMousePos) * mouseSensitivity;
				prevMousePos = vector2;
				vector = vector3;
			}
			else
			{
				prevMousePos = Input.mousePosition;
			}
			filteredDelta = Vector2.Lerp(filteredDelta, vector, 1f - Mathf.Exp(-12f * deltaTime));
			float num = (invertHorizontal ? 1f : (-1f));
			float num2 = (invertVertical ? (-1f) : 1f);
			float num3 = Mathf.Clamp(num * filteredDelta.x * horizontalVelocityToLean, 0f - maxLeanZ, maxLeanZ);
			float num4 = Mathf.Clamp(num2 * filteredDelta.y * verticalVelocityToPitch, 0f - maxLeanX, maxLeanX);
			Spring(ref leanZ, ref leanZVel, flag4 ? num3 : 0f, springFrequency, dampingRatio, deltaTime);
			Spring(ref leanX, ref leanXVel, flag4 ? num4 : 0f, springFrequency, dampingRatio, deltaTime);
			limbZ = Mathf.Lerp(limbZ, 0f - leanZ, 1f - Mathf.Exp((0f - limbLag) * deltaTime));
			limbX = Mathf.Lerp(limbX, 0f - leanX, 1f - Mathf.Exp((0f - limbLag) * deltaTime));
			float num5 = (flag4 ? blendSpeed : (blendSpeed * 2f));
			effectWeight = Mathf.MoveTowards(effectWeight, flag4 ? 1f : 0f, num5 * deltaTime);
		}
	}

	private void LateUpdate()
	{
		if (!anim || !hips)
		{
			return;
		}
		if (effectWeight <= 0.0001f)
		{
			ClearPreviousAdditivesIfAny();
			return;
		}
		float num = leanX * effectWeight;
		float num2 = leanZ * effectWeight;
		if (useLocalRotation)
		{
			Quaternion quaternion = Quaternion.Euler(num, 0f, num2);
			hips.localRotation *= quaternion;
			lastHipAddLocal = quaternion;
			if (armsAdditive > 0f)
			{
				float num3 = (invertArms ? (-1f) : 1f);
				float x = Mathf.Clamp(limbX * armsAdditive, 0f - armsMaxX, armsMaxX) * num3 * effectWeight;
				float z = Mathf.Clamp(limbZ * armsAdditive, 0f - armsMaxZ, armsMaxZ) * num3 * effectWeight;
				Quaternion quaternion2 = Quaternion.Euler(x, 0f, z);
				if ((bool)leftUpperArm)
				{
					leftUpperArm.localRotation *= quaternion2;
					lastArmLAddLocal = quaternion2;
				}
				else
				{
					lastArmLAddLocal = Quaternion.identity;
				}
				if ((bool)rightUpperArm)
				{
					rightUpperArm.localRotation *= quaternion2;
					lastArmRAddLocal = quaternion2;
				}
				else
				{
					lastArmRAddLocal = Quaternion.identity;
				}
			}
			else
			{
				lastArmLAddLocal = Quaternion.identity;
				lastArmRAddLocal = Quaternion.identity;
			}
			if (legsAdditive > 0f)
			{
				float num4 = (invertLegs ? (-1f) : 1f);
				float x2 = Mathf.Clamp(limbX * legsAdditive, 0f - legsMaxX, legsMaxX) * num4 * effectWeight;
				float z2 = Mathf.Clamp(limbZ * legsAdditive, 0f - legsMaxZ, legsMaxZ) * num4 * effectWeight;
				Quaternion quaternion3 = Quaternion.Euler(x2, 0f, z2);
				if ((bool)leftUpperLeg)
				{
					leftUpperLeg.localRotation *= quaternion3;
					lastLegLAddLocal = quaternion3;
				}
				else
				{
					lastLegLAddLocal = Quaternion.identity;
				}
				if ((bool)rightUpperLeg)
				{
					rightUpperLeg.localRotation *= quaternion3;
					lastLegRAddLocal = quaternion3;
				}
				else
				{
					lastLegRAddLocal = Quaternion.identity;
				}
			}
			else
			{
				lastLegLAddLocal = Quaternion.identity;
				lastLegRAddLocal = Quaternion.identity;
			}
			lastHipAddWorld = Quaternion.identity;
			lastArmLAddWorld = Quaternion.identity;
			lastArmRAddWorld = Quaternion.identity;
			lastLegLAddWorld = Quaternion.identity;
			lastLegRAddWorld = Quaternion.identity;
			return;
		}
		Transform transform = (globalReference ? globalReference : base.transform);
		Quaternion quaternion4 = Quaternion.AngleAxis(num, transform.right) * Quaternion.AngleAxis(num2, transform.forward);
		hips.rotation = quaternion4 * hips.rotation;
		lastHipAddWorld = quaternion4;
		if (armsAdditive > 0f)
		{
			float num5 = (invertArms ? (-1f) : 1f);
			float angle = Mathf.Clamp(limbX * armsAdditive, 0f - armsMaxX, armsMaxX) * num5 * effectWeight;
			float angle2 = Mathf.Clamp(limbZ * armsAdditive, 0f - armsMaxZ, armsMaxZ) * num5 * effectWeight;
			Quaternion quaternion5 = Quaternion.AngleAxis(angle, transform.right) * Quaternion.AngleAxis(angle2, transform.forward);
			if ((bool)leftUpperArm)
			{
				leftUpperArm.rotation = quaternion5 * leftUpperArm.rotation;
				lastArmLAddWorld = quaternion5;
			}
			else
			{
				lastArmLAddWorld = Quaternion.identity;
			}
			if ((bool)rightUpperArm)
			{
				rightUpperArm.rotation = quaternion5 * rightUpperArm.rotation;
				lastArmRAddWorld = quaternion5;
			}
			else
			{
				lastArmRAddWorld = Quaternion.identity;
			}
		}
		else
		{
			lastArmLAddWorld = Quaternion.identity;
			lastArmRAddWorld = Quaternion.identity;
		}
		if (legsAdditive > 0f)
		{
			float num6 = (invertLegs ? (-1f) : 1f);
			float angle3 = Mathf.Clamp(limbX * legsAdditive, 0f - legsMaxX, legsMaxX) * num6 * effectWeight;
			float angle4 = Mathf.Clamp(limbZ * legsAdditive, 0f - legsMaxZ, legsMaxZ) * num6 * effectWeight;
			Quaternion quaternion6 = Quaternion.AngleAxis(angle3, transform.right) * Quaternion.AngleAxis(angle4, transform.forward);
			if ((bool)leftUpperLeg)
			{
				leftUpperLeg.rotation = quaternion6 * leftUpperLeg.rotation;
				lastLegLAddWorld = quaternion6;
			}
			else
			{
				lastLegLAddWorld = Quaternion.identity;
			}
			if ((bool)rightUpperLeg)
			{
				rightUpperLeg.rotation = quaternion6 * rightUpperLeg.rotation;
				lastLegRAddWorld = quaternion6;
			}
			else
			{
				lastLegRAddWorld = Quaternion.identity;
			}
		}
		else
		{
			lastLegLAddWorld = Quaternion.identity;
			lastLegRAddWorld = Quaternion.identity;
		}
		lastHipAddLocal = Quaternion.identity;
		lastArmLAddLocal = Quaternion.identity;
		lastArmRAddLocal = Quaternion.identity;
		lastLegLAddLocal = Quaternion.identity;
		lastLegRAddLocal = Quaternion.identity;
	}

	private void EnsureAnimatorAndBones()
	{
		if (!animator)
		{
			Animator componentInParent = GetComponentInParent<Animator>();
			if ((bool)componentInParent)
			{
				animator = componentInParent;
			}
		}
		if (anim != animator)
		{
			anim = animator;
			cachedForBones = null;
		}
		if ((bool)anim && (cachedForBones != anim || !hips))
		{
			hips = anim.GetBoneTransform(HumanBodyBones.Hips);
			leftUpperArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
			rightUpperArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
			leftUpperLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
			rightUpperLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
			cachedForBones = anim;
			ClearPreviousAdditivesIfAny();
		}
	}

	private bool IsInAllowedState()
	{
		if (!useAllowedStatesWhitelist)
		{
			return true;
		}
		if (!anim)
		{
			return false;
		}
		if (allowedStates == null || allowedStates.Length == 0)
		{
			return true;
		}
		AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(Mathf.Clamp(stateLayerIndex, 0, Mathf.Max(0, anim.layerCount - 1)));
		for (int i = 0; i < allowedStates.Length; i++)
		{
			string value = allowedStates[i];
			if (!string.IsNullOrEmpty(value) && currentAnimatorStateInfo.IsName(value))
			{
				return true;
			}
		}
		return false;
	}

	private static void Spring(ref float x, ref float v, float xt, float f, float z, float dt)
	{
		float num = Mathf.Max(0.01f, f) * 2f * (float)Math.PI;
		float num2 = num * num * (xt - x) - 2f * z * num * v;
		v += num2 * dt;
		x += v * dt;
	}

	private void ClearPreviousAdditivesIfAny()
	{
		if (useLocalRotation)
		{
			if ((bool)hips && lastHipAddLocal != Quaternion.identity)
			{
				hips.localRotation *= Quaternion.Inverse(lastHipAddLocal);
			}
			if ((bool)leftUpperArm && lastArmLAddLocal != Quaternion.identity)
			{
				leftUpperArm.localRotation *= Quaternion.Inverse(lastArmLAddLocal);
			}
			if ((bool)rightUpperArm && lastArmRAddLocal != Quaternion.identity)
			{
				rightUpperArm.localRotation *= Quaternion.Inverse(lastArmRAddLocal);
			}
			if ((bool)leftUpperLeg && lastLegLAddLocal != Quaternion.identity)
			{
				leftUpperLeg.localRotation *= Quaternion.Inverse(lastLegLAddLocal);
			}
			if ((bool)rightUpperLeg && lastLegRAddLocal != Quaternion.identity)
			{
				rightUpperLeg.localRotation *= Quaternion.Inverse(lastLegRAddLocal);
			}
			lastHipAddLocal = Quaternion.identity;
			lastArmLAddLocal = Quaternion.identity;
			lastArmRAddLocal = Quaternion.identity;
			lastLegLAddLocal = Quaternion.identity;
			lastLegRAddLocal = Quaternion.identity;
		}
		else
		{
			if ((bool)hips && lastHipAddWorld != Quaternion.identity)
			{
				hips.rotation = Quaternion.Inverse(lastHipAddWorld) * hips.rotation;
			}
			if ((bool)leftUpperArm && lastArmLAddWorld != Quaternion.identity)
			{
				leftUpperArm.rotation = Quaternion.Inverse(lastArmLAddWorld) * leftUpperArm.rotation;
			}
			if ((bool)rightUpperArm && lastArmRAddWorld != Quaternion.identity)
			{
				rightUpperArm.rotation = Quaternion.Inverse(lastArmRAddWorld) * rightUpperArm.rotation;
			}
			if ((bool)leftUpperLeg && lastLegLAddWorld != Quaternion.identity)
			{
				leftUpperLeg.rotation = Quaternion.Inverse(lastLegLAddWorld) * leftUpperLeg.rotation;
			}
			if ((bool)rightUpperLeg && lastLegRAddWorld != Quaternion.identity)
			{
				rightUpperLeg.rotation = Quaternion.Inverse(lastLegRAddWorld) * rightUpperLeg.rotation;
			}
			lastHipAddWorld = Quaternion.identity;
			lastArmLAddWorld = Quaternion.identity;
			lastArmRAddWorld = Quaternion.identity;
			lastLegLAddWorld = Quaternion.identity;
			lastLegRAddWorld = Quaternion.identity;
		}
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
		return true;
	}

	private static Vector2Int GetWindowPosition(IntPtr hWnd)
	{
		GetWindowRect(hWnd, out var lpRect);
		return new Vector2Int(lpRect.left, lpRect.top);
	}
}
