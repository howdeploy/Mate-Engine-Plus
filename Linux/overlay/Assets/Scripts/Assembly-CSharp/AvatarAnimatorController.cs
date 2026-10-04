using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PulseAudio;
using UnityEngine;

public class AvatarAnimatorController : MonoBehaviour
{
	[Header("State Values")]
	public Animator animator;

	public float SOUND_THRESHOLD = 0.02f;

	public List<string> allowedApps = new List<string>();

	public int totalIdleAnimations = 10;

	public float IDLE_SWITCH_TIME = 12f;

	public float IDLE_TRANSITION_TIME = 3f;

	public int DANCE_CLIP_COUNT = 5;

	[Header("Dancing")]
	public bool enableDancing = true;

	public bool enableDanceSwitch = true;

	public float DANCE_SWITCH_TIME = 15f;

	public float DANCE_TRANSITION_TIME = 2f;

	public bool BlockDraggingOverride;

	private static readonly int danceIndexParam = Animator.StringToHash("DanceIndex");

	private static readonly int isIdleParam = Animator.StringToHash("isIdle");

	private static readonly int isDraggingParam = Animator.StringToHash("isDragging");

	private static readonly int isDancingParam = Animator.StringToHash("isDancing");

	private static readonly int idleIndexParam = Animator.StringToHash("IdleIndex");

	private Coroutine audioQueryCoroutine;

	private Coroutine soundCheckCoroutine;

	private Coroutine idleTransitionCoroutine;

	private Coroutine danceTransitionCoroutine;

	private float lastSoundCheckTime;

	private float idleTimer;

	private float danceTimer;

	private int idleState;

	private int danceState;

	private float dragLockTimer;

	private bool mouseHeld;

	public bool isDragging;

	public bool isDancing;

	public bool isIdle;

	[Header("Character Mode")]
	public bool enableHusbandoMode;

	private static readonly int isMaleParam = Animator.StringToHash("isMale");

	private static readonly int isFemaleParam = Animator.StringToHash("isFemale");

	private void OnEnable()
	{
		if ((object)animator == null)
		{
			animator = GetComponent<Animator>();
		}
		Application.runInBackground = true;
		if (PulseAudioManager.Instance == null)
		{
			var audio = new GameObject("Linux Audio Monitor");
			UnityEngine.Object.DontDestroyOnLoad(audio);
			audio.AddComponent<PulseAudioManager>();
		}
		animator.SetFloat(isFemaleParam, enableHusbandoMode ? 0f : 1f);
		animator.SetFloat(isMaleParam, enableHusbandoMode ? 1f : 0f);
		soundCheckCoroutine = StartCoroutine(CheckSoundContinuously());
	}

	private void OnDisable()
	{
		CleanupAudioResources();
	}

	private void OnDestroy()
	{
		CleanupAudioResources();
	}

	private void OnApplicationQuit()
	{
		CleanupAudioResources();
	}

	private IEnumerator CheckSoundContinuously()
	{
		WaitForSeconds wait = new WaitForSeconds(2f);
		while (true)
		{
			CheckForSound();
			yield return wait;
		}
	}

	private void CheckForSound()
	{
		if (MenuActions.IsMovementBlocked() || !enableDancing)
		{
			if (isDancing)
			{
				SetDancing(value: false);
			}
		}
		else if (!isDragging && audioQueryCoroutine == null)
		{
			audioQueryCoroutine = StartCoroutine(CheckPlayingAudio());
		}
	}

	private void StartDancing()
	{
		isDancing = true;
		danceTimer = 0f;
		danceState = UnityEngine.Random.Range(0, DANCE_CLIP_COUNT);
		animator.SetBool(isDancingParam, value: true);
		animator.SetFloat(danceIndexParam, danceState);
	}

	private void SetDancing(bool value)
	{
		isDancing = value;
		animator.SetBool(isDancingParam, value);
		if (!value && danceTransitionCoroutine != null)
		{
			StopCoroutine(danceTransitionCoroutine);
			danceTransitionCoroutine = null;
		}
	}

	private IEnumerator CheckPlayingAudio()
	{
		// Yield before querying so the owner can store and cancel this coroutine.
		yield return null;
		var audio = PulseAudioManager.Instance;
		while (audio != null && (!audio.allSet || audio.callbackRunning)) yield return null;
		if (audio == null) { audioQueryCoroutine = null; yield break; }
		List<AudioProgram> programs = null;
		audio.GetPlayingAudioPrograms(result => programs = result);
		while (programs == null && audio != null) yield return null;
		bool playing = false;
		if (programs != null)
		{
			foreach (var program in programs)
			{
				if (program.IsMuted || program.IsCorked) continue;
				foreach (var allowed in allowedApps)
				{
					if (string.IsNullOrEmpty(allowed) || !((program.Name ?? "").StartsWith(allowed, StringComparison.OrdinalIgnoreCase) ||
						(program.ProcessName ?? "").StartsWith(allowed, StringComparison.OrdinalIgnoreCase))) continue;
					audio.StartMonitoringStream(program.NodeId);
					if (audio.TryGetProgramPeak(program.NodeId, out var peak) && peak > SOUND_THRESHOLD) playing = true;
				}
			}
		}
		if (!MenuActions.IsMovementBlocked() && enableDancing && !isDragging)
		{
			if (playing && !isDancing) StartDancing();
			else if (!playing && isDancing) SetDancing(false);
		}
		audioQueryCoroutine = null;
	}

	private void Update()
	{
		animator.SetFloat(isFemaleParam, enableHusbandoMode ? 0f : 1f);
		animator.SetFloat(isMaleParam, enableHusbandoMode ? 1f : 0f);
		if (BlockDraggingOverride || MenuActions.IsMovementBlocked() || TutorialMenu.IsActive)
		{
			if (isDragging)
			{
				SetDragging(value: false);
			}
			if (isDancing)
			{
				SetDancing(value: false);
			}
			return;
		}
		if (Input.GetMouseButtonDown(0))
		{
			SetDragging(value: true);
			mouseHeld = true;
			dragLockTimer = 0.3f;
			SetDancing(value: false);
		}
		if (Input.GetMouseButtonUp(0))
		{
			mouseHeld = false;
		}
		if (dragLockTimer > 0f)
		{
			dragLockTimer -= Time.deltaTime;
			animator.SetBool(isDraggingParam, value: true);
		}
		else if (!mouseHeld && isDragging)
		{
			SetDragging(value: false);
		}
		idleTimer += Time.deltaTime;
		if (idleTimer > IDLE_SWITCH_TIME)
		{
			idleTimer = 0f;
			int num = (idleState + 1) % totalIdleAnimations;
			if (num == 0)
			{
				animator.SetFloat(idleIndexParam, 0f);
			}
			else
			{
				if (idleTransitionCoroutine != null)
				{
					StopCoroutine(idleTransitionCoroutine);
				}
				idleTransitionCoroutine = StartCoroutine(SmoothIdleTransition(num));
			}
			idleState = num;
		}
		UpdateIdleStatus();
		if (!isDancing || !enableDanceSwitch)
		{
			return;
		}
		danceTimer += Time.deltaTime;
		if (!(danceTimer > DANCE_SWITCH_TIME))
		{
			return;
		}
		danceTimer = 0f;
		int num2 = (danceState + 1) % DANCE_CLIP_COUNT;
		if (num2 == 0)
		{
			animator.SetFloat(danceIndexParam, 0f);
		}
		else
		{
			if (danceTransitionCoroutine != null)
			{
				StopCoroutine(danceTransitionCoroutine);
			}
			danceTransitionCoroutine = StartCoroutine(SmoothDanceTransition(num2));
		}
		danceState = num2;
	}

	private void SetDragging(bool value)
	{
		isDragging = value;
		animator.SetBool(isDraggingParam, value);
	}

	private void UpdateIdleStatus()
	{
		bool flag = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
		if (isIdle != flag)
		{
			isIdle = flag;
			animator.SetBool(isIdleParam, isIdle);
		}
	}

	private IEnumerator SmoothIdleTransition(int newIdle)
	{
		float elapsed = 0f;
		float start = animator.GetFloat(idleIndexParam);
		while (elapsed < IDLE_TRANSITION_TIME)
		{
			elapsed += Time.deltaTime;
			animator.SetFloat(idleIndexParam, Mathf.Lerp(start, newIdle, elapsed / IDLE_TRANSITION_TIME));
			yield return null;
		}
		animator.SetFloat(idleIndexParam, newIdle);
	}

	private IEnumerator SmoothDanceTransition(int newDance)
	{
		float elapsed = 0f;
		float start = animator.GetFloat(danceIndexParam);
		while (elapsed < DANCE_TRANSITION_TIME)
		{
			elapsed += Time.deltaTime;
			animator.SetFloat(danceIndexParam, Mathf.Lerp(start, newDance, elapsed / DANCE_TRANSITION_TIME));
			yield return null;
		}
		animator.SetFloat(danceIndexParam, newDance);
	}

	public bool IsInIdleState()
	{
		return isIdle;
	}

	private void CleanupAudioResources()
	{
		if (soundCheckCoroutine != null)
		{
			StopCoroutine(soundCheckCoroutine);
			soundCheckCoroutine = null;
		}
		if (idleTransitionCoroutine != null)
		{
			StopCoroutine(idleTransitionCoroutine);
			idleTransitionCoroutine = null;
		}
		if (danceTransitionCoroutine != null)
		{
			StopCoroutine(danceTransitionCoroutine);
			danceTransitionCoroutine = null;
		}
		if (audioQueryCoroutine != null)
		{
			StopCoroutine(audioQueryCoroutine);
			audioQueryCoroutine = null;
		}
	}
}
