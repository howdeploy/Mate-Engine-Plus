using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;

public class UploadButtonHoldHandler : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	[HideInInspector]
	public AvatarLibraryMenu.AvatarEntry entry;

	[Header("UI References")]
	public Slider progressSlider;

	public TMP_Text labelText;

	public TMP_Text errorText;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip tickSound;

	public AudioClip completeSound;

	[SerializeField]
	private string fallbackUpload = "Upload";

	[SerializeField]
	private string fallbackUpdate = "Update";

	[SerializeField]
	private string fallbackPngMissing = "PNG Missing";

	private Coroutine holdRoutine;

	private bool isHolding;

	private LocalizedString currentLocString;

	private void OnEnable()
	{
		CancelHoldIfRunning();
		SetInteractable(value: true);
		UpdateButtonLabel();
	}

	private void OnDisable()
	{
		CancelHoldIfRunning();
		SetInteractable(value: true);
		UpdateButtonLabel();
		if (currentLocString != null)
		{
			currentLocString.StringChanged -= OnLocalizedChanged;
			currentLocString = null;
		}
	}

	private void Start()
	{
		if (entry != null && !entry.isOwner)
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			UpdateButtonLabel();
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (IsThumbnailMissing() || IsThumbnailTooBig())
		{
			string[] array = StandaloneFileBrowser.OpenFilePanel("Select PNG Thumbnail (Max 700KB)", "", new ExtensionFilter[1]
			{
				new ExtensionFilter("Image", "png")
			}, multiselect: false);
			if (array.Length == 0 || !File.Exists(array[0]))
			{
				return;
			}
			if (new FileInfo(array[0]).Length > 716800)
			{
				if (errorText != null)
				{
					SetErrorByKey("PNG_TOO_BIG", "PNG too big");
				}
				return;
			}
			string text = Path.Combine(Application.persistentDataPath, "Thumbnails");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = Path.GetFileNameWithoutExtension(entry.filePath) + "_thumb.png";
			string text2 = Path.Combine(text, path);
			File.Copy(array[0], text2, overwrite: true);
			entry.thumbnailPath = text2;
			string path2 = Path.Combine(Application.persistentDataPath, "avatars.json");
			if (File.Exists(path2))
			{
				List<AvatarLibraryMenu.AvatarEntry> list = JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(path2));
				AvatarLibraryMenu.AvatarEntry avatarEntry = list.FirstOrDefault((AvatarLibraryMenu.AvatarEntry e) => e.filePath == entry.filePath);
				if (avatarEntry != null)
				{
					avatarEntry.thumbnailPath = text2;
					File.WriteAllText(path2, JsonConvert.SerializeObject(list, Formatting.Indented));
				}
			}
			AvatarLibraryMenu avatarLibraryMenu = Object.FindFirstObjectByType<AvatarLibraryMenu>();
			if (avatarLibraryMenu != null)
			{
				avatarLibraryMenu.ReloadAvatars();
			}
			if (errorText != null)
			{
				errorText.text = "";
			}
			UpdateButtonLabel();
		}
		else if (holdRoutine == null)
		{
			isHolding = true;
			holdRoutine = StartCoroutine(HoldToUpload());
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		isHolding = false;
	}

	private bool IsThumbnailMissing()
	{
		if (!string.IsNullOrEmpty(entry.thumbnailPath))
		{
			return !File.Exists(entry.thumbnailPath);
		}
		return true;
	}

	private bool IsThumbnailTooBig()
	{
		if (string.IsNullOrEmpty(entry.thumbnailPath) || !File.Exists(entry.thumbnailPath))
		{
			return false;
		}
		return new FileInfo(entry.thumbnailPath).Length > 716800;
	}

	private void SetErrorByKey(string key, string fallback)
	{
		if (errorText == null)
		{
			return;
		}
		errorText.text = fallback;
		new LocalizedString("Languages (UI)", key).StringChanged += delegate(string val)
		{
			if (errorText != null)
			{
				errorText.text = val;
			}
		};
	}

	private void SetLabelImmediate(string text)
	{
		if (labelText != null)
		{
			labelText.text = text;
		}
	}

	private void OnLocalizedChanged(string val)
	{
		if (labelText != null)
		{
			labelText.text = val;
		}
	}

	private void SetLabelByKey(string key, string fallback)
	{
		if (!(labelText == null))
		{
			if (currentLocString != null)
			{
				currentLocString.StringChanged -= OnLocalizedChanged;
			}
			labelText.text = fallback;
			currentLocString = new LocalizedString("Languages (UI)", key);
			currentLocString.StringChanged += OnLocalizedChanged;
		}
	}

	private void UpdateButtonLabel()
	{
		if (!(labelText == null))
		{
			if (IsThumbnailMissing() || IsThumbnailTooBig())
			{
				SetLabelByKey("PNG_MISSING", fallbackPngMissing);
			}
			else if (entry != null && entry.steamFileId != 0)
			{
				SetLabelByKey("UPDATE", fallbackUpdate);
			}
			else
			{
				SetLabelByKey("UPLOAD", fallbackUpload);
			}
		}
	}

	private IEnumerator HoldToUpload()
	{
		float duration = 5f;
		float timeHeld = 0f;
		int lastSecond = -1;
		float pitch = 1f;
		bool completed = false;
		SetLabelImmediate(Mathf.CeilToInt(duration).ToString());
		SetInteractable(value: false);
		while (isHolding && timeHeld < duration)
		{
			timeHeld += Time.deltaTime;
			int num = Mathf.CeilToInt(duration - timeHeld);
			if (num != lastSecond)
			{
				lastSecond = num;
				SetLabelImmediate(num.ToString());
				if (audioSource != null && tickSound != null)
				{
					audioSource.pitch = pitch;
					audioSource.PlayOneShot(tickSound);
					pitch += 0.1f;
				}
			}
			yield return null;
		}
		if (timeHeld >= duration && isHolding)
		{
			completed = true;
			SetLabelImmediate("0");
			if (audioSource != null && completeSound != null)
			{
				audioSource.pitch = 1f;
				audioSource.PlayOneShot(completeSound);
			}
			yield return new WaitForSeconds(0.5f);
			if (SteamWorkshopHandler.Instance != null)
			{
				SteamWorkshopHandler.Instance.UploadToWorkshop(entry, progressSlider);
			}
			SetLabelImmediate("Uploaded");
			yield return StartCoroutine(WaitForSteamIdAndRelabel(entry.filePath, 20f));
		}
		if (!completed)
		{
			UpdateButtonLabel();
		}
		SetInteractable(value: true);
		holdRoutine = null;
	}

	private IEnumerator WaitForSteamIdAndRelabel(string filePath, float timeoutSeconds)
	{
		float t = 0f;
		string avatarsJsonPath = Path.Combine(Application.persistentDataPath, "avatars.json");
		for (; t < timeoutSeconds; t += 0.5f)
		{
			try
			{
				if (File.Exists(avatarsJsonPath))
				{
					AvatarLibraryMenu.AvatarEntry avatarEntry = JsonConvert.DeserializeObject<List<AvatarLibraryMenu.AvatarEntry>>(File.ReadAllText(avatarsJsonPath))?.FirstOrDefault((AvatarLibraryMenu.AvatarEntry e) => e.filePath == filePath);
					if (avatarEntry != null && avatarEntry.steamFileId != 0L)
					{
						entry.steamFileId = avatarEntry.steamFileId;
						entry.isSteamWorkshop = avatarEntry.isSteamWorkshop;
						SetLabelByKey("UPDATE", fallbackUpdate);
						yield break;
					}
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(0.5f);
		}
		UpdateButtonLabel();
	}

	private void CancelHoldIfRunning()
	{
		isHolding = false;
		if (holdRoutine != null)
		{
			StopCoroutine(holdRoutine);
			holdRoutine = null;
		}
	}

	private void SetInteractable(bool value)
	{
		Button component = GetComponent<Button>();
		if (component != null)
		{
			component.interactable = value;
		}
	}
}
