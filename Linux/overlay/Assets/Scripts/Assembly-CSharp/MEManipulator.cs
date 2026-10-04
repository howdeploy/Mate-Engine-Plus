using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MEManipulator : MonoBehaviour
{
	[Serializable]
	public class ReplacementEntry
	{
		public GameObject sourceObject;
	}

	public List<ReplacementEntry> replacements = new List<ReplacementEntry>();

	private HashSet<Animator> patchedAnimators = new HashSet<Animator>();

	private static AnimationClip _dummyClip;

	private static AnimationClip DummyClip
	{
		get
		{
			if (_dummyClip == null)
			{
				_dummyClip = new AnimationClip();
				_dummyClip.name = "EmptyFallback";
			}
			return _dummyClip;
		}
	}

	private void Update()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		AvatarAnimatorReceiver[] array = UnityEngine.Object.FindObjectsByType<AvatarAnimatorReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (AvatarAnimatorReceiver avatarAnimatorReceiver in array)
		{
			if (!(avatarAnimatorReceiver == null) && !(avatarAnimatorReceiver.avatarAnimator == null))
			{
				Animator avatarAnimator = avatarAnimatorReceiver.avatarAnimator;
				if (!patchedAnimators.Contains(avatarAnimator))
				{
					ApplyAllReplacementsToAnimator(avatarAnimator);
					patchedAnimators.Add(avatarAnimator);
				}
			}
		}
	}

	public void ApplyAllReplacementsToAnimator(Animator animator)
	{
		GameObject targetRoot = animator.gameObject;
		foreach (ReplacementEntry replacement in replacements)
		{
			if (!replacement.sourceObject)
			{
				continue;
			}
			Animator component = replacement.sourceObject.GetComponent<Animator>();
			if (component != null && component.runtimeAnimatorController != null)
			{
				RuntimeAnimatorController runtimeAnimatorController = component.runtimeAnimatorController;
				AnimatorOverrideController animatorOverrideController = ((!(runtimeAnimatorController is AnimatorOverrideController animatorOverrideController2)) ? new AnimatorOverrideController(runtimeAnimatorController) : animatorOverrideController2);
				EnsureClipNotEmpty(animatorOverrideController, "HoverReaction");
				EnsureClipNotEmpty(animatorOverrideController, "HoverFace");
				animator.runtimeAnimatorController = animatorOverrideController;
				PetVoiceReactionHandler component2 = animator.GetComponent<PetVoiceReactionHandler>();
				if (component2 != null)
				{
					FieldInfo field = typeof(PetVoiceReactionHandler).GetField("overrideController", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(component2, animatorOverrideController);
					}
					FieldInfo field2 = typeof(PetVoiceReactionHandler).GetField("lastController", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field2 != null)
					{
						field2.SetValue(component2, animatorOverrideController.runtimeAnimatorController);
					}
					FieldInfo field3 = typeof(PetVoiceReactionHandler).GetField("hasSetup", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field3 != null)
					{
						field3.SetValue(component2, false);
					}
				}
			}
			MonoBehaviour[] components = replacement.sourceObject.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour monoBehaviour in components)
			{
				if (!(monoBehaviour == null))
				{
					Type type = monoBehaviour.GetType();
					CopyFieldsAndProperties(targetRoot, type, monoBehaviour);
					Behaviour behaviour = monoBehaviour;
					if ((object)behaviour != null)
					{
						behaviour.enabled = false;
					}
				}
			}
			replacement.sourceObject.SetActive(value: false);
		}
	}

	private void EnsureClipNotEmpty(AnimatorOverrideController ctrl, string slot)
	{
		List<KeyValuePair<AnimationClip, AnimationClip>> list = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		ctrl.GetOverrides(list);
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Key != null && list[i].Key.name == slot)
			{
				flag = true;
				if (list[i].Value == null)
				{
					ctrl[list[i].Key] = DummyClip;
				}
				break;
			}
		}
		if (!flag)
		{
			try
			{
				ctrl[slot] = DummyClip;
			}
			catch
			{
			}
		}
	}

	private void CopyFieldsAndProperties(GameObject targetRoot, Type type, MonoBehaviour source)
	{
		Component component = targetRoot.GetComponent(type);
		if (!component)
		{
			component = targetRoot.AddComponent(type);
		}
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.IsNotSerialized || fieldInfo.Name == "enabled")
			{
				continue;
			}
			try
			{
				object value = fieldInfo.GetValue(source);
				if (!IsEmpty(value))
				{
					fieldInfo.SetValue(component, value);
				}
			}
			catch
			{
			}
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!propertyInfo.CanWrite || !propertyInfo.CanRead || propertyInfo.Name == "name" || propertyInfo.Name == "tag" || propertyInfo.Name == "enabled")
			{
				continue;
			}
			try
			{
				object value2 = propertyInfo.GetValue(source, null);
				if (!IsEmpty(value2))
				{
					propertyInfo.SetValue(component, value2, null);
				}
			}
			catch
			{
			}
		}
	}

	private bool IsEmpty(object value)
	{
		if (value == null)
		{
			return true;
		}
		Type type = value.GetType();
		if (type == typeof(string))
		{
			return string.IsNullOrWhiteSpace((string)value);
		}
		if (type.IsValueType)
		{
			object obj = Activator.CreateInstance(type);
			return value.Equals(obj);
		}
		if (value is UnityEngine.Object obj2)
		{
			return obj2 == null;
		}
		return false;
	}

	public void ResetPatchedAnimators()
	{
		patchedAnimators.Clear();
	}
}
