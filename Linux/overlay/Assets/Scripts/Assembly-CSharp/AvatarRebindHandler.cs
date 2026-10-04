using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class AvatarRebindHandler : MonoBehaviour
{
	public bool rebindOnEnable = true;

	public bool rebindOnStart;

	public int waitFrames = 1;

	public bool setCullingAlwaysAnimate = true;

	public bool softControllerNudge = true;

	public bool hardControllerNudge;

	public bool rebindUniversalBlendshapes = true;

	private void OnEnable()
	{
		if (rebindOnEnable)
		{
			StartCoroutine(RebindRoutine());
		}
	}

	private void Start()
	{
		if (rebindOnStart)
		{
			StartCoroutine(RebindRoutine());
		}
	}

	public void RebindNow()
	{
		StartCoroutine(RebindRoutine());
	}

	public static void RebindTree(GameObject root, bool setCulling = true, bool softNudge = true, bool hardNudge = false)
	{
		if (root == null)
		{
			return;
		}
		Animator[] componentsInChildren = root.GetComponentsInChildren<Animator>(includeInactive: true);
		foreach (Animator animator in componentsInChildren)
		{
			if (!(animator == null))
			{
				if (setCulling)
				{
					animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				}
				if (hardNudge)
				{
					RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
					animator.runtimeAnimatorController = null;
					animator.Update(0f);
					animator.runtimeAnimatorController = runtimeAnimatorController;
				}
				else if (softNudge)
				{
					RuntimeAnimatorController runtimeAnimatorController2 = animator.runtimeAnimatorController;
					animator.runtimeAnimatorController = runtimeAnimatorController2;
				}
				animator.Rebind();
				animator.Update(0f);
			}
		}
	}

	private IEnumerator RebindRoutine()
	{
		for (int i = 0; i < Mathf.Max(0, waitFrames); i++)
		{
			yield return null;
		}
		RebindTree(base.gameObject, setCullingAlwaysAnimate, softControllerNudge, hardControllerNudge);
		if (rebindUniversalBlendshapes)
		{
			TryRebindUniversalBlendshapes();
		}
	}

	private void TryRebindUniversalBlendshapes()
	{
		Type type = Type.GetType("UniversalBlendshapes");
		if (!(type == null))
		{
			MethodInfo method = type.GetMethod("RebindAllIn", BindingFlags.Static | BindingFlags.Public);
			if (method != null)
			{
				method.Invoke(null, new object[1] { base.gameObject });
			}
		}
	}
}
