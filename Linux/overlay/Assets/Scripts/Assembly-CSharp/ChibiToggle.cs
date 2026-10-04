using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChibiToggle : MonoBehaviour
{
	[Header("Chibi Scale Settings")]
	public Vector3 chibiArmatureScale = new Vector3(0.3f, 0.3f, 0.3f);

	public Vector3 chibiHeadScale = new Vector3(2.7f, 2.7f, 2.7f);

	public Vector3 chibiUpperLegScale = new Vector3(0.6f, 0.6f, 0.6f);

	[Header("Sound Effects")]
	public AudioSource audioSource;

	public List<AudioClip> chibiEnterSounds = new List<AudioClip>();

	public List<AudioClip> chibiExitSounds = new List<AudioClip>();

	[Header("Particle Effect")]
	public GameObject particleEffectObject;

	public float particleDuration = 4f;

	private Animator anim;

	private Transform armatureRoot;

	private Transform head;

	private Transform leftFoot;

	private Transform rightFoot;

	private Transform leftUpperLeg;

	private Transform rightUpperLeg;

	private bool isChibi;

	private Vector3 originalArmaturePosition;

	private void Start()
	{
		anim = GetComponent<Animator>();
		anim.applyRootMotion = false;
		Transform boneTransform = anim.GetBoneTransform(HumanBodyBones.Hips);
		head = anim.GetBoneTransform(HumanBodyBones.Head);
		leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
		rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
		leftUpperLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
		rightUpperLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
		if (boneTransform != null)
		{
			armatureRoot = boneTransform;
			while (armatureRoot.parent != null && armatureRoot.parent != base.transform)
			{
				armatureRoot = armatureRoot.parent;
			}
			originalArmaturePosition = armatureRoot.localPosition;
		}
	}

	public void ToggleChibiMode()
	{
		if ((bool)armatureRoot && (bool)head && (bool)leftFoot && (bool)rightFoot)
		{
			bool flag = !isChibi;
			float originalFootY = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
			armatureRoot.localScale = (flag ? chibiArmatureScale : Vector3.one);
			head.localScale = (flag ? chibiHeadScale : Vector3.one);
			if ((bool)leftUpperLeg)
			{
				leftUpperLeg.localScale = (flag ? chibiUpperLegScale : Vector3.one);
			}
			if ((bool)rightUpperLeg)
			{
				rightUpperLeg.localScale = (flag ? chibiUpperLegScale : Vector3.one);
			}
			isChibi = flag;
			PlayRandomSound(flag);
			TriggerParticles();
			StartCoroutine(AdjustFeetToGround(originalFootY));
		}
	}

	private IEnumerator AdjustFeetToGround(float originalFootY)
	{
		yield return null;
		float num = Mathf.Min(leftFoot.position.y, rightFoot.position.y);
		float y = originalFootY - num;
		base.transform.position += new Vector3(0f, y, 0f);
	}

	private void PlayRandomSound(bool enteringChibi)
	{
		if ((bool)audioSource)
		{
			List<AudioClip> list = (enteringChibi ? chibiEnterSounds : chibiExitSounds);
			if (list.Count != 0)
			{
				AudioClip clip = list[Random.Range(0, list.Count)];
				audioSource.PlayOneShot(clip);
			}
		}
	}

	private void TriggerParticles()
	{
		if ((bool)particleEffectObject)
		{
			StopAllCoroutines();
			StartCoroutine(TemporaryParticleCoroutine());
		}
	}

	private IEnumerator TemporaryParticleCoroutine()
	{
		particleEffectObject.SetActive(value: true);
		yield return new WaitForSeconds(particleDuration);
		particleEffectObject.SetActive(value: false);
	}
}
