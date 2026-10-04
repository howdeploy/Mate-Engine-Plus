using UnityEngine;

public class AvatarSleepController : MonoBehaviour
{
	[Header("Enable Sleep Feature")]
	public bool enableSleep;

	[Header("Sleep Timer (seconds)")]
	[Range(30f, 360f)]
	public float sleepTimer = 60f;

	[Header("Allowed States (Whitelist)")]
	public string[] allowedStates = new string[2] { "Idle", "Sleeping" };

	[Header("Wake Up If Any Of These Animator Bools Is True")]
	public string[] wakeUpBools = new string[1] { "isDragging" };

	[Header("Debug Info (Read Only)")]
	[SerializeField]
	private float idleTime;

	[SerializeField]
	private string currentState = "";

	[SerializeField]
	private bool isSleeping;

	private Animator animator;

	private static readonly int isSleepingParam = Animator.StringToHash("IsSleeping");

	private void Start()
	{
		animator = GetComponent<Animator>();
		SetSleeping(value: false);
		idleTime = 0f;
	}

	private void Update()
	{
		if (!enableSleep || animator == null)
		{
			SetSleeping(value: false);
			idleTime = 0f;
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		currentState = GetCurrentStateName(currentAnimatorStateInfo);
		if (IsAnyWakeUpBoolTrue())
		{
			WakeUp();
			return;
		}
		bool flag = IsInAllowedState(currentAnimatorStateInfo);
		if (flag)
		{
			if (!isSleeping)
			{
				idleTime += Time.deltaTime;
			}
			if (idleTime >= sleepTimer && !isSleeping)
			{
				SetSleeping(value: true);
			}
		}
		else
		{
			idleTime = 0f;
			SetSleeping(value: false);
		}
		if (isSleeping && !flag)
		{
			SetSleeping(value: false);
			idleTime = 0f;
		}
	}

	private string GetCurrentStateName(AnimatorStateInfo state)
	{
		string[] array = allowedStates;
		foreach (string text in array)
		{
			if (!string.IsNullOrEmpty(text) && state.IsName(text))
			{
				return text;
			}
		}
		return state.shortNameHash.ToString();
	}

	private bool IsInAllowedState(AnimatorStateInfo state)
	{
		if (allowedStates == null || allowedStates.Length == 0)
		{
			return true;
		}
		string[] array = allowedStates;
		foreach (string value in array)
		{
			if (!string.IsNullOrEmpty(value) && state.IsName(value))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsAnyWakeUpBoolTrue()
	{
		if (wakeUpBools == null || wakeUpBools.Length == 0 || animator == null)
		{
			return false;
		}
		string[] array = wakeUpBools;
		foreach (string value in array)
		{
			if (!string.IsNullOrEmpty(value) && animator.GetBool(value))
			{
				return true;
			}
		}
		return false;
	}

	private void SetSleeping(bool value)
	{
		if (isSleeping != value)
		{
			isSleeping = value;
			if (animator != null)
			{
				animator.SetBool(isSleepingParam, value);
			}
		}
	}

	public void WakeUp()
	{
		SetSleeping(value: false);
		idleTime = 0f;
	}
}
