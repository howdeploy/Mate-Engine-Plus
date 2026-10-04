using UnityEngine;

public class UISetOnOff : MonoBehaviour
{
	public GameObject target;

	public void ToggleTarget()
	{
		if (target != null)
		{
			target.SetActive(!target.activeSelf);
		}
	}

	public void SetOnOff(GameObject obj)
	{
		if (obj != null)
		{
			obj.SetActive(!obj.activeSelf);
		}
	}

	public void ToggleAccessoryByName(string ruleName)
	{
		foreach (AccessoiresHandler activeHandler in AccessoiresHandler.ActiveHandlers)
		{
			foreach (AccessoiresHandler.AccessoryRule rule in activeHandler.rules)
			{
				if (rule.ruleName == ruleName)
				{
					rule.isEnabled = !rule.isEnabled;
					break;
				}
			}
		}
	}

	public void ToggleBubbleFeature()
	{
		foreach (AvatarBubbleHandler activeHandler in AvatarBubbleHandler.ActiveHandlers)
		{
			activeHandler.ToggleBubbleFromUI();
		}
	}

	public void UnsnapAllAvatars()
	{
		AvatarWindowHandler[] array = Object.FindObjectsByType<AvatarWindowHandler>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ForceExitWindowSitting();
		}
	}

	public void SetAccessoryState(string ruleName, bool state)
	{
		foreach (AccessoiresHandler activeHandler in AccessoiresHandler.ActiveHandlers)
		{
			foreach (AccessoiresHandler.AccessoryRule rule in activeHandler.rules)
			{
				if (rule.ruleName == ruleName)
				{
					rule.isEnabled = state;
					break;
				}
			}
		}
	}

	public void ToggleBigScreenFeature()
	{
		foreach (AvatarBigScreenHandler activeHandler in AvatarBigScreenHandler.ActiveHandlers)
		{
			activeHandler.ToggleBigScreenFromUI();
		}
	}

	public void ToggleChibiMode()
	{
		ChibiToggle[] array = Object.FindObjectsByType<ChibiToggle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ToggleChibiMode();
		}
	}

	public void CloseApp()
	{
		Application.Quit();
	}

	public void OpenWebsite(string url)
	{
		if (!string.IsNullOrEmpty(url))
		{
			Application.OpenURL(url);
		}
	}
}
