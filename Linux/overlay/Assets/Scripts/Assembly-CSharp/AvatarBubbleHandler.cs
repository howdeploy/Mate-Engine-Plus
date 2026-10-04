using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AvatarBubbleHandler : MonoBehaviour
{
	[Header("Animator")]
	public Animator avatarAnimator;

	public string animatorParameter = "isSitting";

	[Header("Attach Settings")]
	public GameObject attachTarget;

	public HumanBodyBones attachBone = HumanBodyBones.Head;

	public bool keepOriginalRotation;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip enableSound;

	public AudioClip disableSound;

	[Header("Interaction")]
	public KeyCode activationKey = KeyCode.Space;

	[Header("Spawn Animation")]
	[Range(0f, 1f)]
	public float spawnAnimationSpeed = 0.1f;

	private Animator animator;

	private Transform bone;

	private Transform originalParent;

	private Vector3 originalScale = Vector3.one;

	private float currentLerp;

	private bool wasActive;

	private bool initialized;

	public static List<AvatarBubbleHandler> ActiveHandlers = new List<AvatarBubbleHandler>();

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		animator = ((avatarAnimator != null) ? avatarAnimator : GetComponent<Animator>());
		if (!ActiveHandlers.Contains(this))
		{
			ActiveHandlers.Add(this);
		}
		if (attachTarget != null)
		{
			originalParent = attachTarget.transform.parent;
			if (originalScale == Vector3.zero || attachTarget.transform.localScale == Vector3.zero)
			{
				originalScale = new Vector3(1f, 1f, 1f);
			}
			attachTarget.transform.localScale = Vector3.zero;
			attachTarget.SetActive(value: false);
			currentLerp = 0f;
			wasActive = false;
			initialized = true;
		}
		bone = null;
	}

	private void OnDisable()
	{
		if (Application.isPlaying)
		{
			ActiveHandlers.Remove(this);
			if (attachTarget != null)
			{
				attachTarget.transform.localScale = Vector3.zero;
				attachTarget.SetActive(value: false);
			}
			bone = null;
			wasActive = false;
			currentLerp = 0f;
			initialized = false;
		}
	}

	private void Update()
	{
		if (!Application.isPlaying || animator == null || attachTarget == null)
		{
			return;
		}
		if (bone == null)
		{
			bone = animator.GetBoneTransform(attachBone);
		}
		if (IsDragging() && Input.GetKeyDown(activationKey) && !animator.GetBool("isWindowSit"))
		{
			bool value = !animator.GetBool(animatorParameter);
			animator.SetBool(animatorParameter, value);
		}
		if (animator != null && animator.GetBool("isBigScreen"))
		{
			if (animator.GetBool(animatorParameter))
			{
				animator.SetBool(animatorParameter, value: false);
			}
			if (attachTarget != null)
			{
				attachTarget.SetActive(value: false);
			}
			wasActive = false;
			currentLerp = 0f;
			return;
		}
		bool flag = animator.GetBool(animatorParameter);
		float target = (flag ? 1f : 0f);
		float num = Mathf.Lerp(10f, 0.25f, spawnAnimationSpeed);
		currentLerp = Mathf.MoveTowards(currentLerp, target, Time.unscaledDeltaTime * num);
		if (!wasActive && currentLerp > 0f)
		{
			attachTarget.SetActive(value: true);
			PlaySound(enableSound);
			wasActive = true;
		}
		Vector3 lossyScale = animator.transform.lossyScale;
		attachTarget.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.Scale(originalScale, lossyScale), currentLerp);
		if (wasActive && currentLerp <= 0f)
		{
			attachTarget.SetActive(value: false);
			PlaySound(disableSound);
			wasActive = false;
		}
		if (flag && bone != null)
		{
			if (keepOriginalRotation)
			{
				attachTarget.transform.position = bone.position;
			}
			else if (attachTarget.transform.parent != bone)
			{
				attachTarget.transform.SetParent(bone, worldPositionStays: false);
			}
		}
		else if (!flag && attachTarget.transform.parent != originalParent)
		{
			attachTarget.transform.SetParent(originalParent, worldPositionStays: false);
		}
	}

	private void PlaySound(AudioClip clip)
	{
		if (audioSource != null && clip != null)
		{
			audioSource.PlayOneShot(clip);
		}
	}

	public void SetAnimator(Animator newAnimator)
	{
		avatarAnimator = newAnimator;
		animator = newAnimator;
		bone = null;
	}

	private bool IsDragging()
	{
		if (animator != null)
		{
			return animator.GetBool("isDragging");
		}
		return false;
	}

	public void ToggleBubbleFromUI()
	{
		if (!(animator == null) && !animator.GetBool("isWindowSit"))
		{
			bool value = !animator.GetBool(animatorParameter);
			animator.SetBool(animatorParameter, value);
		}
	}
}
