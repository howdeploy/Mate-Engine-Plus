using System;
using UnityEngine;

public sealed class AvatarLocomotionController : MonoBehaviour
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private struct BlockingInfo
	{
		public int monitorLeft;

		public int monitorRight;

		public float effectiveMinXUnity;

		public float effectiveMaxXUnity;

		public int effectiveMinGlobalX;

		public int effectiveMaxGlobalX;

		public float edgeThresholdUnity;

		public int edgeThresholdGlobal;

		public int minWindowX;

		public int maxWindowX;
	}

	[Header("Enable")]
	public bool EnableLocomotion = true;

	[Header("Animator (optional, will auto-find)")]
	public Animator Animator;

	[Header("Debug")]
	public bool DebugTriggerWalkNow;

	[Header("Locomotion Timing")]
	[Range(0f, 60f)]
	public float Randomizer = 10f;

	[Range(10f, 4000f)]
	public float MinWalkCycle = 250f;

	[Range(10f, 4000f)]
	public float MaxWalkCycle = 550f;

	[Header("Window Movement")]
	[Range(0f, 10f)]
	public float WindowSpeed = 2f;

	[Header("Animator Wiring")]
	public string BaseLayerName = "Base Layer";

	public string BaseIdleStateName = "Idle";

	public string WalkLeftParam = "WalkLeft";

	public string WalkRightParam = "WalkRight";

	[Header("Optional")]
	public bool OnlyMoveWhenFocused;

	[Header("Avatar Bounds Based Blocking (Left/Right only)")]
	public bool UseAvatarBoundsBlocking = true;

	[Tooltip("Root transform whose child Renderers define the avatar bounds. If null, this GameObject is used.")]
	public Transform AvatarBoundsRoot;

	[Tooltip("Camera used to project avatar bounds into Unity pixels. If null, Camera.main is used.")]
	public Camera BoundsCamera;

	[Header("Blocking Box Fine Tuning (Unity pixels)")]
	[Min(0f)]
	public float BoundsInsetLeft;

	[Min(0f)]
	public float BoundsInsetRight;

	[Tooltip("If the avatar box is within this many Unity pixels to a screen edge, the next walk cycle is forced away from that edge.")]
	[Min(0f)]
	public float EdgeThresholdUnityPixels = 12f;

	[Header("Visual Debug (Game + Gizmos)")]
	public bool DrawBlockingDebug = true;

	[Range(0f, 1f)]
	public float DebugOverlayAlpha = 0.55f;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private const int SM_XVIRTUALSCREEN = 76;

	private const int SM_CXVIRTUALSCREEN = 78;

	private int _baseLayerIndex;

	private IntPtr _hwnd = IntPtr.Zero;

	private bool _walking;

	private int _dir;

	private float _remainingPixels;

	private float _nextDecisionTime;

	private float _pauseUntil;

	private float _nextAnimatorResolveTime;

	private Renderer[] _boundsRenderers;

	private float _nextBoundsResolveTime;

	private int _forcedNextDir;

	private bool _wasBaseIdle;

	private static readonly Vector3[] _boundsCorners = new Vector3[8];

	private static IntPtr GetForegroundWindow()
	{
		return Application.isFocused && WindowManager.Instance != null
			? WindowManager.Instance.UnityWindow : IntPtr.Zero;
	}

	private static int GetSystemMetrics(int index)
	{
		var wm = WindowManager.Instance;
		if (wm == null) return 0;
		int left = int.MaxValue, right = int.MinValue;
		foreach (RectInt monitor in wm.GetAllMonitors().Values)
		{
			left = Math.Min(left, monitor.xMin);
			right = Math.Max(right, monitor.xMax);
		}
		if (right <= left) return 0;
		return index == SM_XVIRTUALSCREEN ? left :
			index == SM_CXVIRTUALSCREEN ? right - left : 0;
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default(RECT);
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out RectInt geometry)) return false;
		rect = new RECT { Left = geometry.xMin, Top = geometry.yMin,
			Right = geometry.xMax, Bottom = geometry.yMax };
		return true;
	}

	private static bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y,
		int width, int height, uint flags)
	{
		var wm = WindowManager.Instance;
		if (wm == null || window != wm.UnityWindow || (flags & SWP_NOSIZE) == 0) return false;
		wm.SetWindowPosition(new Vector2Int(x, y));
		return true;
	}

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
		if (!GetWindowRect(window, out RECT rect)) return false;
		point.X += rect.Left;
		point.Y += rect.Top;
		return true;
	}

	private void OnEnable()
	{
		Application.runInBackground = true;
		ResolveAnimatorSmart(immediate: true);
		CacheWindowHandle();
		ResolveBoundsRenderersSmart(immediate: true);
		ScheduleNextDecision(immediate: true);
		_wasBaseIdle = true;
	}

	private void Update()
	{
		if (!EnableLocomotion || !WindowManager.AllowDesktopActivity)
		{
			StopWalking();
			return;
		}
		ResolveAnimatorSmart(immediate: false);
		ResolveBoundsRenderersSmart(immediate: false);
		if (Animator == null)
		{
			return;
		}
		if (_hwnd == IntPtr.Zero)
		{
			CacheWindowHandle();
		}
		if (_hwnd == IntPtr.Zero)
		{
			return;
		}
		if (OnlyMoveWhenFocused && GetForegroundWindow() != _hwnd)
		{
			if (_walking)
			{
				StopWalking();
			}
			return;
		}
		if (DebugTriggerWalkNow)
		{
			DebugTriggerWalkNow = false;
			ForceStartWalk();
		}
		float unscaledTime = Time.unscaledTime;
		if (!IsBaseIdle())
		{
			if (_walking)
			{
				StopWalking();
			}
			if (_wasBaseIdle)
			{
				_pauseUntil = unscaledTime + UnityEngine.Random.Range(0.08f, 0.22f);
				_nextDecisionTime = unscaledTime + UnityEngine.Random.Range(0.25f, 0.75f);
			}
			_wasBaseIdle = false;
			return;
		}
		if (!_wasBaseIdle)
		{
			_pauseUntil = unscaledTime + UnityEngine.Random.Range(0.08f, 0.22f);
			_nextDecisionTime = Mathf.Max(_nextDecisionTime, unscaledTime + UnityEngine.Random.Range(0.25f, 0.75f));
			_wasBaseIdle = true;
		}
		if (unscaledTime < _pauseUntil)
		{
			return;
		}
		if (!_walking)
		{
			if (!(Randomizer <= 0f) && unscaledTime >= _nextDecisionTime)
			{
				StartWalk(forced: false);
			}
		}
		else
		{
			StepWalk();
		}
	}

	private void OnGUI()
	{
		if (DrawBlockingDebug && UseAvatarBoundsBlocking && EnableLocomotion && TryGetBlockingInfo(out var bi))
		{
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(DebugOverlayAlpha));
			float effectiveMinXUnity = bi.effectiveMinXUnity;
			float effectiveMaxXUnity = bi.effectiveMaxXUnity;
			DrawVLine(effectiveMinXUnity, 0f, Screen.height, 2f);
			DrawVLine(effectiveMaxXUnity, 0f, Screen.height, 2f);
			float edgeThresholdUnity = bi.edgeThresholdUnity;
			float x = Mathf.Clamp(edgeThresholdUnity, 0f, Screen.width);
			float x2 = Mathf.Clamp((float)Screen.width - edgeThresholdUnity, 0f, Screen.width);
			DrawVLine(x, 0f, Screen.height, 1f);
			DrawVLine(x2, 0f, Screen.height, 1f);
			GUI.color = color;
		}
	}

	private void DrawVLine(float x, float yTop, float height, float width)
	{
		GUI.DrawTexture(new Rect(x - width * 0.5f, yTop, width, height), Texture2D.whiteTexture);
	}

	private void OnDrawGizmos()
	{
		if (DrawBlockingDebug && UseAvatarBoundsBlocking)
		{
			Camera camera = ((BoundsCamera != null) ? BoundsCamera : Camera.main);
			if (!(camera == null) && TryGetBlockingInfo(out var bi))
			{
				float z = camera.nearClipPlane + 0.05f;
				Vector3 vector = camera.ScreenToWorldPoint(new Vector3(bi.effectiveMinXUnity, 0f, z));
				Vector3 to = camera.ScreenToWorldPoint(new Vector3(bi.effectiveMinXUnity, Screen.height, z));
				Vector3 vector2 = camera.ScreenToWorldPoint(new Vector3(bi.effectiveMaxXUnity, 0f, z));
				Vector3 to2 = camera.ScreenToWorldPoint(new Vector3(bi.effectiveMaxXUnity, Screen.height, z));
				Gizmos.color = new Color(1f, 1f, 1f, 0.75f);
				Gizmos.DrawLine(vector, to);
				Gizmos.DrawLine(vector2, to2);
				float edgeThresholdUnity = bi.edgeThresholdUnity;
				Vector3 vector3 = camera.ScreenToWorldPoint(new Vector3(edgeThresholdUnity, 0f, z));
				Vector3 to3 = camera.ScreenToWorldPoint(new Vector3(edgeThresholdUnity, Screen.height, z));
				Vector3 vector4 = camera.ScreenToWorldPoint(new Vector3((float)Screen.width - edgeThresholdUnity, 0f, z));
				Vector3 to4 = camera.ScreenToWorldPoint(new Vector3((float)Screen.width - edgeThresholdUnity, Screen.height, z));
				Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
				Gizmos.DrawLine(vector3, to3);
				Gizmos.DrawLine(vector4, to4);
			}
		}
	}

	public void SetAnimator(Animator a)
	{
		Animator = a;
		RefreshLayerIndex();
	}

	private void ResolveAnimatorSmart(bool immediate)
	{
		float unscaledTime = Time.unscaledTime;
		if (!immediate && unscaledTime < _nextAnimatorResolveTime)
		{
			return;
		}
		_nextAnimatorResolveTime = unscaledTime + 0.75f;
		if (Animator != null && Animator.isActiveAndEnabled)
		{
			return;
		}
		Animator animator = null;
		animator = GetComponent<Animator>();
		if (animator != null && animator.isActiveAndEnabled)
		{
			Animator = animator;
			RefreshLayerIndex();
			return;
		}
		AvatarAnimatorController avatarAnimatorController = UnityEngine.Object.FindFirstObjectByType<AvatarAnimatorController>();
		if (avatarAnimatorController != null && avatarAnimatorController.animator != null && avatarAnimatorController.animator.isActiveAndEnabled)
		{
			Animator = avatarAnimatorController.animator;
			RefreshLayerIndex();
			return;
		}
		PetVoiceReactionHandler petVoiceReactionHandler = UnityEngine.Object.FindFirstObjectByType<PetVoiceReactionHandler>();
		if (petVoiceReactionHandler != null && petVoiceReactionHandler.avatarAnimator != null && petVoiceReactionHandler.avatarAnimator.isActiveAndEnabled)
		{
			Animator = petVoiceReactionHandler.avatarAnimator;
			RefreshLayerIndex();
			return;
		}
		AvatarBubbleHandler avatarBubbleHandler = UnityEngine.Object.FindFirstObjectByType<AvatarBubbleHandler>();
		if (avatarBubbleHandler != null && avatarBubbleHandler.avatarAnimator != null && avatarBubbleHandler.avatarAnimator.isActiveAndEnabled)
		{
			Animator = avatarBubbleHandler.avatarAnimator;
			RefreshLayerIndex();
			return;
		}
		Animator[] array = Resources.FindObjectsOfTypeAll<Animator>();
		foreach (Animator animator2 in array)
		{
			if (!(animator2 == null) && animator2.isActiveAndEnabled && animator2.gameObject.activeInHierarchy && !(animator2.runtimeAnimatorController == null))
			{
				animator = animator2;
				break;
			}
		}
		Animator = animator;
		RefreshLayerIndex();
	}

	private void ResolveBoundsRenderersSmart(bool immediate)
	{
		float unscaledTime = Time.unscaledTime;
		if (immediate || !(unscaledTime < _nextBoundsResolveTime))
		{
			_nextBoundsResolveTime = unscaledTime + 1f;
			Transform transform = ((AvatarBoundsRoot != null && AvatarBoundsRoot.IsChildOf(base.transform)) ? AvatarBoundsRoot : base.transform);
			if (transform == null)
			{
				_boundsRenderers = null;
			}
			else
			{
				_boundsRenderers = transform.GetComponentsInChildren<Renderer>(includeInactive: true);
			}
		}
	}

	private void RefreshLayerIndex()
	{
		if (!(Animator == null))
		{
			int layerIndex = Animator.GetLayerIndex(BaseLayerName);
			_baseLayerIndex = ((layerIndex >= 0) ? layerIndex : 0);
		}
	}

	private bool IsBaseIdle()
	{
		return Animator.GetCurrentAnimatorStateInfo(_baseLayerIndex).IsName(BaseIdleStateName);
	}

	private void ForceStartWalk()
	{
		StopWalking();
		_pauseUntil = 0f;
		_nextDecisionTime = 0f;
		StartWalk(forced: true);
	}

	private void StartWalk(bool forced)
	{
		float num = Mathf.Max(0f, MinWalkCycle);
		float num2 = Mathf.Max(num, MaxWalkCycle);
		if (num2 <= 0.01f)
		{
			StopWalking();
			ScheduleNextDecision(immediate: false);
			return;
		}
		int num3 = 0;
		if (_forcedNextDir != 0)
		{
			num3 = _forcedNextDir;
			_forcedNextDir = 0;
		}
		else
		{
			num3 = PickDirectionByEdges();
		}
		if (num3 == 0)
		{
			num3 = ((!(UnityEngine.Random.value < 0.5f)) ? 1 : (-1));
		}
		_dir = num3;
		_remainingPixels = UnityEngine.Random.Range(num, num2);
		_walking = true;
		Animator.SetBool(WalkLeftParam, _dir < 0);
		Animator.SetBool(WalkRightParam, _dir > 0);
	}

	private int PickDirectionByEdges()
	{
		if (!UseAvatarBoundsBlocking)
		{
			return 0;
		}
		if (!TryGetBlockingInfo(out var bi))
		{
			return 0;
		}
		float num = bi.effectiveMinGlobalX - bi.monitorLeft;
		float num2 = bi.monitorRight - bi.effectiveMaxGlobalX;
		if (num <= (float)bi.edgeThresholdGlobal && num2 <= (float)bi.edgeThresholdGlobal)
		{
			if (num < num2)
			{
				return 1;
			}
			if (num2 < num)
			{
				return -1;
			}
			if (!(UnityEngine.Random.value < 0.5f))
			{
				return 1;
			}
			return -1;
		}
		if (num2 <= (float)bi.edgeThresholdGlobal)
		{
			return -1;
		}
		if (num <= (float)bi.edgeThresholdGlobal)
		{
			return 1;
		}
		return 0;
	}

	private void StepWalk()
	{
		if (!_walking)
		{
			return;
		}
		if (!GetWindowRect(_hwnd, out var lpRect))
		{
			StopWalking();
			ScheduleNextDecision(immediate: false);
			return;
		}
		int num = lpRect.Right - lpRect.Left;
		if (!TryGetMonitorBounds(_hwnd, out var left, out var right))
		{
			int systemMetrics = GetSystemMetrics(76);
			int systemMetrics2 = GetSystemMetrics(78);
			left = systemMetrics;
			right = systemMetrics + systemMetrics2;
		}
		int min;
		int max;
		if (UseAvatarBoundsBlocking && TryGetBlockingInfo(out var bi))
		{
			min = bi.minWindowX;
			max = bi.maxWindowX;
		}
		else
		{
			int num2 = left;
			int num3 = right - num;
			if (num3 < num2)
			{
				num3 = num2;
			}
			min = num2;
			max = num3;
		}
		float num4 = Mathf.Max(0f, WindowSpeed) * 100f;
		if (num4 <= 0.01f)
		{
			EndWalk();
			return;
		}
		float num5 = Mathf.Min(num4 * Time.unscaledDeltaTime, _remainingPixels);
		int num6 = Mathf.Clamp(lpRect.Left + Mathf.RoundToInt(num5 * (float)_dir), min, max);
		int num7 = Mathf.Abs(num6 - lpRect.Left);
		_remainingPixels -= num7;
		int top = lpRect.Top;
		if (!SetWindowPos(_hwnd, IntPtr.Zero, num6, top, 0, 0, 21u))
		{
			StopWalking();
			ScheduleNextDecision(immediate: false);
			return;
		}
		if (num7 <= 0)
		{
			_remainingPixels = 0f;
			_forcedNextDir = -_dir;
		}
		if (_remainingPixels <= 0.01f)
		{
			EndWalk();
		}
	}

	private void EndWalk()
	{
		StopWalking();
		_pauseUntil = Time.unscaledTime + UnityEngine.Random.Range(0.4f, 1.2f);
		ScheduleNextDecision(immediate: false);
	}

	private void StopWalking()
	{
		if (Animator != null)
		{
			Animator.SetBool(WalkLeftParam, value: false);
			Animator.SetBool(WalkRightParam, value: false);
		}
		_walking = false;
		_remainingPixels = 0f;
		_dir = 0;
	}

	private void ScheduleNextDecision(bool immediate)
	{
		float unscaledTime = Time.unscaledTime;
		if (immediate)
		{
			_nextDecisionTime = unscaledTime + UnityEngine.Random.Range(0.2f, 0.8f);
			return;
		}
		float num = Mathf.Max(0.1f, Randomizer);
		_nextDecisionTime = unscaledTime + UnityEngine.Random.Range(num, num * 2f);
	}

	private void CacheWindowHandle()
	{
		_hwnd = WindowManager.Instance != null ? WindowManager.Instance.UnityWindow : IntPtr.Zero;
	}

	private bool TryGetMonitorBounds(IntPtr hwnd, out int left, out int right)
	{
		left = 0;
		right = 0;
		var wm = WindowManager.Instance;
		if (wm == null) return false;
		RectInt monitor = wm.GetMonitorRectFromWindow(hwnd);
		if (monitor.width <= 0) return false;
		left = monitor.xMin;
		right = monitor.xMax;
		return true;
	}

	private bool TryGetBlockingInfo(out BlockingInfo bi)
	{
		bi = default(BlockingInfo);
		if (_hwnd == IntPtr.Zero)
		{
			return false;
		}
		Camera camera = ((BoundsCamera != null) ? BoundsCamera : Camera.main);
		if (camera == null)
		{
			return false;
		}
		if (!GetWindowRect(_hwnd, out var lpRect))
		{
			return false;
		}
		if (!GetClientRect(_hwnd, out var lpRect2))
		{
			return false;
		}
		POINT lpPoint = new POINT
		{
			X = 0,
			Y = 0
		};
		if (!ClientToScreen(_hwnd, ref lpPoint))
		{
			return false;
		}
		int x = lpPoint.X;
		int num = lpRect2.Right - lpRect2.Left;
		if (num <= 0)
		{
			return false;
		}
		if (Screen.width <= 0 || Screen.height <= 0)
		{
			return false;
		}
		float num2 = (float)num / (float)Screen.width;
		if (!TryGetMonitorBounds(_hwnd, out var left, out var right))
		{
			int systemMetrics = GetSystemMetrics(76);
			int systemMetrics2 = GetSystemMetrics(78);
			left = systemMetrics;
			right = systemMetrics + systemMetrics2;
		}
		if (!TryGetAvatarScreenBoundsUnity(camera, out var minX, out var maxX))
		{
			minX = 0f;
			maxX = Screen.width;
		}
		float num3 = minX + BoundsInsetLeft;
		float num4 = maxX - BoundsInsetRight;
		if (num4 < num3)
		{
			num4 = (num3 = (num3 + num4) * 0.5f);
		}
		int num5 = x - lpRect.Left;
		int effectiveMinGlobalX = x + Mathf.RoundToInt(num3 * num2);
		int effectiveMaxGlobalX = x + Mathf.RoundToInt(num4 * num2);
		int b = Mathf.RoundToInt(EdgeThresholdUnityPixels * num2);
		int num6 = left - num5 - Mathf.RoundToInt(num3 * num2);
		int num7 = right - num5 - Mathf.RoundToInt(num4 * num2);
		if (num7 < num6)
		{
			num7 = num6;
		}
		bi.monitorLeft = left;
		bi.monitorRight = right;
		bi.effectiveMinXUnity = num3;
		bi.effectiveMaxXUnity = num4;
		bi.effectiveMinGlobalX = effectiveMinGlobalX;
		bi.effectiveMaxGlobalX = effectiveMaxGlobalX;
		bi.edgeThresholdUnity = EdgeThresholdUnityPixels;
		bi.edgeThresholdGlobal = Mathf.Max(0, b);
		bi.minWindowX = num6;
		bi.maxWindowX = num7;
		return true;
	}

	private bool TryGetAvatarScreenBoundsUnity(Camera cam, out float minX, out float maxX)
	{
		minX = 1f / 0f;
		maxX = -1f / 0f;
		if (_boundsRenderers == null || _boundsRenderers.Length == 0)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < _boundsRenderers.Length; i++)
		{
			Renderer renderer = _boundsRenderers[i];
			if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is UnityEngine.SpriteRenderer)
			{
				continue;
			}
			Bounds bounds = renderer.bounds;
			Vector3 center = bounds.center;
			Vector3 extents = bounds.extents;
			_boundsCorners[0] = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);
			_boundsCorners[1] = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
			_boundsCorners[2] = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
			_boundsCorners[3] = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);
			_boundsCorners[4] = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);
			_boundsCorners[5] = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);
			_boundsCorners[6] = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);
			_boundsCorners[7] = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);
			for (int j = 0; j < 8; j++)
			{
				Vector3 vector = cam.WorldToScreenPoint(_boundsCorners[j]);
				if (!(vector.z <= 0.0001f))
				{
					flag = true;
					if (vector.x < minX)
					{
						minX = vector.x;
					}
					if (vector.x > maxX)
					{
						maxX = vector.x;
					}
				}
			}
		}
		if (!flag)
		{
			return false;
		}
		if (maxX < minX)
		{
			maxX = (minX = (minX + maxX) * 0.5f);
		}
		return true;
	}
}
