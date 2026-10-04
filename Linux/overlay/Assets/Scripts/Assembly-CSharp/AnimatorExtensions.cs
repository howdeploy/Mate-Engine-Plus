using UnityEngine;

public static class AnimatorExtensions
{
	public static bool HasParameter(this Animator animator, string name, AnimatorControllerParameterType type)
	{
		if (animator == null)
		{
			return false;
		}
		AnimatorControllerParameter[] parameters = animator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].name == name && parameters[i].type == type)
			{
				return true;
			}
		}
		return false;
	}
}
