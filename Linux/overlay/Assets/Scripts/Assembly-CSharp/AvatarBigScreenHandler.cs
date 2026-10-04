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
				if (!isBigScreenActive && !isFading)
				{
					ActivateBigScreen();
				}
				else if (isBigScreenActive && !isFading)
				{
					DeactivateBigScreen();
				}
				break;
			}
		}
		if (isBigScreenActive && MainCamera != null && bone != null && avatarAnimator != null && !isFading && !isInDesktopTransition)
		{
			UpdateBigScreenCamera();
		}
	}

	private void UpdateBigScreenCamera()
	{
		float y = avatarAnimator.transform.lossyScale.y;
		Vector3 position = bone.position;
		Transform boneTransform = avatarAnimator.GetBoneTransform(HumanBodyBones.Neck);
		float num = Mathf.Max(0.12f, boneTransform ? Mathf.Abs(position.y - boneTransform.position.y) : 0.25f) * y;
		float num2 = 1.4f;
		Vector3 position2 = originalCamPos;
		position2.y = position.y + YOffset * y;
		MainCamera.transform.position = position2;
		MainCamera.transform.rotation = Quaternion.identity;
		if (TargetZoom > 0f)
		{
			if (MainCamera.orthographic)
			{
				MainCamera.orthographicSize = TargetZoom * y;
			}
			else
			{
				MainCamera.fieldOfView = TargetZoom;
			}
		}
		else if (MainCamera.orthographic)
		{
			MainCamera.orthographicSize = num * num2;
		}
		else
		{
			float num3 = Mathf.Abs(MainCamera.transform.position.z - position.z);
			MainCamera.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(num * num2 / (2f * num3)) * 57.29578f, 10f, 60f);
		}
	}

	private void ActivateBigScreen()
	{
		if (!isBigScreenActive)
		{
			SaveCameraState();
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
			fadeCoroutine = StartCoroutine(BigScreenEnterSequence());
		}
	}

	private void DeactivateBigScreen()
	{
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}
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
		isFading = true;
		if (avatarAnimator == null || bone == null || MainCamera == null)
		{
			isFading = false;
			yield break;
		}
		float scale = avatarAnimator.transform.lossyScale.y;
		Vector3 headPos = bone.position;
		float num = headPos.y + YOffset * scale;
		float num2 = num + FadeYOffset;
		Vector3 camPos = MainCamera.transform.position;
		float fromY = (fadeIn ? num2 : num);
		float toY = (fadeIn ? num : num2);
		float duration = (fadeIn ? FadeInDuration : FadeOutDuration);
		float time = 0f;
		Transform boneTransform = avatarAnimator.GetBoneTransform(HumanBodyBones.Neck);
		float headHeight = Mathf.Max(0.12f, boneTransform ? Mathf.Abs(headPos.y - boneTransform.position.y) : 0.25f) * scale;
		float buffer = 1.4f;
		while (time < duration)
		{
			float t = Mathf.SmoothStep(0f, 1f, time / duration);
			camPos.y = Mathf.Lerp(fromY, toY, t);
			MainCamera.transform.position = camPos;
			MainCamera.transform.rotation = Quaternion.identity;
			if (TargetZoom > 0f)
			{
				if (MainCamera.orthographic)
				{
					MainCamera.orthographicSize = TargetZoom * scale;
				}
				else
				{
					MainCamera.fieldOfView = TargetZoom;
				}
			}
			else if (MainCamera.orthographic)
			{
				MainCamera.orthographicSize = headHeight * buffer;
			}
			else
			{
				float num3 = Mathf.Abs(MainCamera.transform.position.z - headPos.z);
				MainCamera.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(headHeight * buffer / (2f * num3)) * 57.29578f, 10f, 60f);
			}
			time += Time.deltaTime;
			yield return null;
		}
		camPos.y = toY;
		MainCamera.transform.position = camPos;
		MainCamera.transform.rotation = Quaternion.identity;
		if (TargetZoom > 0f)
		{
			if (MainCamera.orthographic)
			{
				MainCamera.orthographicSize = TargetZoom * scale;
			}
			else
			{
				MainCamera.fieldOfView = TargetZoom;
			}
		}
		else if (MainCamera.orthographic)
		{
			MainCamera.orthographicSize = headHeight * buffer;
		}
		else
		{
			float num4 = Mathf.Abs(MainCamera.transform.position.z - headPos.z);
			MainCamera.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(headHeight * buffer / (2f * num4)) * 57.29578f, 10f, 60f);
		}
		isFading = false;
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
			if (MainCamera != null)
			{
				MainCamera.transform.position = originalCamPos;
				MainCamera.transform.rotation = originalCamRot;
				MainCamera.fieldOfView = originalFOV;
				MainCamera.orthographicSize = originalOrthoSize;
			}
		}
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
		float y = avatarAnimator.transform.lossyScale.y;
		float num = bone.position.y + YOffset * y;
		float num2 = num + FadeYOffset;
		Vector3 camPos = MainCamera.transform.position;
		float fromY = (toFadeY ? num : num2);
		float toY = (toFadeY ? num2 : num);
		float time = 0f;
		while (time < duration)
		{
			camPos.y = Mathf.Lerp(fromY, toY, Mathf.SmoothStep(0f, 1f, time / duration));
			MainCamera.transform.position = camPos;
			time += Time.deltaTime;
			yield return null;
		}
		camPos.y = toY;
		MainCamera.transform.position = camPos;
		if (toFadeY && unityHWND != IntPtr.Zero && GetWindowRect(unityHWND, out var lpRect))
		{
			RECT rECT = FindBestMonitorRect(lpRect);
			int nWidth = rECT.right - rECT.left;
			int nHeight = rECT.bottom - rECT.top;
			MoveWindow(unityHWND, rECT.left, rECT.top, nWidth, nHeight, bRepaint: true);
			originalWindowRect = lpRect;
			originalRectSet = true;
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
		yield return StartCoroutine(GlideAvatarDesktop(0.4f, toFadeY: true));
		yield return StartCoroutine(FadeCameraY(fadeIn: true));
	}

	private IEnumerator BigScreenExitSequence()
	{
		yield return StartCoroutine(FadeCameraY(fadeIn: false));
		yield return StartCoroutine(GlideAvatarDesktop(0.4f, toFadeY: false));
	}
}
