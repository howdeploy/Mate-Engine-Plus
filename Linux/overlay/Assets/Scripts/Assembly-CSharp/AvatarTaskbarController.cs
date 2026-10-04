using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteAlways]
public class AvatarTaskbarController : MonoBehaviour
{
	private struct APPBARDATA
	{
		public int cbSize;

		public IntPtr hWnd;

		public uint uCallbackMessage;

		public uint uEdge;

		public RECT rc;

		public int lParam;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[Header("Animator")]
	public Animator avatarAnimator;

	[Header("Detection Settings")]
	public Vector2 snapZoneOffset = new Vector2(0f, -5f);

	public Vector2 snapZoneSize = new Vector2(100f, 10f);

	[Header("Attach Settings")]
	public GameObject attachTarget;

	public HumanBodyBones attachBone = HumanBodyBones.Head;

	public bool keepOriginalRotation;

	[Header("Spawn / Despawn Animation")]
	public float spawnScaleTime = 0.2f;

	public float despawnScaleTime = 0.2f;

	[Header("Debug")]
	public bool showDebugGizmo = true;

	public Color taskbarGizmoColor = Color.green;

	public Color pinkZoneGizmoColor = Color.magenta;

	private IntPtr unityHWND;

	private Vector2 unityPos;

	private Rect taskbarRect;

	private Rect pinkZoneDesktopRect;

	private Animator animator;

	private Transform attachBoneTransform;

	private Transform originalAttachParent;

	private Vector3 originalScale = Vector3.one;

	private float scaleLerpT;

	private bool isScaling;

	private bool scalingUp;

	private bool wasAllowSpawn;

	private static readonly int IsSitting = Animator.StringToHash("isSitting");

	private const int ABM_GETTASKBARPOS = 5;

	private void Start()
	{
		if (!Application.isPlaying || WindowManager.Instance == null) return;
		unityHWND = WindowManager.Instance.UnityWindow;
		animator = avatarAnimator ?? GetComponent<Animator>();
		if (attachTarget != null)
		{
			originalScale = attachTarget.transform.localScale;
			originalAttachParent = attachTarget.transform.parent;
			attachTarget.SetActive(value: false);
		}
		UpdateTaskbarRect();
	}

	public void SetAnimator(Animator newAnimator)
	{
		avatarAnimator = newAnimator;
	}

	private void Update()
	{
		if (unityHWND == IntPtr.Zero || animator == null)
		{
			return;
		}
		UpdateUnityWindowPosition();
		UpdateTaskbarRect();
		UpdatePinkZone();
		Rect other = new Rect(taskbarRect.x, taskbarRect.y, taskbarRect.width, 5f);
		bool flag = pinkZoneDesktopRect.Overlaps(other);
		animator.SetBool(IsSitting, flag);
		bool flag2 = flag && animator.GetCurrentAnimatorStateInfo(0).IsName("Sitting");
		if (attachBoneTransform == null && attachTarget != null)
		{
			attachBoneTransform = animator.GetBoneTransform(attachBone);
		}
		if (attachTarget != null)
		{
			if (flag2 && !keepOriginalRotation && attachBoneTransform != null)
			{
				attachTarget.transform.SetParent(attachBoneTransform, worldPositionStays: false);
			}
			else if (!flag2 && !keepOriginalRotation && attachTarget.transform.parent != originalAttachParent)
			{
				attachTarget.transform.SetParent(originalAttachParent, worldPositionStays: false);
			}
		}
		if (attachTarget != null && flag2 && !wasAllowSpawn)
		{
			attachTarget.SetActive(value: true);
			attachTarget.transform.localScale = Vector3.zero;
			scaleLerpT = 0f;
			scalingUp = true;
			isScaling = true;
		}
		if (attachTarget != null && !flag2 && attachTarget.activeSelf && (!isScaling || scalingUp))
		{
			scalingUp = false;
			isScaling = true;
			scaleLerpT = 0f;
		}
		if (attachTarget != null && isScaling && attachTarget.activeSelf)
		{
			float a = (scalingUp ? spawnScaleTime : despawnScaleTime);
			scaleLerpT += Time.deltaTime / Mathf.Max(a, 0.0001f);
			float num = Mathf.Clamp01(scaleLerpT);
			Vector3 a2 = (scalingUp ? Vector3.zero : originalScale);
			Vector3 b = (scalingUp ? originalScale : Vector3.zero);
			attachTarget.transform.localScale = Vector3.Lerp(a2, b, num);
			if (num >= 1f)
			{
				isScaling = false;
				if (!scalingUp)
				{
					attachTarget.SetActive(value: false);
					attachTarget.transform.localScale = originalScale;
				}
			}
		}
		if (attachTarget != null && attachTarget.activeSelf && keepOriginalRotation && attachBoneTransform != null)
		{
			attachTarget.transform.position = attachBoneTransform.position;
		}
		wasAllowSpawn = flag2;
	}

	private void UpdatePinkZone()
	{
		GetWindowRect(unityHWND, out var lpRect);
		int num = lpRect.Right - lpRect.Left;
		int num2 = lpRect.Bottom - lpRect.Top;
		float num3 = unityPos.x + (float)num / 2f + snapZoneOffset.x;
		float y = unityPos.y + (float)num2 + snapZoneOffset.y;
		pinkZoneDesktopRect = new Rect(num3 - snapZoneSize.x / 2f, y, snapZoneSize.x, snapZoneSize.y);
	}

	private void UpdateUnityWindowPosition()
	{
		GetWindowRect(unityHWND, out var lpRect);
		unityPos = new Vector2(lpRect.Left, lpRect.Top);
	}

	private void UpdateTaskbarRect()
	{
		taskbarRect = MonitorHelper.GetTaskbarRectForWindow(unityHWND);
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying && showDebugGizmo)
		{
			float basePixel = 1000f;
			Rect desktopRect = new Rect(taskbarRect.x, taskbarRect.y, taskbarRect.width, 5f);
			Gizmos.color = taskbarGizmoColor;
			DrawDesktopRect(desktopRect, basePixel);
			Gizmos.color = pinkZoneGizmoColor;
			DrawDesktopRect(pinkZoneDesktopRect, basePixel);
		}
	}

	private void DrawDesktopRect(Rect desktopRect, float basePixel)
	{
		float num = desktopRect.x + desktopRect.width / 2f;
		float num2 = desktopRect.y + desktopRect.height / 2f;
		int systemWidth = Display.main.systemWidth;
		int systemHeight = Display.main.systemHeight;
		float x = (num - (float)systemWidth / 2f) / basePixel;
		float y = (0f - (num2 - (float)systemHeight / 2f)) / basePixel;
		Vector3 center = new Vector3(x, y, 0f);
		Vector3 size = new Vector3(desktopRect.width / basePixel, desktopRect.height / basePixel, 0f);
		Gizmos.DrawWireCube(center, size);
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { Left = bounds.xMin, Top = bounds.yMin, Right = bounds.xMax, Bottom = bounds.yMax };
		return true;
	}
}
