using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class AvatarWindowHandler : MonoBehaviour
{
	public struct WINDOWPLACEMENT
	{
		public int length;

		public int flags;

		public int showCmd;

		public POINT ptMinPosition;

		public POINT ptMaxPosition;

		public RECT rcNormalPosition;
	}

	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct POINT
	{
		public int X;

		public int Y;
	}

	private struct WindowEntry
	{
		public IntPtr hwnd;

		public RECT rect;

		public bool isTaskbar;
	}

	[Header("Snap Safety")]
	public float minDragHoldSecondsToSit = 1f;

	public float unsnapCooldownSeconds = 0.3f;

	private float _dragStartTime = -1f;

	private bool _canSitHold;

	private float _unsnapCooldownUntil = -1f;

	[Header("Snap Probe Offset")]
	public float probeZoneYOffsetLocal;

	[Header("Snap Probe")]
	public float probeRadiusPx = 24f;

	public bool showProbeGizmo = true;

	public Color probeGizmoColor = Color.magenta;

	private bool _guardZoneActive;

	private Vector2 _guardCenterDesktop;

	[Header("Snap Guard Zone")]
	public bool useGuardZone = true;

	public float probeGuardPx = 240f;

	public Color probeGuardGizmoColor = Color.cyan;

	[Header("Sit Blockers")]
	public List<string> blockSitIfBoolTrue = new List<string>();

	private readonly List<string> _blockSitValidNames = new List<string>();

	[Header("Window Sit BlendTree")]
	public int totalWindowSitAnimations = 4;

	private static readonly int windowSitIndexParam = Animator.StringToHash("WindowSitIndex");

	private bool wasSitting;

	[Header("Seat Alignment")]
	[Range(-256f, 256f)]
	public float seatOffsetPx;

	[Range(-0.05f, 0.05f)]
	public float windowSitYOffset;

	[Header("Occluder")]
	public Material occluderMaterial;

	public Camera targetCamera;

	public float targetQuadZOffset = 0.001f;

	public float othersQuadZOffset = 0.002f;

	public int maxOtherQuads = 12;

	[Header("Occluder Pool")]
	public bool precreateQuadsOnStart = true;

	public int prewarmOtherQuads = 6;

	[Header("Target Quad Z Auto-Scale")]
	public bool autoScaleTargetZ = true;

	public float targetZBase = 3.2f;

	public float targetZRefScale = 1f;

	public float targetZSensitivity = 3f;

	public float targetZMin = 0.05f;

	public float targetZMax = 10f;

	[Header("Snap Smoothing")]
	public bool enableSnapSmoothing = true;

	[Range(0.01f, 0.5f)]
	public float snapSmoothingTime = 0.12f;

	public float snapSmoothingMaxSpeed = 6000f;

	private bool _snapSmoothingActive;

	private float _snapVelX;

	private float _snapVelY;

	private bool _havePrevSnapRect;

	private RECT _prevSnapRect;

	private Vector3 _prevLossyScale;

	[Header("Snap Trigger")]
	public int minDragPixelsToSnap = 4;

	private int _dragStartCursorX;

	private int _dragStartCursorY;

	private bool _postSettleRecalib;

	private int _postSettleFrames;

	[Header("Snap Guard")]
	public int snapGuardFrames = 8;

	public int snapLatchFrames = 18;

	public int unsnapVerticalBand = 16;

	[Header("Transparent-Window-Filter")]
	[Range(0f, 255f)]
	public int layeredAlphaIgnoreBelow = 230;

	public bool ignoreLayeredClickThrough = true;

	public bool ignoreLayeredToolOrNoActivate = true;

	public bool ignoreLayeredWithColorKey = true;

	[Header("Performance")]
	public float windowEnumFPS = 15f;

	public float windowEnumIdleFPS = 8f;

	private float snapFraction;

	private int _snapCursorY;

	private bool wasDragging;

	private IntPtr snappedHWND = IntPtr.Zero;

	private IntPtr unityHWND = IntPtr.Zero;

	private Vector2 lastDesktopPosition;

	private readonly List<WindowEntry> cachedWindows = new List<WindowEntry>(128);

	private readonly List<WindowEntry> activeOccluders = new List<WindowEntry>(16);

	private Animator animator;

	private AvatarAnimatorController controller;

	private readonly StringBuilder classNameBuffer = new StringBuilder(256);

	private Transform occluderRoot;

	private GameObject targetQuadGO;

	private Mesh targetMesh;

	private readonly List<GameObject> otherQuadGOs = new List<GameObject>(16);

	private readonly List<Mesh> otherMeshes = new List<Mesh>(16);

	private Material _occluderSharedMat;

	private int _guard;

	private int _latch;

	private float _nextEnumTime;

	private RECT _lastUnityCli;

	private bool _haveUnityCli;

	private static readonly int[] TRI = new int[6] { 0, 1, 2, 0, 2, 3 };

	private readonly Vector3[] verts4 = new Vector3[4];

	private readonly Vector3[] verts4Other = new Vector3[4];

	private Transform boneHips;

	private Transform boneLUL;

	private Transform boneRUL;

	private Transform boneLFoot;

	private Transform boneRFoot;

	private Transform boneHead;

	private SkinnedMeshRenderer[] skinned;

	private bool _skinnedCached;

	private bool seatCalibrated;

	private Vector3 seatLocalAtSnap;

	private Vector3 boundsMinSnapLocal;

	private Vector3 boundsSizeSnapLocal;

	private float seatNormY;

	private bool _recentUnsnap;

	private int _lastSnapTopY;

	private uint _currentPid;

	private float _guardRadiusSq;

	private const int SW_MAXIMIZE = 3;

	private const int DWMWA_CLOAKED = 14;

	private const uint GW_HWNDPREV = 3u;

	private const int GWL_STYLE = -16;

	private const int GWL_EXSTYLE = -20;

	private const int WS_CAPTION = 12582912;

	private const int WS_EX_LAYERED = 524288;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const uint LWA_COLORKEY = 1u;

	private const uint LWA_ALPHA = 2u;

	private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

	private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

	private const uint GA_ROOT = 2u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOACTIVATE = 16u;

	private Vector3 GetProbeWorld()
	{
		return GetHipWorld() + base.transform.up * (probeZoneYOffsetLocal * base.transform.lossyScale.y);
	}

	private void Start()
	{
		unityHWND = WindowManager.Instance.UnityWindow;
		_currentPid = GetCurrentProcessId();
		animator = GetComponent<Animator>();
		controller = GetComponent<AvatarAnimatorController>();
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
		}
		CacheRigRefs();
		BuildBlockSitCache();
		EnsureOccluderRoot();
		if (occluderMaterial != null)
		{
			_occluderSharedMat = new Material(occluderMaterial);
		}
		if (precreateQuadsOnStart)
		{
			EnsureTargetQuad();
			int num = Mathf.Clamp(prewarmOtherQuads, 0, Mathf.Max(maxOtherQuads, 0));
			for (int i = 0; i < num; i++)
			{
				EnsureOtherQuad(i);
			}
			SetTargetQuadActive(on: false);
			SetOtherQuadsActive(0);
		}
		SetTopMost(!(SaveLoadHandler.Instance != null) || SaveLoadHandler.Instance.data.isTopmost);
		_nextEnumTime = 0f;
		_prevLossyScale = base.transform.lossyScale;
		_lastSnapTopY = -2147483648;
		cachedWindows.Capacity = Mathf.Max(cachedWindows.Capacity, 128);
		activeOccluders.Capacity = Mathf.Max(activeOccluders.Capacity, maxOtherQuads);
	}

	private void OnDisable()
	{
		ClearSnapAndHide();
	}

	private void OnDestroy()
	{
		CleanupOccluderArtifacts();
	}

	private void BuildBlockSitCache()
	{
		_blockSitValidNames.Clear();
		if (animator == null || blockSitIfBoolTrue == null || blockSitIfBoolTrue.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(blockSitIfBoolTrue);
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].type == AnimatorControllerParameterType.Bool && hashSet.Contains(parameters[i].name))
			{
				_blockSitValidNames.Add(parameters[i].name);
			}
		}
	}

	private bool IsSitBlocked()
	{
		if (animator == null || _blockSitValidNames.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < _blockSitValidNames.Count; i++)
		{
			if (animator.GetBool(_blockSitValidNames[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void Update()
	{
		float px;
		if (snappedHWND != IntPtr.Zero)
		{
			if ((base.transform.lossyScale - _prevLossyScale).sqrMagnitude > 1E-08f)
			{
				_snapSmoothingActive = false;
				px = (_snapVelX = (_snapVelY = 0f));
			}
			_prevLossyScale = base.transform.lossyScale;
		}
		if (unityHWND == IntPtr.Zero || animator == null || controller == null)
		{
			return;
		}
		if (!SaveLoadHandler.Instance.data.enableWindowSitting)
		{
			ClearSnapAndHide();
			return;
		}
		if (IsSitBlocked())
		{
			if (snappedHWND != IntPtr.Zero)
			{
				ClearSnapAndHide();
			}
			return;
		}
		bool flag = animator.GetBool("isWindowSit");
		if (flag && !wasSitting)
		{
			animator.SetFloat(windowSitIndexParam, UnityEngine.Random.Range(0, totalWindowSitAnimations));
		}
		wasSitting = flag;
		float num = ((controller.isDragging || snappedHWND != IntPtr.Zero) ? Mathf.Max(1f, windowEnumFPS) : Mathf.Max(1f, windowEnumIdleFPS));
		if (Time.unscaledTime >= _nextEnumTime)
		{
			UpdateCachedWindows();
			if (snappedHWND != IntPtr.Zero)
			{
				RebuildActiveOccluders();
			}
			_nextEnumTime = Time.unscaledTime + 1f / num;
		}
		if (controller.isDragging && !wasDragging)
		{
			if (GetCursorPos(out var point))
			{
				_dragStartCursorX = point.x;
				_dragStartCursorY = point.y;
				if (snappedHWND != IntPtr.Zero && flag)
				{
					_snapCursorY = point.y;
				}
			}
			_dragStartTime = Time.unscaledTime;
			_canSitHold = false;
		}
		if (controller.isDragging)
		{
			if (!_canSitHold && _dragStartTime >= 0f && Time.unscaledTime - _dragStartTime >= minDragHoldSecondsToSit)
			{
				_canSitHold = true;
			}
		}
		else
		{
			_canSitHold = false;
			_dragStartTime = -1f;
		}
		if (_recentUnsnap)
		{
			float py;
			if (!controller.isDragging)
			{
				_recentUnsnap = false;
			}
			else if (ComputeZoneDesktop(out px, out py))
			{
				int num2 = Mathf.Max(unsnapVerticalBand, ScaledProbeRadiusI());
				if (Mathf.Abs(py - (float)_lastSnapTopY) >= (float)num2)
				{
					_recentUnsnap = false;
				}
			}
		}
		if (snappedHWND != IntPtr.Zero)
		{
			bool flag2 = false;
			for (int i = 0; i < cachedWindows.Count; i++)
			{
				WindowEntry win = cachedWindows[i];
				if (!(win.hwnd != snappedHWND) && (IsWindowMaximized(win.hwnd) || IsWindowFullscreen(win)))
				{
					ClearSnapAndHide();
					flag2 = true;
					break;
				}
			}
			if (!flag2 && (IsIconic(snappedHWND) || IsCloaked(snappedHWND)))
			{
				ClearSnapAndHide();
			}
		}
		if (controller.isDragging)
		{
			if (snappedHWND == IntPtr.Zero)
			{
				if (_canSitHold && DraggedPastSnapThreshold())
				{
					TrySnap();
				}
			}
			else if (!IsStillNearSnappedWindow())
			{
				SetGuardZoneFromCurrent();
				ClearSnapAndHide(fromUnsnap: true);
			}
			else
			{
				FollowSnapped(dragging: true);
			}
		}
		else if (!controller.isDragging && snappedHWND != IntPtr.Zero)
		{
			FollowSnapped(dragging: false);
		}
		if (animator.GetBool("isBigScreenAlarm"))
		{
			if (flag)
			{
				animator.SetBool("isWindowSit", value: false);
			}
			ClearSnapAndHide();
		}
		if (snappedHWND != IntPtr.Zero && _postSettleRecalib)
		{
			if (_postSettleFrames > 0)
			{
				_postSettleFrames--;
			}
			else
			{
				if (GetWindowRect(snappedHWND, out var lpRect))
				{
					CalibrateSeatAnchorToDesktopY((float)lpRect.Top + seatOffsetPx);
					if (ComputeSeatDesktop(out var px2, out px))
					{
						float num3 = Mathf.Max(1, lpRect.Right - lpRect.Left);
						snapFraction = Mathf.Clamp01((px2 - (float)lpRect.Left) / num3);
					}
					_snapSmoothingActive = enableSnapSmoothing;
					_snapVelX = (_snapVelY = 0f);
					_havePrevSnapRect = false;
					PinToTarget(lpRect);
				}
				_postSettleRecalib = false;
			}
		}
		wasDragging = controller.isDragging;
	}

	private void LateUpdate()
	{
		UpdateOccluderQuadsFrameSync();
	}

	private bool DraggedPastSnapThreshold()
	{
		if (!GetCursorPos(out var point))
		{
			return true;
		}
		if (Mathf.Abs(point.x - _dragStartCursorX) < minDragPixelsToSnap)
		{
			return Mathf.Abs(point.y - _dragStartCursorY) >= minDragPixelsToSnap;
		}
		return true;
	}

	private void SetGuardZoneFromCurrent()
	{
		if (useGuardZone && ComputeZoneDesktop(out var px, out var py))
		{
			_guardCenterDesktop = new Vector2(px, py);
			_guardZoneActive = true;
			float num = ScaledGuardRadiusF();
			_guardRadiusSq = num * num;
		}
	}

	private float ScaleFactor()
	{
		if (!(boneHips != null))
		{
			return Mathf.Max(0.0001f, base.transform.lossyScale.magnitude);
		}
		return boneHips.lossyScale.magnitude;
	}

	private int ScaledProbeRadiusI()
	{
		return Mathf.Max(1, Mathf.RoundToInt(probeRadiusPx * ScaleFactor()));
	}

	private int ScaledGuardRadiusI()
	{
		return Mathf.Max(1, Mathf.RoundToInt(probeGuardPx * ScaleFactor()));
	}

	private float ScaledProbeRadiusF()
	{
		return probeRadiusPx * ScaleFactor();
	}

	private float ScaledGuardRadiusF()
	{
		return probeGuardPx * ScaleFactor();
	}

	private Vector3 GetHipWorld()
	{
		if (!(boneHips != null))
		{
			return base.transform.position;
		}
		return boneHips.position;
	}

	private bool ComputeZoneDesktop(out float px, out float py)
	{
		return ComputeDesktopFromWorld(GetProbeWorld(), out px, out py);
	}

	private bool ComputeSeatDesktop(out float px, out float py)
	{
		return ComputeDesktopFromWorld(GetSeatWorldCurrent(), out px, out py);
	}

	private bool ComputeDesktopFromWorld(Vector3 wp, out float px, out float py)
	{
		px = (py = 0f);
		if (targetCamera == null)
		{
			return false;
		}
		if (!GetUnityClientRect(out var r))
		{
			return false;
		}
		_haveUnityCli = true;
		_lastUnityCli = r;
		Vector3 vector = targetCamera.WorldToScreenPoint(wp);
		if (vector.z < 0.01f)
		{
			return false;
		}
		float num = Mathf.Max(1f, r.Right - r.Left);
		float num2 = Mathf.Max(1f, r.Bottom - r.Top);
		px = (float)r.Left + Mathf.Clamp(vector.x, 0f, targetCamera.pixelWidth) * (num / (float)Mathf.Max(1, targetCamera.pixelWidth));
		py = (float)r.Top + ((float)targetCamera.pixelHeight - Mathf.Clamp(vector.y, 0f, targetCamera.pixelHeight)) * (num2 / (float)Mathf.Max(1, targetCamera.pixelHeight));
		return true;
	}

	private void CacheRigRefs()
	{
		if (animator != null && animator.isHuman)
		{
			boneHips = animator.GetBoneTransform(HumanBodyBones.Hips);
			boneLUL = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
			boneRUL = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
			boneLFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
			boneRFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
			boneHead = animator.GetBoneTransform(HumanBodyBones.Head);
		}
		if (!_skinnedCached)
		{
			skinned = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			_skinnedCached = true;
		}
	}

	private bool IsEffectivelyTransparentWindow(IntPtr hWnd, StringBuilder cls)
	{
		long num = GetWindowLongPtr(hWnd, -20).ToInt64();
		if ((num & 0x80000) == 0L)
		{
			return false;
		}
		if (ignoreLayeredClickThrough && (num & 0x20) != 0L)
		{
			return true;
		}
		if (ignoreLayeredToolOrNoActivate && ((num & 0x80) != 0L || (num & 0x8000000) != 0L))
		{
			return true;
		}
		if (GetLayeredWindowAttributes(hWnd, out var _, out var pbAlpha, out var pdwFlags))
		{
			if (ignoreLayeredWithColorKey && (pdwFlags & 1) != 0)
			{
				return true;
			}
			if ((pdwFlags & 2) != 0 && pbAlpha <= layeredAlphaIgnoreBelow)
			{
				return true;
			}
		}
		long num2 = GetWindowLongPtr(hWnd, -16).ToInt64();
		int windowTextLength = GetWindowTextLength(hWnd);
		if ((num2 & 0xC00000) == 0L && windowTextLength <= 1)
		{
			return true;
		}
		if ((num2 & 0xC00000) == 0L && (SBEq(cls, "UnityWndClass") || SBEq(cls, "UnityGUIView")))
		{
			return true;
		}
		return false;
	}

	private bool IsSameProcessWindow(IntPtr hWnd)
	{
		GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
		return lpdwProcessId == _currentPid;
	}

	private void ClearSnapAndHide(bool fromUnsnap = false)
	{
		_havePrevSnapRect = false;
		_snapSmoothingActive = false;
		_snapVelX = (_snapVelY = 0f);
		if (controller != null && controller.isDragging)
		{
			_recentUnsnap = true;
		}
		if (fromUnsnap)
		{
			_unsnapCooldownUntil = Time.unscaledTime + Mathf.Max(0f, unsnapCooldownSeconds);
		}
		snappedHWND = IntPtr.Zero;
		seatCalibrated = false;
		if (animator != null)
		{
			animator.SetBool("isWindowSit", value: false);
			animator.SetBool("isTaskbarSit", value: false);
		}
		SetTopMost(!(SaveLoadHandler.Instance != null) || SaveLoadHandler.Instance.data.isTopmost);
		SetTargetQuadActive(on: false);
		SetOtherQuadsActive(0);
		_guard = (_latch = 0);
		activeOccluders.Clear();
	}

	private void UpdateCachedWindows()
	{
		cachedWindows.Clear();
		EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
		{
			if (hWnd == unityHWND || !IsWindowVisible(hWnd) || !GetWindowRect(hWnd, out var lpRect))
			{
				return true;
			}
			classNameBuffer.Clear();
			GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);
			if (IsSameProcessWindow(hWnd) || IsEffectivelyTransparentWindow(hWnd, classNameBuffer))
			{
				return true;
			}
			if (SBEq(classNameBuffer, "Shell_TrayWnd") || SBEq(classNameBuffer, "Shell_SecondaryTrayWnd"))
			{
				cachedWindows.Add(new WindowEntry
				{
					hwnd = hWnd,
					rect = lpRect,
					isTaskbar = true
				});
				return true;
			}
			if (IsLikelyUniWindowMascot(hWnd, classNameBuffer) || !IsSitEligibleWindow(hWnd, lpRect, classNameBuffer))
			{
				return true;
			}
			cachedWindows.Add(new WindowEntry
			{
				hwnd = hWnd,
				rect = lpRect,
				isTaskbar = false
			});
			return true;
		}, IntPtr.Zero);
	}

	private void RebuildActiveOccluders()
	{
		activeOccluders.Clear();
		for (int i = 0; i < cachedWindows.Count; i++)
		{
			if (activeOccluders.Count >= maxOtherQuads)
			{
				break;
			}
			WindowEntry item = cachedWindows[i];
			if (!(item.hwnd == unityHWND) && !(item.hwnd == snappedHWND) && !IsSameProcessWindow(item.hwnd))
			{
				classNameBuffer.Clear();
				GetClassName(item.hwnd, classNameBuffer, classNameBuffer.Capacity);
				if (!IsEffectivelyTransparentWindow(item.hwnd, classNameBuffer) && !IsLikelyUniWindowMascot(item.hwnd, classNameBuffer) && (item.isTaskbar || IsAboveInZOrder(item.hwnd, snappedHWND)))
				{
					activeOccluders.Add(item);
				}
			}
		}
	}

	private bool IsSitEligibleWindow(IntPtr hWnd, RECT r, StringBuilder cls)
	{
		if (GetParent(hWnd) != IntPtr.Zero || GetAncestor(hWnd, 2u) != hWnd || IsIconic(hWnd) || GetWindowTextLength(hWnd) == 0 || IsCloaked(hWnd))
		{
			return false;
		}
		int num = r.Right - r.Left;
		int num2 = r.Bottom - r.Top;
		if (num < 200 || num2 < 60)
		{
			return false;
		}
		if (SBEq(cls, "Progman") || SBEq(cls, "WorkerW") || SBEq(cls, "DV2ControlHost") || SBEq(cls, "MsgrIMEWindowClass"))
		{
			return false;
		}
		if (SBStartsWith(cls, "#") || SBContains(cls, "Desktop"))
		{
			return false;
		}
		return true;
	}

	private bool IsCloaked(IntPtr hWnd)
	{
		int pvAttribute = 0;
		DwmGetWindowAttribute(hWnd, 14, out pvAttribute, 4);
		return pvAttribute != 0;
	}

	private void TrySnap()
	{
		if (Time.unscaledTime < _unsnapCooldownUntil || IsSitBlocked())
		{
			return;
		}
		if (useGuardZone && _guardZoneActive && ComputeZoneDesktop(out var px, out var py))
		{
			float num = px - _guardCenterDesktop.x;
			float num2 = py - _guardCenterDesktop.y;
			if (num * num + num2 * num2 < _guardRadiusSq)
			{
				return;
			}
			_guardZoneActive = false;
		}
		if (!ComputeZoneDesktop(out var px2, out var py2))
		{
			return;
		}
		if (_recentUnsnap)
		{
			int num3 = Mathf.Max(unsnapVerticalBand, ScaledProbeRadiusI());
			if (Mathf.Abs(py2 - (float)_lastSnapTopY) < (float)num3)
			{
				return;
			}
		}
		float num4 = ScaledProbeRadiusI();
		int candidate = -1;
		for (int i = 0; i < cachedWindows.Count; i++)
		{
			WindowEntry windowEntry = cachedWindows[i];
			if (windowEntry.hwnd == unityHWND)
			{
				continue;
			}
			int left = windowEntry.rect.Left;
			int right = windowEntry.rect.Right;
			int top = windowEntry.rect.Top;
			float distance = Mathf.Abs(py2 - top);
			if (!(px2 >= (float)left) || !(px2 <= (float)right) || distance > num4 || IsWindowMaximized(windowEntry.hwnd) ||
				IsWindowFullscreen(windowEntry) || IsSameProcessWindow(windowEntry.hwnd) ||
				IsOccludedByHigherWindowsAtPoint(windowEntry.hwnd, Mathf.RoundToInt(px2), top))
			{
				continue;
			}
			classNameBuffer.Clear();
			GetClassName(windowEntry.hwnd, classNameBuffer, classNameBuffer.Capacity);
			if (!IsEffectivelyTransparentWindow(windowEntry.hwnd, classNameBuffer))
			{
				candidate = i;
				break;
			}
		}
		if (candidate >= 0)
		{
			WindowEntry windowEntry = cachedWindows[candidate];
			int left = windowEntry.rect.Left;
			int right = windowEntry.rect.Right;
			int top = windowEntry.rect.Top;
			lastDesktopPosition = GetUnityWindowPosition();
			snappedHWND = windowEntry.hwnd;
			_guardZoneActive = false;
			animator.SetBool("isWindowSit", value: true);
			animator.SetBool("isTaskbarSit", windowEntry.isTaskbar);
			animator.Update(0f);
			CalibrateSeatAnchorToDesktopY((float)top + seatOffsetPx);
			_postSettleFrames = 1;
			_postSettleRecalib = true;
			if (ComputeSeatDesktop(out var px3, out var _))
			{
				float num5 = Mathf.Max(1, right - left);
				snapFraction = Mathf.Clamp01((px3 - (float)left) / num5);
			}
			_lastSnapTopY = top;
			_recentUnsnap = false;
			SetTopMost(en: true);
			if (GetCursorPos(out var point))
			{
				_snapCursorY = point.y;
			}
			_guard = Mathf.Max(1, snapGuardFrames);
			_latch = Mathf.Max(1, snapLatchFrames);
			_snapSmoothingActive = enableSnapSmoothing;
			_snapVelX = (_snapVelY = 0f);
			_havePrevSnapRect = false;
			RebuildActiveOccluders();
			UpdateOccluderQuadsFrameSync();
			if (GetWindowRect(windowEntry.hwnd, out var lpRect))
			{
				PinToTarget(lpRect);
			}
			else
			{
				PinToTarget(windowEntry.rect);
			}
		}
	}

	private void CancelSnapSmoothingIfTargetMoved(RECT tr)
	{
		if (!_havePrevSnapRect)
		{
			_prevSnapRect = tr;
			_havePrevSnapRect = true;
			return;
		}
		if (tr.Left != _prevSnapRect.Left || tr.Top != _prevSnapRect.Top || tr.Right != _prevSnapRect.Right || tr.Bottom != _prevSnapRect.Bottom)
		{
			_snapSmoothingActive = false;
			_snapVelX = (_snapVelY = 0f);
		}
		_prevSnapRect = tr;
	}

	private bool CalibrateSeatAnchorToDesktopY(float targetDesktopY)
	{
		if (targetCamera == null || !GetUnityClientRect(out var r))
		{
			return false;
		}
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		float num = 1f / 0f;
		float num2 = -1f / 0f;
		if (animator != null && animator.isHuman)
		{
			if (boneHead != null)
			{
				float y = boneHead.position.y;
				if (y < num)
				{
					num = y;
				}
				if (y > num2)
				{
					num2 = y;
				}
			}
			if (boneHips != null)
			{
				float y2 = boneHips.position.y;
				if (y2 < num)
				{
					num = y2;
				}
				if (y2 > num2)
				{
					num2 = y2;
				}
			}
			if (boneLUL != null)
			{
				float y3 = boneLUL.position.y;
				if (y3 < num)
				{
					num = y3;
				}
				if (y3 > num2)
				{
					num2 = y3;
				}
			}
			if (boneRUL != null)
			{
				float y4 = boneRUL.position.y;
				if (y4 < num)
				{
					num = y4;
				}
				if (y4 > num2)
				{
					num2 = y4;
				}
			}
			if (boneLFoot != null)
			{
				float y5 = boneLFoot.position.y;
				if (y5 < num)
				{
					num = y5;
				}
				if (y5 > num2)
				{
					num2 = y5;
				}
			}
			if (boneRFoot != null)
			{
				float y6 = boneRFoot.position.y;
				if (y6 < num)
				{
					num = y6;
				}
				if (y6 > num2)
				{
					num2 = y6;
				}
			}
		}
		float num4;
		float num5;
		if (float.IsInfinity(num) || float.IsInfinity(num2))
		{
			Bounds bounds = WorldBoundsToRootLocal(GetCombinedWorldBounds());
			float num3 = Mathf.Max(0.0001f, bounds.size.y);
			num4 = bounds.min.y - 0.5f * num3 - 0.25f;
			num5 = bounds.max.y + 0.5f * num3 + 0.25f;
			boundsMinSnapLocal = bounds.min;
			boundsSizeSnapLocal = bounds.size;
		}
		else
		{
			Vector3 vector = worldToLocalMatrix.MultiplyPoint3x4(new Vector3(base.transform.position.x, num, base.transform.position.z));
			Vector3 vector2 = worldToLocalMatrix.MultiplyPoint3x4(new Vector3(base.transform.position.x, num2, base.transform.position.z));
			float num6 = Mathf.Min(vector.y, vector2.y);
			float num7 = Mathf.Max(vector.y, vector2.y);
			float num8 = Mathf.Max(0.05f, (num7 - num6) * 0.2f);
			num4 = num6 - num8;
			num5 = num7 + num8;
			Bounds combinedWorldBounds = GetCombinedWorldBounds();
			Bounds bounds2 = WorldBoundsToRootLocal(combinedWorldBounds);
			boundsMinSnapLocal = bounds2.min;
			boundsSizeSnapLocal = bounds2.size;
		}
		Vector3 vector3 = base.transform.worldToLocalMatrix.MultiplyPoint3x4(SeatWorldGuess());
		float num9 = vector3.y;
		float f = 3.4028235E+38f;
		for (int i = 0; i < 20; i++)
		{
			float num10 = 0.5f * (num4 + num5);
			Vector3 point = new Vector3(vector3.x, num10, vector3.z);
			Vector3 vector4 = targetCamera.WorldToScreenPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(point));
			if (vector4.z < 0.01f)
			{
				break;
			}
			float num11 = Mathf.Max(1f, r.Bottom - r.Top);
			float num12 = (float)r.Top + ((float)targetCamera.pixelHeight - Mathf.Clamp(vector4.y, 0f, targetCamera.pixelHeight)) * (num11 / (float)Mathf.Max(1, targetCamera.pixelHeight)) - targetDesktopY;
			if (Mathf.Abs(num12) < Mathf.Abs(f))
			{
				f = num12;
				num9 = num10;
			}
			if (num12 > 0f)
			{
				num5 = num10;
			}
			else
			{
				num4 = num10;
			}
		}
		seatLocalAtSnap = new Vector3(vector3.x, num9, vector3.z);
		float num13 = Mathf.Max(0.0001f, boundsSizeSnapLocal.y);
		seatNormY = Mathf.Clamp01((num9 - boundsMinSnapLocal.y) / num13);
		seatCalibrated = true;
		return true;
	}

	private void FollowSnapped(bool dragging)
	{
		if (snappedHWND == IntPtr.Zero || !GetWindowRect(snappedHWND, out var lpRect))
		{
			ClearSnapAndHide();
			return;
		}
		CancelSnapSmoothingIfTargetMoved(lpRect);
		if (dragging && ComputeSeatDesktop(out var px, out var _))
		{
			float num = Mathf.Max(1, lpRect.Right - lpRect.Left);
			snapFraction = Mathf.Clamp01((px - (float)lpRect.Left) / num);
		}
		PinToTarget(lpRect);
		SetTopMost(en: true);
	}

	private void PinToTarget(RECT r)
	{
		if (!ComputeSeatDesktop(out var px, out var py))
		{
			return;
		}
		int left = r.Left;
		int right = r.Right;
		int top = r.Top;
		float num = (float)left + snapFraction * (float)Mathf.Max(1, right - left);
		float num2 = (float)top + seatOffsetPx;
		int num3 = Mathf.RoundToInt(num - px);
		int num4 = Mathf.RoundToInt(num2 - py);
		GetWindowRect(unityHWND, out var lpRect);
		int nWidth = lpRect.Right - lpRect.Left;
		int nHeight = lpRect.Bottom - lpRect.Top;
		int num5 = lpRect.Left + num3;
		int num6 = lpRect.Top + num4;
		if (!_snapSmoothingActive || !enableSnapSmoothing)
		{
			if (num3 != 0 || num4 != 0)
			{
				MoveWindow(unityHWND, num5, num6, nWidth, nHeight, bRepaint: true);
			}
			return;
		}
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		float f = Mathf.SmoothDamp(lpRect.Left, num5, ref _snapVelX, snapSmoothingTime, snapSmoothingMaxSpeed, unscaledDeltaTime);
		float num7 = Mathf.SmoothDamp(lpRect.Top, num6, ref _snapVelY, snapSmoothingTime, snapSmoothingMaxSpeed, unscaledDeltaTime);
		if (controller != null && controller.isDragging)
		{
			float num8 = py + (num7 - (float)lpRect.Top) - num2;
			if (num8 > 0f)
			{
				float a = snapSmoothingMaxSpeed * unscaledDeltaTime;
				float b = Mathf.Max(0f, num8 - 1f);
				num7 -= Mathf.Min(a, b);
			}
		}
		int num9 = Mathf.RoundToInt(f);
		int num10 = Mathf.RoundToInt(num7);
		if (Mathf.Abs(num5 - num9) <= 1 && Mathf.Abs(num6 - num10) <= 1)
		{
			num9 = num5;
			num10 = num6;
			_snapSmoothingActive = false;
			_snapVelX = (_snapVelY = 0f);
		}
		if (num9 != lpRect.Left || num10 != lpRect.Top)
		{
			MoveWindow(unityHWND, num9, num10, nWidth, nHeight, bRepaint: true);
		}
	}

	private bool IsStillNearSnappedWindow()
	{
		if (_latch > 0)
		{
			_latch--;
			return true;
		}
		if (_guard > 0)
		{
			_guard--;
			return true;
		}
		for (int i = 0; i < cachedWindows.Count; i++)
		{
			WindowEntry windowEntry = cachedWindows[i];
			if (windowEntry.hwnd != snappedHWND)
			{
				continue;
			}
			if (!ComputeZoneDesktop(out var px, out var py))
			{
				return true;
			}
			int left = windowEntry.rect.Left;
			int right = windowEntry.rect.Right;
			int top = windowEntry.rect.Top;
			bool num = px >= (float)left && px <= (float)right;
			bool flag = Mathf.Abs(py - (float)top) <= (float)Mathf.Max(unsnapVerticalBand, ScaledProbeRadiusI());
			if (!num || !flag)
			{
				return false;
			}
			if (controller.isDragging && animator.GetBool("isWindowSit"))
			{
				if (!GetCursorPos(out var point))
				{
					return true;
				}
				int num2 = Mathf.Max(unsnapVerticalBand, ScaledProbeRadiusI());
				if (Mathf.Abs(point.y - _snapCursorY) > num2)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	private bool IsOccludedByHigherWindowsAtPoint(IntPtr hwnd, int x, int y)
	{
		IntPtr window = GetWindow(hwnd, 3u);
		while (window != IntPtr.Zero)
		{
			if (window == unityHWND || IsSameProcessWindow(window))
			{
				window = GetWindow(window, 3u);
				continue;
			}
			if (!IsWindowVisible(window) || IsCloaked(window) || !GetWindowRect(window, out var lpRect))
			{
				window = GetWindow(window, 3u);
				continue;
			}
			if (x < lpRect.Left || x > lpRect.Right || y < lpRect.Top || y > lpRect.Bottom)
			{
				window = GetWindow(window, 3u);
				continue;
			}
			classNameBuffer.Clear();
			GetClassName(window, classNameBuffer, classNameBuffer.Capacity);
			if (IsEffectivelyTransparentWindow(window, classNameBuffer) || IsLikelyUniWindowMascot(window, classNameBuffer))
			{
				window = GetWindow(window, 3u);
				continue;
			}
			long num = GetWindowLongPtr(window, -20).ToInt64();
			if ((num & 0x20) != 0L)
			{
				window = GetWindow(window, 3u);
				continue;
			}
			if ((num & 0x80000) != 0L && GetLayeredWindowAttributes(window, out var _, out var pbAlpha, out var pdwFlags) && (pdwFlags & 2) != 0 && pbAlpha <= 8)
			{
				window = GetWindow(window, 3u);
				continue;
			}
			return true;
		}
		return false;
	}

	private Vector3 GetSeatWorldCurrent()
	{
		if (!seatCalibrated)
		{
			return GetHipWorld();
		}
		float num = Mathf.Clamp(seatNormY + windowSitYOffset, -0.5f, 1.5f);
		float y = boundsMinSnapLocal.y + num * boundsSizeSnapLocal.y;
		Vector3 point = new Vector3(seatLocalAtSnap.x, y, seatLocalAtSnap.z);
		return base.transform.localToWorldMatrix.MultiplyPoint3x4(point);
	}

	private Vector3 SeatWorldGuess()
	{
		if (animator != null && animator.isHuman)
		{
			Vector3 vector = ((boneHips != null) ? boneHips.position : base.transform.position);
			Vector3 obj = ((boneLUL != null && boneRUL != null) ? ((boneLUL.position + boneRUL.position) * 0.5f) : vector);
			float num = ((boneHead != null) ? boneHead.position.y : (vector.y + 0.5f));
			float num2 = vector.y;
			if (boneLFoot != null)
			{
				num2 = boneLFoot.position.y;
			}
			if (boneRFoot != null)
			{
				num2 = Mathf.Min(num2, boneRFoot.position.y);
			}
			float num3 = Mathf.Max(0.1f, num - num2);
			float num4 = Mathf.Clamp(num3 * 0.12f, 0.01f, num3 * 0.5f);
			return obj + Vector3.down * num4;
		}
		Bounds combinedWorldBounds = GetCombinedWorldBounds();
		return new Vector3(combinedWorldBounds.center.x, Mathf.Lerp(combinedWorldBounds.min.y, combinedWorldBounds.center.y, 0.2f), combinedWorldBounds.center.z);
	}

	private Bounds GetCombinedWorldBounds()
	{
		Bounds result = new Bounds(base.transform.position, Vector3.zero);
		bool flag = false;
		if (!_skinnedCached || skinned == null || skinned.Length == 0)
		{
			skinned = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			_skinnedCached = true;
		}
		if (skinned != null)
		{
			for (int i = 0; i < skinned.Length; i++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = skinned[i];
				if (!(skinnedMeshRenderer == null) && skinnedMeshRenderer.enabled)
				{
					if (!flag)
					{
						result = skinnedMeshRenderer.bounds;
						flag = true;
					}
					else
					{
						result.Encapsulate(skinnedMeshRenderer.bounds);
					}
				}
			}
		}
		if (!flag)
		{
			Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (!(renderer == null) && renderer.enabled)
				{
					if (!flag)
					{
						result = renderer.bounds;
						flag = true;
					}
					else
					{
						result.Encapsulate(renderer.bounds);
					}
				}
			}
		}
		if (!flag)
		{
			result = new Bounds(base.transform.position, Vector3.one * 0.5f);
		}
		return result;
	}

	private Bounds WorldBoundsToRootLocal(Bounds wb)
	{
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		Vector3 min = wb.min;
		Vector3 max = wb.max;
		Vector3[] array = new Vector3[8]
		{
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)),
			worldToLocalMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z))
		};
		Vector3 vector = array[0];
		Vector3 vector2 = array[0];
		for (int i = 1; i < 8; i++)
		{
			vector = Vector3.Min(vector, array[i]);
			vector2 = Vector3.Max(vector2, array[i]);
		}
		return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
	}

	private void UpdateOccluderQuadsFrameSync()
	{
		if (_occluderSharedMat == null || targetCamera == null || snappedHWND == IntPtr.Zero)
		{
			SetTargetQuadActive(on: false);
			SetOtherQuadsActive(0);
			return;
		}
		if (!_haveUnityCli && !GetUnityClientRect(out _lastUnityCli))
		{
			SetTargetQuadActive(on: false);
			SetOtherQuadsActive(0);
			return;
		}
		RECT lastUnityCli = _lastUnityCli;
		Rect rect = new Rect(lastUnityCli.Left, lastUnityCli.Top, lastUnityCli.Right - lastUnityCli.Left, lastUnityCli.Bottom - lastUnityCli.Top);
		if (snappedHWND != unityHWND && GetWindowRect(snappedHWND, out var lpRect))
		{
			Rect desktopRect = Intersect(new Rect(lpRect.Left, lpRect.Top, lpRect.Right - lpRect.Left, lpRect.Bottom - lpRect.Top), rect);
			if (desktopRect.width > 0f && desktopRect.height > 0f)
			{
				EnsureTargetQuad();
				float zOffset = (autoScaleTargetZ ? GetAutoTargetZ() : targetQuadZOffset);
				UpdateQuadLocalFast(desktopRect, rect, zOffset, targetMesh, targetQuadGO, verts4);
				SetTargetQuadActive(on: true);
			}
			else
			{
				SetTargetQuadActive(on: false);
			}
		}
		else
		{
			SetTargetQuadActive(on: false);
		}
		int num = 0;
		for (int i = 0; i < activeOccluders.Count; i++)
		{
			if (num >= maxOtherQuads)
			{
				break;
			}
			if (GetWindowRect(activeOccluders[i].hwnd, out var lpRect2))
			{
				Rect desktopRect2 = Intersect(new Rect(lpRect2.Left, lpRect2.Top, lpRect2.Right - lpRect2.Left, lpRect2.Bottom - lpRect2.Top), rect);
				if (!(desktopRect2.width <= 0f) && !(desktopRect2.height <= 0f))
				{
					EnsureOtherQuad(num);
					UpdateQuadLocalFast(desktopRect2, rect, othersQuadZOffset, otherMeshes[num], otherQuadGOs[num], verts4Other);
					num++;
				}
			}
		}
		SetOtherQuadsActive(num);
	}

	private float GetAutoTargetZ()
	{
		float num = Mathf.Max(0.0001f, base.transform.lossyScale.y);
		return Mathf.Clamp(targetZBase + (num - targetZRefScale) * targetZSensitivity, targetZMin, targetZMax);
	}

	private void EnsureOccluderRoot()
	{
		if (!(occluderRoot != null))
		{
			GameObject gameObject = new GameObject("OccluderRoot");
			gameObject.layer = ((targetCamera != null) ? targetCamera.gameObject.layer : 0);
			gameObject.transform.SetParent((targetCamera != null) ? targetCamera.transform : null, worldPositionStays: false);
			occluderRoot = gameObject.transform;
		}
	}

	private void EnsureTargetQuad()
	{
		if (!(targetQuadGO != null))
		{
			targetQuadGO = new GameObject("TargetWindowQuad");
			targetQuadGO.layer = targetCamera.gameObject.layer;
			targetQuadGO.transform.SetParent(occluderRoot, worldPositionStays: false);
			MeshFilter meshFilter = targetQuadGO.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = targetQuadGO.AddComponent<MeshRenderer>();
			targetMesh = new Mesh();
			targetMesh.MarkDynamic();
			meshFilter.sharedMesh = targetMesh;
			meshRenderer.sharedMaterial = _occluderSharedMat;
			targetMesh.vertices = verts4;
			targetMesh.triangles = TRI;
			targetMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
			targetQuadGO.SetActive(value: false);
		}
	}

	private void EnsureOtherQuad(int index)
	{
		while (otherQuadGOs.Count <= index)
		{
			GameObject gameObject = new GameObject("OtherWindowQuad_" + otherQuadGOs.Count);
			gameObject.layer = targetCamera.gameObject.layer;
			gameObject.transform.SetParent(occluderRoot, worldPositionStays: false);
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			Mesh mesh = new Mesh();
			mesh.MarkDynamic();
			meshFilter.sharedMesh = mesh;
			meshRenderer.sharedMaterial = _occluderSharedMat;
			mesh.vertices = verts4Other;
			mesh.triangles = TRI;
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
			otherQuadGOs.Add(gameObject);
			otherMeshes.Add(mesh);
			gameObject.SetActive(value: false);
		}
	}

	private void SetTargetQuadActive(bool on)
	{
		if (targetQuadGO != null && targetQuadGO.activeSelf != on)
		{
			targetQuadGO.SetActive(on);
		}
	}

	private void SetOtherQuadsActive(int activeCount)
	{
		for (int i = 0; i < otherQuadGOs.Count; i++)
		{
			bool flag = i < activeCount;
			if (otherQuadGOs[i].activeSelf != flag)
			{
				otherQuadGOs[i].SetActive(flag);
			}
		}
	}

	private void CleanupOccluderArtifacts()
	{
		if ((bool)targetQuadGO)
		{
			UnityEngine.Object.Destroy(targetQuadGO);
			targetQuadGO = null;
			targetMesh = null;
		}
		for (int i = 0; i < otherQuadGOs.Count; i++)
		{
			if ((bool)otherQuadGOs[i])
			{
				UnityEngine.Object.Destroy(otherQuadGOs[i]);
			}
		}
		otherQuadGOs.Clear();
		otherMeshes.Clear();
		activeOccluders.Clear();
		_haveUnityCli = false;
		if ((bool)_occluderSharedMat)
		{
			UnityEngine.Object.Destroy(_occluderSharedMat);
			_occluderSharedMat = null;
		}
	}

	private bool IsLikelyUniWindowMascot(IntPtr hWnd, StringBuilder cls)
	{
		long num = GetWindowLongPtr(hWnd, -20).ToInt64();
		long num2 = GetWindowLongPtr(hWnd, -16).ToInt64();
		bool flag = (num & 0x80000) != 0;
		bool flag2 = (num & 0x80) != 0L || (num & 0x8000000) != 0;
		bool flag3 = (num & 0x20) != 0;
		bool flag4 = false;
		if (flag && GetLayeredWindowAttributes(hWnd, out var _, out var pbAlpha, out var pdwFlags))
		{
			flag4 = ((pdwFlags & 2) != 0 && pbAlpha < 255) || (pdwFlags & 1) != 0;
		}
		int windowTextLength = GetWindowTextLength(hWnd);
		if (flag && (flag2 || flag3 || flag4) && (num2 & 0xC00000) == 0L && windowTextLength <= 1)
		{
			return true;
		}
		if (flag && (flag2 || flag3 || flag4) && SBEq(cls, "UnityWndClass"))
		{
			return true;
		}
		return false;
	}

	private bool IsAboveInZOrder(IntPtr a, IntPtr b)
	{
		if (a == b || a == IntPtr.Zero || b == IntPtr.Zero)
		{
			return false;
		}
		IntPtr intPtr = b;
		for (int i = 0; i < 2048; i++)
		{
			if (!(intPtr != IntPtr.Zero))
			{
				break;
			}
			intPtr = GetWindow(intPtr, 3u);
			if (intPtr == a)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateQuadLocalFast(Rect desktopRect, Rect unityDesktopRect, float zOffset, Mesh mesh, GameObject go, Vector3[] buffer)
	{
		float num = Mathf.Max(1f, unityDesktopRect.width);
		float num2 = Mathf.Max(1f, unityDesktopRect.height);
		float num3 = Mathf.Max(1, targetCamera.pixelWidth);
		float num4 = Mathf.Max(1, targetCamera.pixelHeight);
		float x = (desktopRect.xMin - unityDesktopRect.xMin) * (num3 / num);
		float x2 = (desktopRect.xMax - unityDesktopRect.xMin) * (num3 / num);
		float y = num4 - (desktopRect.yMax - unityDesktopRect.yMin) * (num4 / num2);
		float y2 = num4 - (desktopRect.yMin - unityDesktopRect.yMin) * (num4 / num2);
		float z = targetCamera.nearClipPlane + zOffset;
		Vector3 position = targetCamera.ScreenToWorldPoint(new Vector3(x, y, z));
		Vector3 position2 = targetCamera.ScreenToWorldPoint(new Vector3(x, y2, z));
		Vector3 position3 = targetCamera.ScreenToWorldPoint(new Vector3(x2, y2, z));
		Vector3 position4 = targetCamera.ScreenToWorldPoint(new Vector3(x2, y, z));
		buffer[0] = targetCamera.transform.InverseTransformPoint(position);
		buffer[1] = targetCamera.transform.InverseTransformPoint(position2);
		buffer[2] = targetCamera.transform.InverseTransformPoint(position3);
		buffer[3] = targetCamera.transform.InverseTransformPoint(position4);
		mesh.vertices = buffer;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
	}

	private static Rect Intersect(Rect a, Rect b)
	{
		float num = Mathf.Max(a.xMin, b.xMin);
		float num2 = Mathf.Max(a.yMin, b.yMin);
		float num3 = Mathf.Min(a.xMax, b.xMax);
		float num4 = Mathf.Max(Mathf.Min(a.yMax, b.yMax), num2);
		if (num3 <= num || num4 <= num2)
		{
			return new Rect(0f, 0f, 0f, 0f);
		}
		return new Rect(num, num2, num3 - num, num4 - num2);
	}

	private Vector2 GetUnityWindowPosition()
	{
		GetWindowRect(unityHWND, out var lpRect);
		return new Vector2(lpRect.Left, lpRect.Top);
	}

	private bool GetUnityClientRect(out RECT r)
	{
		r = default(RECT);
		if (!GetClientRect(unityHWND, out var lpRect))
		{
			return false;
		}
		POINT lpPoint = new POINT
		{
			X = 0,
			Y = 0
		};
		if (!ClientToScreen(unityHWND, ref lpPoint))
		{
			return false;
		}
		r.Left = lpPoint.X;
		r.Top = lpPoint.Y;
		r.Right = lpPoint.X + lpRect.Right;
		r.Bottom = lpPoint.Y + lpRect.Bottom;
		return true;
	}

	private void SetTopMost(bool en)
	{
		SetWindowPos(unityHWND, en ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, 19u);
	}

	private bool IsWindowMaximized(IntPtr hwnd)
	{
		WINDOWPLACEMENT lpwndpl = new WINDOWPLACEMENT
		{
			length = Marshal.SizeOf(typeof(WINDOWPLACEMENT))
		};
		if (GetWindowPlacement(hwnd, ref lpwndpl))
		{
			return lpwndpl.showCmd == 3;
		}
		return false;
	}

	private bool IsWindowFullscreen(WindowEntry win)
	{
		return WindowManager.Instance != null && WindowManager.Instance.IsWindowFullscreen(win.hwnd);
	}

	public void ForceExitWindowSitting()
	{
		ClearSnapAndHide();
	}

	private void OnDrawGizmos()
	{
		if (showProbeGizmo && !(targetCamera == null))
		{
			Vector3 probeWorld = GetProbeWorld();
			Vector3 vector = targetCamera.WorldToScreenPoint(probeWorld);
			if (!(vector.z <= 0f))
			{
				Vector3 vector2 = vector + new Vector3(ScaledProbeRadiusF(), 0f, 0f);
				Vector3 a = targetCamera.ScreenToWorldPoint(new Vector3(vector.x, vector.y, vector.z));
				Vector3 b = targetCamera.ScreenToWorldPoint(new Vector3(vector2.x, vector2.y, vector2.z));
				float radius = Vector3.Distance(a, b);
				Gizmos.color = probeGizmoColor;
				Gizmos.DrawWireSphere(probeWorld, radius);
				Vector3 vector3 = vector + new Vector3(ScaledGuardRadiusF(), 0f, 0f);
				Vector3 b2 = targetCamera.ScreenToWorldPoint(new Vector3(vector3.x, vector3.y, vector3.z));
				float radius2 = Vector3.Distance(a, b2);
				Gizmos.color = probeGuardGizmoColor;
				Gizmos.DrawWireSphere(probeWorld, radius2);
			}
		}
	}

	public void SetBaseOffset(float v)
	{
	}

	public void SetBaseScale(float v)
	{
	}

	public float GetBaseOffset()
	{
		return 0f;
	}

	public float GetBaseScale()
	{
		return 1f;
	}

	public float GetScaleCompPx()
	{
		return 0f;
	}

	private static bool SBEq(StringBuilder sb, string s)
	{
		if (sb.Length != s.Length)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (sb[i] != s[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool SBStartsWith(StringBuilder sb, string s)
	{
		if (sb.Length < s.Length)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (sb[i] != s[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool SBContains(StringBuilder sb, string s)
	{
		int length = sb.Length;
		int length2 = s.Length;
		if (length2 == 0)
		{
			return true;
		}
		for (int i = 0; i <= length - length2; i++)
		{
			int j;
			for (j = 0; j < length2 && sb[i + j] == s[j]; j++)
			{
			}
			if (j == length2)
			{
				return true;
			}
		}
		return false;
	}

	private struct CursorPoint { public int x, y; }

	private static bool GetCursorPos(out CursorPoint point)
	{
		point = default;
		if (WindowManager.Instance == null || !WindowManager.Instance.GetMousePosition(out var position)) return false;
		point.x = position.x;
		point.y = position.y;
		return true;
	}

	private static uint GetCurrentProcessId() => (uint)Process.GetCurrentProcess().Id;

	private static bool GetWindowPlacement(IntPtr window, ref WINDOWPLACEMENT placement)
	{
		var wm = WindowManager.Instance;
		if (wm == null || !GetWindowRect(window, out var rect)) return false;
		placement.rcNormalPosition = rect;
		placement.showCmd = wm.IsWindowMaximized(window) ? 3 : 1;
		return true;
	}

	private static bool IsIconic(IntPtr window) => !IsWindowVisible(window);

	private static int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size)
	{
		value = IsWindowVisible(window) ? 0 : 1;
		return 0;
	}

	private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
	{
		// Windows-only layered/style metadata has no Wayland counterpart.
		// Treat compositor-listed toplevels as normal windows; desktop layers
		// remain non-interactive supports and are excluded by their class.
		return nIndex == -16 ? new IntPtr(0xC00000) : IntPtr.Zero;
	}

	private static bool GetLayeredWindowAttributes(IntPtr window, out uint key, out byte alpha, out uint flags)
	{
		key = flags = 0;
		alpha = 255;
		return false;
	}

	private static IntPtr GetWindow(IntPtr window, uint command)
	{
		var wm = WindowManager.Instance;
		if (wm == null || command != 3) return IntPtr.Zero;
		var windows = wm.GetClientStackingList();
		int index = windows.IndexOf(window);
		return index >= 0 && index + 1 < windows.Count ? windows[index + 1] : IntPtr.Zero;
	}

	private static int GetClassName(IntPtr window, StringBuilder text, int capacity)
	{
		text.Clear();
		var wm = WindowManager.Instance;
		if (wm == null || capacity < 1) return 0;
		string name = wm.IsDock(window) ? "Shell_TrayWnd" :
			wm.IsDesktop(window) ? "WorkerW" : wm.GetClassName(window);
		if (name == null) return 0;
		text.Append(name, 0, Math.Min(name.Length, capacity - 1));
		return text.Length;
	}

	private static IntPtr GetAncestor(IntPtr window, uint flags) => window;

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { Left = bounds.xMin, Top = bounds.yMin, Right = bounds.xMax, Bottom = bounds.yMax };
		return true;
	}

	private static bool MoveWindow(IntPtr window, int x, int y, int width, int height, bool bRepaint)
	{
		var wm = WindowManager.Instance;
		if (wm == null || window != wm.UnityWindow) return false;
		wm.SetWindowPosition(x, y);
		return true;
	}

	private static bool IsWindowVisible(IntPtr window) => WindowManager.Instance != null &&
		WindowManager.Instance.IsWindowVisible(window);

	private static uint GetWindowThreadProcessId(IntPtr window, out uint pid)
	{
		int value = WindowManager.Instance != null ? WindowManager.Instance.GetWindowPid(window) : -1;
		pid = value > 0 ? (uint)value : 0;
		return pid;
	}

	private static bool EnumWindows(EnumWindowsProc callback, IntPtr data)
	{
		var wm = WindowManager.Instance;
		if (wm == null) return false;
		var windows = wm.GetClientStackingList();
		for (int i = windows.Count - 1; i >= 0; i--)
			if (!callback(windows[i], data)) break;
		return true;
	}

	private static bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags)
	{
		var wm = WindowManager.Instance;
		if (wm == null || window != wm.UnityWindow) return false;
		if ((flags & SWP_NOMOVE) == 0) wm.SetWindowPosition(x, y);
		if ((flags & SWP_NOSIZE) == 0) wm.SetWindowSize(width, height);
		if ((flags & 4) == 0 && (insertAfter == HWND_TOPMOST || insertAfter == HWND_NOTOPMOST))
			wm.SetTopmost(insertAfter == HWND_TOPMOST);
		return true;
	}

	private static IntPtr GetParent(IntPtr window) => IntPtr.Zero;

	private static int GetWindowTextLength(IntPtr window) => WindowManager.Instance?.GetClassName(window)?.Length ?? 0;

	private static bool GetClientRect(IntPtr window, out RECT rect)
	{
		if (!GetWindowRect(window, out rect)) return false;
		rect.Right -= rect.Left;
		rect.Bottom -= rect.Top;
		rect.Left = rect.Top = 0;
		return true;
	}

	private static bool ClientToScreen(IntPtr window, ref POINT point)
	{
		if (!GetWindowRect(window, out var rect)) return false;
		point.X += rect.Left;
		point.Y += rect.Top;
		return true;
	}
}
