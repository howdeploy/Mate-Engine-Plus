using UnityEngine;

public static class AnimatorParameterHelper
{
	public static bool IsAnyAnimatorBoolTrue(string parameterName)
	{
		Animator[] array = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (Animator animator in array)
		{
			if (animator.runtimeAnimatorController == null)
			{
				continue;
			}
			AnimatorControllerParameter[] parameters = animator.parameters;
			foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
			{
				if (animatorControllerParameter.name == parameterName && animatorControllerParameter.type == AnimatorControllerParameterType.Bool && animator.GetBool(parameterName))
				{
					return true;
				}
			}
		}
		return false;
	}
}
