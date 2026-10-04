using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyBindHandler : MonoBehaviour
{
	[Serializable]
	public class KeyBindEntry
	{
		public GameObject target;

		public KeyCode key;

		public AudioSource toggleOnSound;

		public AudioSource toggleOffSound;

		public bool unloadLLMOnToggleOff;

		public bool waitForKey;
	}

	public List<KeyBindEntry> keyBinds = new List<KeyBindEntry>();

	private void Update()
	{
		foreach (KeyBindEntry keyBind in keyBinds)
		{
			if (keyBind.waitForKey)
			{
				foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
				{
					if (Input.GetKeyDown(value))
					{
						keyBind.key = value;
						keyBind.waitForKey = false;
						Debug.Log($"Key bound: {value}");
						break;
					}
				}
			}
			else
			{
				if (!Application.isPlaying || !(keyBind.target != null) || keyBind.key == KeyCode.None || !Input.GetKeyDown(keyBind.key))
				{
					continue;
				}
				bool flag = !keyBind.target.activeSelf;
				keyBind.target.SetActive(flag);
				if (flag && keyBind.toggleOnSound != null)
				{
					keyBind.toggleOnSound.Play();
				}
				else if (!flag)
				{
					if (keyBind.toggleOffSound != null)
					{
						keyBind.toggleOffSound.Play();
					}
					_ = keyBind.unloadLLMOnToggleOff;
				}
			}
		}
	}
}
