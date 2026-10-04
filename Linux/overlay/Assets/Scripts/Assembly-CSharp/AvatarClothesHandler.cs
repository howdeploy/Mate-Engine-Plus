using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarClothesHandler : MonoBehaviour
{
	[Header("Optional UI Panel for this menu")]
	public GameObject menuPanel;

	[Header("Button References (Max 8)")]
	public Button[] outfitButtons = new Button[8];

	public TextMeshProUGUI[] buttonLabels = new TextMeshProUGUI[8];

	[Header("Animated Button Settings")]
	[Range(0f, 10f)]
	public float floatSpeedX = 1f;

	[Range(0f, 10f)]
	public float floatSpeedY = 1.5f;

	[Range(0f, 10f)]
	public float floatAmplitudeX = 5f;

	[Range(0f, 10f)]
	public float floatAmplitudeY = 5f;

	private Vector3[] initialButtonPositions = new Vector3[8];

	private float[] buttonTimeOffsets = new float[8];

	private bool lastPanelState;

	public static bool IsMenuOpen { get; private set; }

	private void Start()
	{
		if (menuPanel != null)
		{
			menuPanel.SetActive(value: false);
			IsMenuOpen = false;
			lastPanelState = false;
		}
		for (int i = 0; i < outfitButtons.Length; i++)
		{
			if (outfitButtons[i] != null)
			{
				initialButtonPositions[i] = outfitButtons[i].transform.localPosition;
				buttonTimeOffsets[i] = UnityEngine.Random.Range(0f, 100f);
			}
		}
		RefreshButtons();
	}

	private void Update()
	{
		AnimateButtons();
		if (menuPanel != null)
		{
			bool activeSelf = menuPanel.activeSelf;
			if (activeSelf && !lastPanelState)
			{
				RefreshButtons();
			}
			lastPanelState = activeSelf;
		}
	}

	private void AnimateButtons()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		float time = Time.time;
		for (int i = 0; i < outfitButtons.Length; i++)
		{
			if (outfitButtons[i] != null && outfitButtons[i].gameObject.activeSelf)
			{
				Vector3 vector = initialButtonPositions[i];
				float num = buttonTimeOffsets[i];
				float x = Mathf.Sin(time * floatSpeedX + num) * floatAmplitudeX;
				float y = Mathf.Cos(time * floatSpeedY + num) * floatAmplitudeY;
				outfitButtons[i].transform.localPosition = vector + new Vector3(x, y, 0f);
			}
		}
	}

	public void RefreshButtons()
	{
		Type clothesType;
		Component clothesComponent = FindClothesComponent(out clothesType);
		if (clothesComponent == null)
		{
			HideAllButtons();
			IsMenuOpen = false;
			return;
		}
		FieldInfo field = clothesType.GetField("entries");
		if (field == null)
		{
			HideAllButtons();
			IsMenuOpen = false;
			return;
		}
		if (!(field.GetValue(clothesComponent) is Array array))
		{
			HideAllButtons();
			IsMenuOpen = false;
			return;
		}
		int num = 0;
		int num2 = Mathf.Min(array.Length, outfitButtons.Length);
		for (int i = 0; i < outfitButtons.Length; i++)
		{
			if (i < num2)
			{
				object value = array.GetValue(i);
				if (value == null)
				{
					outfitButtons[i].gameObject.SetActive(value: false);
					continue;
				}
				string text = value.GetType().GetField("name")?.GetValue(value) as string;
				if (string.IsNullOrEmpty(text))
				{
					outfitButtons[i].gameObject.SetActive(value: false);
					continue;
				}
				int index = i;
				outfitButtons[i].gameObject.SetActive(value: true);
				buttonLabels[i].text = text;
				outfitButtons[i].onClick.RemoveAllListeners();
				outfitButtons[i].onClick.AddListener(delegate
				{
					ActivateOutfit(clothesComponent, clothesType, index);
					PlayClothesClickSound();
				});
				initialButtonPositions[i] = outfitButtons[i].transform.localPosition;
				num++;
			}
			else
			{
				outfitButtons[i].gameObject.SetActive(value: false);
			}
		}
		IsMenuOpen = num > 0 && menuPanel != null && menuPanel.activeSelf;
	}

	private void HideAllButtons()
	{
		Button[] array = outfitButtons;
		foreach (Button button in array)
		{
			if (button != null)
			{
				button.gameObject.SetActive(value: false);
			}
		}
	}

	private Component FindClothesComponent(out Type foundType)
	{
		foundType = null;
		MonoBehaviour[] array = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		foreach (MonoBehaviour monoBehaviour in array)
		{
			Type type = monoBehaviour.GetType();
			if (type.Name == "MEClothes" && type.GetField("entries") != null && type.GetField("isScriptLoader") != null && !(bool)type.GetField("isScriptLoader").GetValue(monoBehaviour))
			{
				foundType = type;
				return monoBehaviour;
			}
		}
		return null;
	}

	private void ActivateOutfit(Component clothesComponent, Type clothesType, int index)
	{
		MethodInfo method = clothesType.GetMethod("ActivateOutfit");
		if (method != null)
		{
			method.Invoke(clothesComponent, new object[1] { index });
		}
	}

	private void PlayClothesClickSound()
	{
		MenuAudioHandler menuAudioHandler = UnityEngine.Object.FindFirstObjectByType<MenuAudioHandler>();
		if (!(menuAudioHandler == null) && !(menuAudioHandler.audioSource == null) && menuAudioHandler.buttonSounds != null && menuAudioHandler.buttonSounds.Count != 0)
		{
			float pitch = UnityEngine.Random.Range(menuAudioHandler.buttonPitchMin, menuAudioHandler.buttonPitchMax);
			float num = menuAudioHandler.buttonVolume * (SaveLoadHandler.Instance?.data.menuVolume ?? 1f);
			if (num > 0f)
			{
				menuAudioHandler.audioSource.pitch = pitch;
				menuAudioHandler.audioSource.PlayOneShot(menuAudioHandler.buttonSounds[UnityEngine.Random.Range(0, menuAudioHandler.buttonSounds.Count)], num);
			}
		}
	}
}
