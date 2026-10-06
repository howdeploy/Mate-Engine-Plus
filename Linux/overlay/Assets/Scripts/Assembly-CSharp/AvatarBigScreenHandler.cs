using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class AvatarBigScreenHandler : MonoBehaviour
{
	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

	[Header("Keybinds")]
	public List<KeyCode> ToggleKeys = new List<KeyCode> { KeyCode.B };

	[Header("Animator & Bone Selection")]
	public Animator avatarAnimator;

	public HumanBodyBones attachBone = HumanBodyBones.Head;

	[Header("Camera")]
	public Camera MainCamera;

	[Tooltip("Override for Zoom: Camera FOV (Perspective) or Size (Orthographic). 0 = auto.")]
	public float TargetZoom;

	public float ZoomMoveSpeed = 10f;

	[Tooltip("Y-Offset to bone position (meters, before scaling)")]
	public float YOffset = 0.08f;

	[Header("Fade Animation")]
	public float FadeYOffset = 0.5f;

	public float FadeInDuration = 0.5f;

	public float FadeOutDuration = 0.5f;

	[Header("Canvas Blocking")]
	public GameObject moveCanvas;

	private IntPtr unityHWND = IntPtr.Zero;

	private bool isBigScreenActive;

	public bool IsBigScreenActive => isBigScreenActive;

	private Vector3 originalCamPos;

	private Quaternion originalCamRot;

	private float originalFOV;

	private float originalOrthoSize;

	private RECT originalWindowRect;

	private bool originalRectSet;

	private Transform bone;

	private AvatarAnimatorController avatarAnimatorController;

	private bool moveCanvasWasActive;

	private Coroutine fadeCoroutine;

	private bool isFading;

	private bool isInDesktopTransition;

	public static List<AvatarBigScreenHandler> ActiveHandlers = new List<AvatarBigScreenHandler>();

	private static bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc callback, IntPtr data)
	{
		var wm = WindowManager.Instance;
		if (wm == null) return false;
		foreach (var monitor in wm.GetAllMonitors())
		{
			var bounds = monitor.Value;
			var rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
			if (!callback(monitor.Key, IntPtr.Zero, ref rect, data)) break;
		}
		return true;
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
		return true;
	}

	private static bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool bRepaint)
	{
		var wm = WindowManager.Instance;
		if (wm == null || window != wm.UnityWindow) return false;
		wm.SetWindowPosition(x, y);
		return true;
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

	public void ToggleBigScreenFromUI()
	{
		if (isFading || isInDesktopTransition) return;
		if (!isBigScreenActive)
		{
			ActivateBigScreen();
		}
		else
		{
			DeactivateBigScreen();
		}
	}

	private void Start()
	{
		unityHWND = WindowManager.Instance.UnityWindow;
		if (MainCamera == null)
		{
			MainCamera = Camera.main;
		}
		if (avatarAnimator == null)
		{
			avatarAnimator = GetComponent<Animator>();
		}
		if (MainCamera != null)
		{
			originalCamPos = MainCamera.transform.position;
			originalCamRot = MainCamera.transform.rotation;
			originalFOV = MainCamera.fieldOfView;
			originalOrthoSize = MainCamera.orthographicSize;
		}
		if (unityHWND != IntPtr.Zero && GetWindowRect(unityHWND, out var lpRect))
		{
			originalWindowRect = lpRect;
			originalRectSet = true;
		}
		avatarAnimatorController = GetComponent<AvatarAnimatorController>();
	}

	public void SetAnimator(Animator a)
	{
		avatarAnimator = a;
	}

	private void Update()
	{
		foreach (KeyCode toggleKey in ToggleKeys)
		{
			if (Input.GetKeyDown(toggleKey))
			{
				ToggleBigScreenFromUI();
				break;
			}
		}
	}

	private void LateUpdate()
	{
		if (isBigScreenActive && MainCamera != null && bone != null && avatarAnimator != null && !isFading && !isInDesktopTransition)
		{
			UpdateBigScreenCamera();
		}
	}

	private void UpdateBigScreenCamera()
	{
		GetPortraitCameraTarget(out var position, out var zoom);
		float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, ZoomMoveSpeed) * Time.deltaTime);
		MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, position, blend);
		MainCamera.transform.rotation = Quaternion.Slerp(MainCamera.transform.rotation, Quaternion.identity, blend);
		SetCameraZoom(Mathf.Lerp(GetCameraZoom(), zoom, blend));
	}

	private void GetPortraitCameraTarget(out Vector3 position, out float zoom)
	{
		float scale = avatarAnimator.transform.lossyScale.y;
		Vector3 head = bone.position;
		position = new Vector3(head.x, head.y + YOffset * scale, originalCamPos.z);
		if (TargetZoom > 0f)
		{
			zoom = MainCamera.orthographic ? TargetZoom * Mathf.Abs(scale) : TargetZoom;
			return;
		}
		Transform neck = avatarAnimator.GetBoneTransform(HumanBodyBones.Neck);
		float headHeight = Mathf.Max(0.12f * Mathf.Abs(scale), neck ? Mathf.Abs(head.y - neck.position.y) : 0.25f * Mathf.Abs(scale));
		float extent = headHeight * 1.4f;
		if (!MainCamera.orthographic)
		{
			float distance = Mathf.Max(0.01f, Mathf.Abs(position.z - head.z));
			float fov = Mathf.Clamp(2f * Mathf.Atan(extent / (2f * distance)) * Mathf.Rad2Deg, 10f, 60f);
			extent = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
		}
		extent = FitPortraitHands(headHeight, position, extent);
		zoom = MainCamera.orthographic ? extent : Mathf.Clamp(2f * Mathf.Atan(extent) * Mathf.Rad2Deg, 1f, 179f);
	}

	private float FitPortraitHands(float headHeight, Vector3 cameraPosition, float baseExtent)
	{
		if (!avatarAnimator.isHuman) return baseExtent;
		float aspect = Mathf.Max(0.01f, MainCamera.aspect);
		float inset = Mathf.Clamp01(1f - 24f / Mathf.Max(25f, Mathf.Min(MainCamera.pixelWidth, MainCamera.pixelHeight)));
		float vertical = baseExtent;
		float padding = headHeight * 0.3f;
		foreach (HumanBodyBones hand in PortraitHandBones)
		{
			Transform joint = avatarAnimator.GetBoneTransform(hand);
			if (joint == null) continue;
			// The portrait target has identity rotation. Measure against it,
			// not against the still-interpolating camera from the previous frame.
			Vector3 point = joint.position - cameraPosition;
			if (point.z <= MainCamera.nearClipPlane) continue;
			// Portrait mode intentionally crops the lower body. Fit hands that
			// enter the portrait, not idle hands hanging below its bottom edge.
			float bottom = MainCamera.orthographic ? -baseExtent : -baseExtent * point.z;
			if (point.y + padding < bottom) continue;
			float required = Mathf.Max(Mathf.Abs(point.y) + padding, (Mathf.Abs(point.x) + padding) / aspect) / Mathf.Max(0.01f, inset);
			if (!MainCamera.orthographic) required /= point.z;
			vertical = Mathf.Max(vertical, required);
		}
		return vertical;
	}

	private float GetCameraZoom()
	{
		return MainCamera.orthographic ? MainCamera.orthographicSize : MainCamera.fieldOfView;
	}

	private void SetCameraZoom(float zoom)
	{
		if (MainCamera.orthographic) MainCamera.orthographicSize = zoom;
		else MainCamera.fieldOfView = zoom;
	}

	private static readonly HumanBodyBones[] PortraitHandBones = {
		HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
		HumanBodyBones.LeftMiddleDistal, HumanBodyBones.RightMiddleDistal,
		HumanBodyBones.LeftThumbDistal, HumanBodyBones.RightThumbDistal
	};

	private void ActivateBigScreen()
	{
		if (!isBigScreenActive && !isFading && !isInDesktopTransition)
		{
			SaveCameraState();
			if (unityHWND != IntPtr.Zero && GetWindowRect(unityHWND, out var rect))
			{
				originalWindowRect = rect;
				originalRectSet = true;
			}
			// Portrait owns the window position; a previous seat must not keep
			// moving it while the camera follows the portrait animation.
			var seating = GetComponent<AvatarWindowHandler>();
			if (seating != null) seating.ForceExitWindowSitting();
			if (moveCanvas != null)
			{
				moveCanvasWasActive = moveCanvas.activeSelf;
			}
			isBigScreenActive = true;
			if (avatarAnimator != null)
			{
				avatarAnimator.SetBool("isBigScreen", value: true);
			}
			if (avatarAnimatorController != null)
			{
				avatarAnimatorController.BlockDraggingOverride = true;
			}
			if (moveCanvas != null && moveCanvas.activeSelf)
			{
				moveCanvas.SetActive(value: false);
			}
			bone = (avatarAnimator ? avatarAnimator.GetBoneTransform(attachBone) : null);
			if (fadeCoroutine != null)
			{
				StopCoroutine(fadeCoroutine);
			}
			isFading = true;
			isInDesktopTransition = true;
			fadeCoroutine = StartCoroutine(BigScreenEnterSequence());
		}
	}

	private void DeactivateBigScreen()
	{
		if (!isBigScreenActive) return;
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}
		isFading = true;
		// If the opening glide was interrupted, keep window anchoring paused
		// until the exit sequence restores the desktop position.
		fadeCoroutine = StartCoroutine(BigScreenExitSequence());
	}

	private void SaveCameraState()
	{
		if (MainCamera != null)
		{
			originalCamPos = MainCamera.transform.position;
			originalCamRot = MainCamera.transform.rotation;
			originalFOV = MainCamera.fieldOfView;
			originalOrthoSize = MainCamera.orthographicSize;
		}
	}

	private IEnumerator FadeCameraY(bool fadeIn)
	{
		if (avatarAnimator == null || bone == null || MainCamera == null)
		{
			yield break;
		}
		if (fadeIn)
		{
			GetPortraitCameraTarget(out var position, out var zoom);
			yield return BlendCameraTo(position, Quaternion.identity, zoom, FadeInDuration);
		}
		else
		{
			yield return BlendCameraTo(MainCamera.transform.position + Vector3.up * FadeYOffset,
				MainCamera.transform.rotation, GetCameraZoom(), FadeOutDuration);
		}
		if (!fadeIn)
		{
			isBigScreenActive = false;
			if (avatarAnimator != null)
			{
				avatarAnimator.SetBool("isBigScreen", value: false);
			}
			if (avatarAnimatorController != null)
			{
				avatarAnimatorController.BlockDraggingOverride = false;
			}
			if (moveCanvas != null && moveCanvasWasActive)
			{
				moveCanvas.SetActive(value: true);
			}
			if (unityHWND != IntPtr.Zero && originalRectSet)
			{
				int nWidth = originalWindowRect.right - originalWindowRect.left;
				int nHeight = originalWindowRect.bottom - originalWindowRect.top;
				MoveWindow(unityHWND, originalWindowRect.left, originalWindowRect.top, nWidth, nHeight, bRepaint: true);
			}
		}
	}

	private IEnumerator BlendCameraTo(Vector3 position, Quaternion rotation, float zoom, float duration)
	{
		Vector3 fromPosition = MainCamera.transform.position;
		Quaternion fromRotation = MainCamera.transform.rotation;
		float fromZoom = GetCameraZoom();
		for (float time = 0f; time < duration; time += Time.deltaTime)
		{
			float blend = Mathf.SmoothStep(0f, 1f, time / duration);
			MainCamera.transform.position = Vector3.Lerp(fromPosition, position, blend);
			MainCamera.transform.rotation = Quaternion.Slerp(fromRotation, rotation, blend);
			SetCameraZoom(Mathf.Lerp(fromZoom, zoom, blend));
			yield return null;
		}
		MainCamera.transform.position = position;
		MainCamera.transform.rotation = rotation;
		SetCameraZoom(zoom);
	}

	private RECT FindBestMonitorRect(RECT windowRect)
	{
		List<RECT> monitorRects = new List<RECT>();
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr data)
		{
			monitorRects.Add(lprcMonitor);
			return true;
		}, IntPtr.Zero);
		int index = 0;
		int num = 0;
		for (int num2 = 0; num2 < monitorRects.Count; num2++)
		{
			int num3 = OverlapArea(windowRect, monitorRects[num2]);
			if (num3 > num)
			{
				index = num2;
				num = num3;
			}
		}
		if (monitorRects.Count <= 0)
		{
			return new RECT
			{
				left = 0,
				top = 0,
				right = Screen.currentResolution.width,
				bottom = Screen.currentResolution.height
			};
		}
		return monitorRects[index];
	}

	private int OverlapArea(RECT a, RECT b)
	{
		int num = Math.Max(a.left, b.left);
		int num2 = Math.Min(a.right, b.right);
		int num3 = Math.Max(a.top, b.top);
		int num4 = Math.Min(a.bottom, b.bottom);
		int num5 = num2 - num;
		int num6 = num4 - num3;
		if (num5 <= 0 || num6 <= 0)
		{
			return 0;
		}
		return num5 * num6;
	}

	private IEnumerator GlideAvatarDesktop(float duration, bool toFadeY)
	{
		isInDesktopTransition = true;
		if (avatarAnimator == null || bone == null || MainCamera == null)
		{
			isInDesktopTransition = false;
			yield break;
		}
		Vector3 position = originalCamPos;
		if (toFadeY) position.y += FadeYOffset;
		yield return BlendCameraTo(position, originalCamRot,
			MainCamera.orthographic ? originalOrthoSize : originalFOV, duration);
		if (toFadeY && unityHWND != IntPtr.Zero && GetWindowRect(unityHWND, out var lpRect))
		{
			RECT rECT = FindBestMonitorRect(lpRect);
			int nWidth = rECT.right - rECT.left;
			int nHeight = rECT.bottom - rECT.top;
			MoveWindow(unityHWND, rECT.left, rECT.top, nWidth, nHeight, bRepaint: true);
		}
		if (!toFadeY && MainCamera != null)
		{
			MainCamera.transform.position = originalCamPos;
			MainCamera.transform.rotation = originalCamRot;
			MainCamera.fieldOfView = originalFOV;
			MainCamera.orthographicSize = originalOrthoSize;
		}
		isInDesktopTransition = false;
	}

	private IEnumerator BigScreenEnterSequence()
	{
		yield return GlideAvatarDesktop(0.4f, toFadeY: true);
		yield return FadeCameraY(fadeIn: true);
		isFading = false;
		fadeCoroutine = null;
	}

	private IEnumerator BigScreenExitSequence()
	{
		yield return FadeCameraY(fadeIn: false);
		yield return GlideAvatarDesktop(0.4f, toFadeY: false);
		isFading = false;
		fadeCoroutine = null;
	}
}
