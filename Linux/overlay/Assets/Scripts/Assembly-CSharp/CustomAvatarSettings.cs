using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class CustomAvatarSettings : MonoBehaviour
{
	[Serializable]
	public class CustomParam
	{
		public string label = "Parameter";

		public ParamType type;

		public string componentType = "";

		public string field = "";

		public float min;

		public float max = 1f;

		public float defaultValue;

		public bool defaultToggle;

		public List<string> options = new List<string>();

		public int defaultDropdown;

		public GameObject uiObject;
	}

	public enum ParamType
	{
		Slider = 0,
		Toggle = 1,
		Dropdown = 2,
		Button = 3
	}

	[Serializable]
	public class SaveData
	{
		public Dictionary<string, float> sliderValues = new Dictionary<string, float>();

		public Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();

		public Dictionary<string, int> dropdownValues = new Dictionary<string, int>();
	}

	public List<CustomParam> parameters = new List<CustomParam>();

	private Dictionary<string, float> sliderValues = new Dictionary<string, float>();

	private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();

	private Dictionary<string, int> dropdownValues = new Dictionary<string, int>();

	private string fileName = "modded_settings.json";

	private string FilePath => Path.Combine(Application.persistentDataPath, fileName);

	private void Awake()
	{
		LoadFromDisk();
		InitDefaultsIfNeeded();
		HookAllEvents();
		ApplyAllSettingsToAllComponents();
	}

	private void OnEnable()
	{
		ApplyAllSettingsToAllComponents();
		RegisterSceneCallbacks();
	}

	private void OnDisable()
	{
		UnregisterSceneCallbacks();
	}

	private void HookAllEvents()
	{
		foreach (CustomParam param in parameters)
		{
			if (param.type == ParamType.Slider && param.uiObject != null)
			{
				Slider component = param.uiObject.GetComponent<Slider>();
				if ((bool)component)
				{
					component.minValue = param.min;
					component.maxValue = param.max;
					component.SetValueWithoutNotify(sliderValues[param.label]);
					component.onValueChanged.RemoveAllListeners();
					component.onValueChanged.AddListener(delegate(float v)
					{
						sliderValues[param.label] = v;
						SaveToDisk();
						ApplyAllSettingsToAllComponents();
					});
				}
			}
			if (param.type == ParamType.Toggle && param.uiObject != null)
			{
				Toggle component2 = param.uiObject.GetComponent<Toggle>();
				if ((bool)component2)
				{
					component2.SetIsOnWithoutNotify(toggleValues[param.label]);
					component2.onValueChanged.RemoveAllListeners();
					component2.onValueChanged.AddListener(delegate(bool v)
					{
						toggleValues[param.label] = v;
						SaveToDisk();
						ApplyAllSettingsToAllComponents();
					});
				}
			}
			if (param.type == ParamType.Dropdown && param.uiObject != null)
			{
				Dropdown component3 = param.uiObject.GetComponent<Dropdown>();
				if ((bool)component3)
				{
					component3.ClearOptions();
					component3.AddOptions(param.options);
					component3.SetValueWithoutNotify(dropdownValues[param.label]);
					component3.onValueChanged.RemoveAllListeners();
					component3.onValueChanged.AddListener(delegate(int v)
					{
						dropdownValues[param.label] = v;
						SaveToDisk();
						ApplyAllSettingsToAllComponents();
					});
				}
			}
			if (param.type != ParamType.Button || !(param.uiObject != null))
			{
				continue;
			}
			Button component4 = param.uiObject.GetComponent<Button>();
			if ((bool)component4)
			{
				component4.onClick.RemoveAllListeners();
				component4.onClick.AddListener(delegate
				{
					ApplyAllSettingsToAllComponents();
				});
			}
		}
	}

	private void InitDefaultsIfNeeded()
	{
		foreach (CustomParam parameter in parameters)
		{
			if (parameter.type == ParamType.Slider && !sliderValues.ContainsKey(parameter.label))
			{
				sliderValues[parameter.label] = parameter.defaultValue;
			}
			if (parameter.type == ParamType.Toggle && !toggleValues.ContainsKey(parameter.label))
			{
				toggleValues[parameter.label] = parameter.defaultToggle;
			}
			if (parameter.type == ParamType.Dropdown && !dropdownValues.ContainsKey(parameter.label))
			{
				dropdownValues[parameter.label] = parameter.defaultDropdown;
			}
		}
		SaveToDisk();
	}

	private void ApplyAllSettingsToAllComponents()
	{
		foreach (CustomParam parameter in parameters)
		{
			foreach (Component item in FindAllComponentsOfType(parameter.componentType))
			{
				FieldInfo field = item.GetType().GetField(parameter.field, BindingFlags.Instance | BindingFlags.Public);
				if (field == null)
				{
					continue;
				}
				if (parameter.type == ParamType.Slider && sliderValues.TryGetValue(parameter.label, out var value))
				{
					if (field.FieldType == typeof(float))
					{
						field.SetValue(item, value);
					}
					if (field.FieldType == typeof(int))
					{
						field.SetValue(item, Mathf.RoundToInt(value));
					}
				}
				if (parameter.type == ParamType.Toggle && toggleValues.TryGetValue(parameter.label, out var value2) && field.FieldType == typeof(bool))
				{
					field.SetValue(item, value2);
				}
				if (parameter.type == ParamType.Dropdown && dropdownValues.TryGetValue(parameter.label, out var value3))
				{
					if (field.FieldType == typeof(int))
					{
						field.SetValue(item, value3);
					}
					if (field.FieldType == typeof(string) && parameter.options.Count > value3)
					{
						field.SetValue(item, parameter.options[value3]);
					}
				}
			}
			if (parameter.type == ParamType.Slider && parameter.uiObject != null)
			{
				Slider component = parameter.uiObject.GetComponent<Slider>();
				if ((bool)component)
				{
					component.SetValueWithoutNotify(sliderValues[parameter.label]);
				}
			}
			if (parameter.type == ParamType.Toggle && parameter.uiObject != null)
			{
				Toggle component2 = parameter.uiObject.GetComponent<Toggle>();
				if ((bool)component2)
				{
					component2.SetIsOnWithoutNotify(toggleValues[parameter.label]);
				}
			}
			if (parameter.type == ParamType.Dropdown && parameter.uiObject != null)
			{
				Dropdown component3 = parameter.uiObject.GetComponent<Dropdown>();
				if ((bool)component3)
				{
					component3.SetValueWithoutNotify(dropdownValues[parameter.label]);
				}
			}
		}
	}

	private IEnumerable<Component> FindAllComponentsOfType(string typeName)
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (!gameObject.scene.IsValid())
			{
				continue;
			}
			Component[] components = gameObject.GetComponents<Component>();
			foreach (Component component in components)
			{
				if (!(component == null) && component.GetType().Name == typeName)
				{
					yield return component;
				}
			}
		}
	}

	private void SaveToDisk()
	{
		string contents = JsonConvert.SerializeObject(new SaveData
		{
			sliderValues = sliderValues,
			toggleValues = toggleValues,
			dropdownValues = dropdownValues
		}, Formatting.Indented);
		File.WriteAllText(FilePath, contents);
	}

	private void LoadFromDisk()
	{
		if (File.Exists(FilePath))
		{
			try
			{
				SaveData saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(FilePath));
				sliderValues = saveData.sliderValues ?? new Dictionary<string, float>();
				toggleValues = saveData.toggleValues ?? new Dictionary<string, bool>();
				dropdownValues = saveData.dropdownValues ?? new Dictionary<string, int>();
				return;
			}
			catch
			{
				sliderValues = new Dictionary<string, float>();
				toggleValues = new Dictionary<string, bool>();
				dropdownValues = new Dictionary<string, int>();
				return;
			}
		}
		sliderValues = new Dictionary<string, float>();
		toggleValues = new Dictionary<string, bool>();
		dropdownValues = new Dictionary<string, int>();
	}

	private void RegisterSceneCallbacks()
	{
	}

	private void UnregisterSceneCallbacks()
	{
	}

	private void OnHierarchyChanged()
	{
		ApplyAllSettingsToAllComponents();
	}

	private void OnTransformChildrenChanged()
	{
		ApplyAllSettingsToAllComponents();
	}

	private void OnApplicationFocus(bool focus)
	{
		if (focus)
		{
			ApplyAllSettingsToAllComponents();
		}
	}
}
