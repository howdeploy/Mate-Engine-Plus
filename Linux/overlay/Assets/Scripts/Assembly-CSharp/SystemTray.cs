using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SystemTray : MonoBehaviour
{
	[Serializable]
	public class TrayAction
	{
		public string label;

		public TrayActionType type;

		public GameObject handlerObject;

		public string toggleField;

		public string methodName;
	}

	public enum TrayActionType
	{
		Toggle = 0,
		Button = 1,
		Method = 2
	}

	[SerializeField]
	private Texture2D icon;

	[SerializeField]
	private string iconName;

	[SerializeField]
	public List<TrayAction> actions = new List<TrayAction>();

	private void Start()
	{
		var tray = gameObject.AddComponent<TrayIndicator>();
		tray.OnBuildMenu = BuildLinuxMenu;
		tray.InitializeTrayIcon(iconName);
		tray.AddMenuItem(BuildLinuxMenu());
	}

	private List<TrayMenuEntry> BuildLinuxMenu()
	{
		var entries = new List<TrayMenuEntry>();
		foreach (var entry in BuildMenu()) entries.Add(new TrayMenuEntry(entry.Item1, entry.Item2));
		return entries;
	}

	private List<(string, Action)> BuildMenu()
	{
		List<(string, Action)> list = new List<(string, Action)>();
		foreach (TrayAction action in actions)
		{
			if (action.type == TrayActionType.Toggle)
			{
				string item = (GetToggleState(action) ? "✔ " : "✖ ") + action.label;
				list.Add((item, delegate
				{
					ToggleAction(action);
				}));
			}
			else if (action.type == TrayActionType.Button || action.type == TrayActionType.Method)
			{
				list.Add((action.label, delegate
				{
					ButtonAction(action);
				}));
			}
		}
		RemoveTaskbarApp app = UnityEngine.Object.FindObjectOfType<RemoveTaskbarApp>();
		string item2 = ((app != null && app.IsHidden) ? "✖ Show App in Taskbar" : "✔ Hide App from Taskbar");
		list.Add((item2, delegate
		{
			if (app != null)
			{
				app.ToggleAppMode();
			}
		}));
		list.Add(("Quit MateEngine", QuitApp));
		return list;
	}

	private bool GetToggleState(TrayAction action)
	{
		if (action.handlerObject == null || string.IsNullOrEmpty(action.toggleField))
		{
			return false;
		}
		MonoBehaviour[] components = action.handlerObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in components)
		{
			if (monoBehaviour == null)
			{
				continue;
			}
			FieldInfo field = monoBehaviour.GetType().GetField(action.toggleField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(Toggle))
			{
				Toggle toggle = field.GetValue(monoBehaviour) as Toggle;
				if (toggle != null)
				{
					return toggle.isOn;
				}
			}
		}
		return false;
	}

	private void ToggleAction(TrayAction action)
	{
		if (action.handlerObject == null || string.IsNullOrEmpty(action.toggleField))
		{
			return;
		}
		MonoBehaviour[] components = action.handlerObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in components)
		{
			if (monoBehaviour == null)
			{
				continue;
			}
			FieldInfo field = monoBehaviour.GetType().GetField(action.toggleField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(Toggle))
			{
				Toggle toggle = field.GetValue(monoBehaviour) as Toggle;
				if (toggle != null)
				{
					toggle.isOn = !toggle.isOn;
					break;
				}
			}
		}
	}

	private void ButtonAction(TrayAction action)
	{
		if (action.handlerObject == null || string.IsNullOrEmpty(action.methodName))
		{
			return;
		}
		MonoBehaviour[] components = action.handlerObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in components)
		{
			if (!(monoBehaviour == null))
			{
				MethodInfo method = monoBehaviour.GetType().GetMethod(action.methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
				if (method != null && method.GetParameters().Length == 0)
				{
					method.Invoke(monoBehaviour, null);
					break;
				}
			}
		}
	}

	private void QuitApp()
	{
		Application.Quit();
	}
}
