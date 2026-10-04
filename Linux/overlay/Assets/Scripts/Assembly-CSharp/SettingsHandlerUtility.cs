using System;
using System.Reflection;
using UnityEngine;

public static class SettingsHandlerUtility
{
	public static void ReloadAllSettingsHandlers()
	{
		MonoBehaviour[] array = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (MonoBehaviour monoBehaviour in array)
		{
			if (monoBehaviour.isActiveAndEnabled)
			{
				Type type = monoBehaviour.GetType();
				MethodInfo method = type.GetMethod("LoadSettings");
				MethodInfo method2 = type.GetMethod("ApplySettings");
				if (method != null)
				{
					method.Invoke(monoBehaviour, null);
				}
				if (method2 != null)
				{
					method2.Invoke(monoBehaviour, null);
				}
			}
		}
	}
}
