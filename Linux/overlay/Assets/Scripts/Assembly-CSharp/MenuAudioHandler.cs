using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuAudioHandler : MonoBehaviour
{
	[Header("Audio Source GameObject (drag here)")]
	public AudioSource audioSource;

	[Range(0f, 10f)]
	public float disableDelay = 1f;

	[Header("Startup Sounds (plays once on app start)")]
	public List<AudioClip> startupSounds = new List<AudioClip>();

	public float startupPitchMin = 1f;

	public float startupPitchMax = 1f;

	[Range(0f, 1f)]
	public float startupVolume = 1f;

	[Range(0f, 10f)]
	public float startupDelaySeconds = 3f;

	[Header("Open Menu Sounds")]
	public List<AudioClip> openMenuSounds = new List<AudioClip>();

	public float openMenuPitchMin = 1f;

	public float openMenuPitchMax = 1f;

	[Range(0f, 1f)]
	public float openMenuVolume = 1f;

	[Header("Close Menu Sounds")]
	public List<AudioClip> closeMenuSounds = new List<AudioClip>();

	public float closeMenuPitchMin = 1f;

	public float closeMenuPitchMax = 1f;

	[Range(0f, 1f)]
	public float closeMenuVolume = 1f;

	[Header("Button Sounds")]
	public List<AudioClip> buttonSounds = new List<AudioClip>();

	public float buttonPitchMin = 1f;

	public float buttonPitchMax = 1f;

	[Range(0f, 1f)]
	public float buttonVolume = 1f;

	[Header("Toggle Sounds")]
	public List<AudioClip> toggleSounds = new List<AudioClip>();

	public float togglePitchMin = 1f;

	public float togglePitchMax = 1f;

	[Range(0f, 1f)]
	public float toggleVolume = 1f;

	[Header("Slider Sounds")]
	public List<AudioClip> sliderSounds = new List<AudioClip>();

	public float sliderPitchMin = 1f;

	public float sliderPitchMax = 1f;

	[Range(0f, 1f)]
	public float sliderVolume = 1f;

	[Header("Dropdown Sounds")]
	public List<AudioClip> dropdownSounds = new List<AudioClip>();

	public float dropdownPitchMin = 1f;

	public float dropdownPitchMax = 1f;

	[Range(0f, 1f)]
	public float dropdownVolume = 1f;

	private HashSet<Slider> activeSliders = new HashSet<Slider>();

	private bool wasMenuOpenLastFrame;

	private float disableTimer;

	private static bool s_startupPlayed;

	private void OnEnable()
	{
		SetupUIListeners();
		StartCoroutine(MenuMonitor());
		StartCoroutine(PlayStartupDelayed());
	}

	private IEnumerator PlayStartupDelayed()
	{
		if (s_startupPlayed)
		{
			yield break;
		}
		while (SaveLoadHandler.Instance == null || SaveLoadHandler.Instance.data == null)
		{
			yield return null;
		}
		yield return new WaitForSecondsRealtime(startupDelaySeconds);
		if (s_startupPlayed)
		{
			yield break;
		}
		if (startupSounds == null || startupSounds.Count == 0)
		{
			s_startupPlayed = true;
			yield break;
		}
		float num = 1f;
		if (SaveLoadHandler.Instance != null)
		{
			num = SaveLoadHandler.Instance.data.menuVolume;
		}
		float num2 = startupVolume * num;
		if (num2 <= 0f)
		{
			s_startupPlayed = true;
			yield break;
		}
		if (audioSource != null && !audioSource.gameObject.activeSelf)
		{
			audioSource.gameObject.SetActive(value: true);
		}
		if (audioSource == null)
		{
			s_startupPlayed = true;
			yield break;
		}
		audioSource.pitch = Random.Range(startupPitchMin, startupPitchMax);
		audioSource.PlayOneShot(startupSounds[Random.Range(0, startupSounds.Count)], num2);
		s_startupPlayed = true;
	}

	private IEnumerator MenuMonitor()
	{
		while (true)
		{
			bool flag = AvatarClothesHandler.IsMenuOpen || TutorialMenu.IsActive;
			if (flag)
			{
				if (audioSource != null && !audioSource.gameObject.activeSelf)
				{
					audioSource.gameObject.SetActive(value: true);
				}
				if (!wasMenuOpenLastFrame)
				{
					PlaySound(openMenuSounds, openMenuPitchMin, openMenuPitchMax, openMenuVolume);
				}
				disableTimer = 0f;
			}
			else
			{
				if (wasMenuOpenLastFrame)
				{
					disableTimer = Time.time + disableDelay;
					PlaySound(closeMenuSounds, closeMenuPitchMin, closeMenuPitchMax, closeMenuVolume);
				}
				if (disableTimer != 0f && Time.time >= disableTimer && audioSource != null && audioSource.gameObject.activeSelf)
				{
					audioSource.gameObject.SetActive(value: false);
					disableTimer = 0f;
				}
			}
			wasMenuOpenLastFrame = flag;
			yield return null;
		}
	}

	private void SetupUIListeners()
	{
		if (audioSource == null)
		{
			return;
		}
		Button[] componentsInChildren = GetComponentsInChildren<Button>(includeInactive: true);
		foreach (Button button in componentsInChildren)
		{
			if (button.GetComponent<ButtonLinker>() == null)
			{
				button.onClick.AddListener(delegate
				{
					PlaySound(buttonSounds, buttonPitchMin, buttonPitchMax, buttonVolume);
				});
			}
		}
		Toggle[] componentsInChildren2 = GetComponentsInChildren<Toggle>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].onValueChanged.AddListener(delegate
			{
				PlaySound(toggleSounds, togglePitchMin, togglePitchMax, toggleVolume);
			});
		}
		Slider[] componentsInChildren3 = GetComponentsInChildren<Slider>(includeInactive: true);
		foreach (Slider slider in componentsInChildren3)
		{
			AddSliderEvents(slider);
		}
		Dropdown[] componentsInChildren4 = GetComponentsInChildren<Dropdown>(includeInactive: true);
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			componentsInChildren4[i].onValueChanged.AddListener(delegate
			{
				PlaySound(dropdownSounds, dropdownPitchMin, dropdownPitchMax, dropdownVolume);
			});
		}
	}

	private void AddSliderEvents(Slider slider)
	{
		EventTrigger obj = slider.gameObject.GetComponent<EventTrigger>() ?? slider.gameObject.AddComponent<EventTrigger>();
		EventTrigger.Entry entry = new EventTrigger.Entry
		{
			eventID = EventTriggerType.PointerDown
		};
		entry.callback.AddListener(delegate
		{
			if (!activeSliders.Contains(slider))
			{
				activeSliders.Add(slider);
				PlaySound(sliderSounds, sliderPitchMin, sliderPitchMax, sliderVolume);
			}
		});
		obj.triggers.Add(entry);
		EventTrigger.Entry entry2 = new EventTrigger.Entry
		{
			eventID = EventTriggerType.PointerUp
		};
		entry2.callback.AddListener(delegate
		{
			activeSliders.Remove(slider);
		});
		obj.triggers.Add(entry2);
	}

	private void PlaySound(List<AudioClip> clips, float pitchMin, float pitchMax, float volume)
	{
		if (clips != null && clips.Count != 0 && !(audioSource == null) && audioSource.gameObject.activeSelf)
		{
			float num = 1f;
			if (SaveLoadHandler.Instance != null)
			{
				num = SaveLoadHandler.Instance.data.menuVolume;
			}
			float num2 = volume * num;
			if (!(num2 <= 0f))
			{
				audioSource.pitch = Random.Range(pitchMin, pitchMax);
				audioSource.PlayOneShot(clips[Random.Range(0, clips.Count)], num2);
			}
		}
	}

	public void PlayOpenSound()
	{
		if (audioSource != null && !audioSource.gameObject.activeSelf)
		{
			audioSource.gameObject.SetActive(value: true);
		}
		PlaySound(openMenuSounds, openMenuPitchMin, openMenuPitchMax, openMenuVolume);
	}

	public void PlayCloseSound()
	{
		if (audioSource != null && !audioSource.gameObject.activeSelf)
		{
			audioSource.gameObject.SetActive(value: true);
		}
		PlaySound(closeMenuSounds, closeMenuPitchMin, closeMenuPitchMax, closeMenuVolume);
	}

	public void PlayButtonSound()
	{
		if (audioSource != null && !audioSource.gameObject.activeSelf)
		{
			audioSource.gameObject.SetActive(value: true);
		}
		PlaySound(buttonSounds, buttonPitchMin, buttonPitchMax, buttonVolume);
	}
}
