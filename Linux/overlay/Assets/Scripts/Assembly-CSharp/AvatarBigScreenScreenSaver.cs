using System.Runtime.InteropServices;
using UnityEngine;

public class AvatarBigScreenScreenSaver : MonoBehaviour
{
	public struct POINT
	{
		public int X;

		public int Y;
	}

	[Header("Enable BigScreen Screensaver Feature")]
	public bool enableBigScreenScreenSaver;

	[Header("Timeout Step (Slider 0-10, set by SettingsMenu)")]
	public int timeoutStep;

	[Header("Mouse movement threshold (pixels)")]
	public float minMoveDistance = 2f;

	[Header("Allowed Animator States (Whitelist)")]
	public string[] allowedStates = new string[1] { "Idle" };

	[Header("Click disables BigScreen completely")]
	public bool clickDisablesBoth;

	[Header("Live Status (Inspector)")]
	[SerializeField]
	private float inspectorTime;

	public string inspectorEvent;

	[SerializeField]
	private string inspectorTimeoutLabel;

	private static readonly int[] TimeoutSteps = new int[11]
	{
		30, 60, 300, 900, 1800, 2700, 3600, 5400, 7200, 9000,
		10800
	};

	private static readonly string[] TimeoutLabels = new string[11]
	{
		"30s", "1 min", "5 min", "15 min", "30 min", "45 min", "1 h", "1.5 h", "2 h", "2.5 h",
		"3 h"
	};

	private AvatarBigScreenHandler bigScreenHandler;

	private Animator avatarAnimator;

	private Vector2 lastMousePos;

	private float idleTimer;

	private bool lastGlobalMouseDown;


	private void Start()
	{
		bigScreenHandler = GetComponent<AvatarBigScreenHandler>();
		avatarAnimator = GetComponent<Animator>();
		lastMousePos = GetGlobalMousePosition();
		LoadSettings();
		ResetTimer();
	}

	private void Update()
	{
		LoadSettings();
		if (MenuActions.IsAnyMenuOpen())
		{
			inspectorEvent = "Screensaver blocked by menu";
			inspectorTime = 0f;
			idleTimer = 0f;
			UpdateInspectorTimeoutLabel();
			return;
		}
		if (!enableBigScreenScreenSaver)
		{
			inspectorEvent = "Screensaver disabled";
			inspectorTime = 0f;
			idleTimer = 0f;
			UpdateInspectorTimeoutLabel();
			return;
		}
		bool num = avatarAnimator != null && avatarAnimator.GetBool("isBigScreen");
		bool flag = avatarAnimator != null && avatarAnimator.GetBool("isBigScreenSaver");
		if (num && flag)
		{
			idleTimer = 0f;
			inspectorEvent = "Screensaver active! Timer paused";
			inspectorTime = 0f;
			UpdateInspectorTimeoutLabel();
			if (IsGlobalUserInput())
			{
				avatarAnimator.SetBool("isBigScreenSaver", value: false);
				inspectorEvent = "Screensaver ended by input";
				bool flag2 = avatarAnimator.GetBool("isBigScreenAlarm");
				if (clickDisablesBoth && !flag2)
				{
					avatarAnimator.SetBool("isBigScreen", value: false);
					inspectorEvent = "Exited Screensaver & BigScreen by input";
					if (bigScreenHandler != null)
					{
						bigScreenHandler.SendMessage("DeactivateBigScreen");
					}
				}
			}
			lastMousePos = GetGlobalMousePosition();
			return;
		}
		if (!IsInAllowedState())
		{
			inspectorEvent = "Screensaver blocked by state";
			inspectorTime = 0f;
			idleTimer = 0f;
			UpdateInspectorTimeoutLabel();
			return;
		}
		Vector2 globalMousePosition = GetGlobalMousePosition();
		bool flag3 = IsAnyKeyPressed();
		if (Vector2.Distance(globalMousePosition, lastMousePos) >= minMoveDistance || flag3)
		{
			idleTimer = 0f;
			lastMousePos = globalMousePosition;
			inspectorEvent = (flag3 ? "Timer reset by key" : "Timer reset by mouse");
		}
		else
		{
			idleTimer += Time.deltaTime;
			inspectorEvent = "Timer is running";
		}
		inspectorTime = idleTimer;
		UpdateInspectorTimeoutLabel();
		int num2 = TimeoutSteps[Mathf.Clamp(timeoutStep, 0, TimeoutSteps.Length - 1)];
		if (idleTimer >= (float)num2)
		{
			if (avatarAnimator != null)
			{
				avatarAnimator.SetBool("isBigScreen", value: true);
				avatarAnimator.SetBool("isBigScreenSaver", value: true);
				inspectorEvent = "Screensaver activated!";
				idleTimer = 0f;
			}
			if (bigScreenHandler != null)
			{
				bigScreenHandler.SendMessage("ActivateBigScreen");
			}
		}
	}

	private void LoadSettings()
	{
		if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data != null)
		{
			timeoutStep = Mathf.Clamp(SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex, 0, TimeoutSteps.Length - 1);
			enableBigScreenScreenSaver = SaveLoadHandler.Instance.data.bigScreenScreenSaverEnabled;
		}
	}

	private void ResetTimer()
	{
		idleTimer = 0f;
		inspectorTime = 0f;
		inspectorEvent = "Timer reset";
	}

	private void UpdateInspectorTimeoutLabel()
	{
		int seconds = TimeoutSteps[Mathf.Clamp(timeoutStep, 0, TimeoutSteps.Length - 1)];
		inspectorTimeoutLabel = FormatTime(seconds);
	}

	private static string FormatTime(int seconds)
	{
		int num = seconds / 3600;
		int num2 = seconds % 3600 / 60;
		int num3 = seconds % 60;
		if (num > 0)
		{
			return $"{num}h {num2:D2}m";
		}
		if (num2 > 0)
		{
			return $"{num2}m {num3:D2}s";
		}
		return $"{num3}s";
	}

	private Vector2 GetGlobalMousePosition()
	{
		return WindowManager.Instance != null ? (Vector2)WindowManager.Instance.GetMousePosition() : (Vector2)Input.mousePosition;
	}

	private bool IsAnyKeyPressed()
	{
		return WindowManager.Instance != null && WindowManager.Instance.IsAnyKeyDown();
	}

	private bool IsInAllowedState()
	{
		if (avatarAnimator == null || allowedStates == null || allowedStates.Length == 0)
		{
			return true;
		}
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

	private bool IsGlobalUserInput()
	{
		var wm = WindowManager.Instance;
		bool flag = wm != null && wm.GetMouseButton(KeyCode.Mouse0);
		bool flag2 = flag && !lastGlobalMouseDown;
		lastGlobalMouseDown = flag;
		bool flag3 = IsAnyKeyPressed();
		return flag2 || flag3;
	}
}
