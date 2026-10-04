using System;
using System.Collections;
using System.IO;
using SFB;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModUploadHoldHandler : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	[Header("UI")]
	public Slider progressSlider;

	public TMP_Text labelText;

	public TMP_Text errorText;

	public RawImage previewImage;

	[Header("Audio")]
	public AudioSource audioSource;

	public AudioClip completeSound;

	public AudioClip tickSound;

	[Header("Labels")]
	public string fallbackUpload = "Upload";

	public string fallbackUpdate = "Update";

	[Header("Hold")]
	public float holdSeconds = 3f;

	private Button btn;

	private ModUploadButton modBtn;

	private Coroutine holdRoutine;

	private bool holding;

	private const long MaxBytes = 2097152L;

	private void Awake()
	{
		btn = GetComponent<Button>();
		modBtn = GetComponent<ModUploadButton>();
		if (modBtn != null && modBtn.progressBar == null && progressSlider != null)
		{
			modBtn.progressBar = progressSlider;
		}
	}

	private void OnEnable()
	{
		holding = false;
		CancelHold();
		SetInteractable(v: true);
		EnsureLocalThumbLink();
		UpdateLabel();
		ClearError();
		TryLoadPreviewFromPath((modBtn != null) ? modBtn.thumbnailPath : null);
	}

	private void OnDisable()
	{
		holding = false;
		CancelHold();
		SetInteractable(v: true);
		UpdateLabel();
	}

	public void OnPointerDown(PointerEventData e)
	{
		if (!CanUpload())
		{
			SetError("Not ready");
		}
		else if (IsThumbnailMissingOrTooBig())
		{
			if (PickThumbnail(out var savedPath))
			{
				modBtn.thumbnailPath = savedPath;
				TryLoadPreviewFromPath(savedPath);
				ClearError();
				UpdateLabel();
			}
		}
		else if (holdRoutine == null)
		{
			holding = true;
			holdRoutine = StartCoroutine(HoldAndUpload());
		}
	}

	public void OnPointerUp(PointerEventData e)
	{
		holding = false;
	}

	private IEnumerator HoldAndUpload()
	{
		float t = 0f;
		int lastShown = -1;
		SetInteractable(v: false);
		while (holding && t < holdSeconds)
		{
			t += Time.deltaTime;
			int num = Mathf.CeilToInt(holdSeconds - t);
			if (num != lastShown)
			{
				lastShown = num;
				SetLabel((num > 0) ? num.ToString() : "0");
				if (audioSource != null && tickSound != null && num > 0)
				{
					audioSource.PlayOneShot(tickSound);
				}
			}
			yield return null;
		}
		if (holding)
		{
			if (audioSource != null && completeSound != null)
			{
				audioSource.PlayOneShot(completeSound);
			}
			yield return null;
			StartUpload();
		}
		UpdateLabel();
		SetInteractable(v: true);
		holdRoutine = null;
	}

	private void StartUpload()
	{
		ClearError();
		if (modBtn == null || string.IsNullOrEmpty(modBtn.filePath))
		{
			SetError("Missing file");
			return;
		}
		if (IsThumbnailMissingOrTooBig())
		{
			SetError("Thumbnail required (≤2MB)");
			return;
		}
		if (modBtn.progressBar == null && progressSlider != null)
		{
			modBtn.progressBar = progressSlider;
		}
		modBtn.UploadNow();
		SetLabel("Uploading");
	}

	private void UpdateLabel()
	{
		if (!(labelText == null))
		{
			bool flag = ResolveWorkshopIdForPath((modBtn != null) ? modBtn.filePath : null) != 0;
			labelText.text = (flag ? fallbackUpdate : fallbackUpload);
		}
	}

	private void SetLabel(string s)
	{
		if (labelText != null)
		{
			labelText.text = s;
		}
	}

	private void SetError(string s)
	{
		if (errorText != null)
		{
			errorText.text = s;
		}
	}

	private void ClearError()
	{
		if (errorText != null)
		{
			errorText.text = "";
		}
	}

	private bool CanUpload()
	{
		if (btn == null || modBtn == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(modBtn.filePath))
		{
			return false;
		}
		if (!File.Exists(modBtn.filePath))
		{
			return false;
		}
		return true;
	}

	private bool IsThumbnailMissingOrTooBig()
	{
		string text = ((modBtn != null) ? modBtn.thumbnailPath : null);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return true;
		}
		if (new FileInfo(text).Length > 2097152)
		{
			return true;
		}
		string text2 = Path.GetExtension(text).ToLowerInvariant();
		if (!(text2 == ".png") && !(text2 == ".jpg"))
		{
			return !(text2 == ".jpeg");
		}
		return false;
	}

	private bool PickThumbnail(out string savedPath)
	{
		savedPath = null;
		string[] array = StandaloneFileBrowser.OpenFilePanel("Select Thumbnail (PNG/JPG, Max 2MB)", "", new ExtensionFilter[1]
		{
			new ExtensionFilter("Image", "png", "jpg", "jpeg")
		}, multiselect: false);
		if (array == null || array.Length == 0)
		{
			return false;
		}
		string text = array[0];
		if (!File.Exists(text))
		{
			return false;
		}
		if (new FileInfo(text).Length > 2097152)
		{
			SetError("Image too big (>2MB)");
			return false;
		}
		string text2 = Path.GetExtension(text).ToLowerInvariant();
		if (text2 != ".png" && text2 != ".jpg" && text2 != ".jpeg")
		{
			SetError("Unsupported format");
			return false;
		}
		string text3 = Path.Combine(Application.persistentDataPath, "Thumbnails");
		Directory.CreateDirectory(text3);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(modBtn.filePath);
		string text4 = Path.Combine(text3, fileNameWithoutExtension + "_thumb.png");
		try
		{
			File.Copy(text, text4, overwrite: true);
		}
		catch
		{
			SetError("Copy failed");
			return false;
		}
		savedPath = text4;
		return true;
	}

	private void TryLoadPreviewFromPath(string path)
	{
		if (previewImage == null || string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return;
		}
		try
		{
			byte[] data = File.ReadAllBytes(path);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
			texture2D.LoadImage(data);
			previewImage.texture = texture2D;
		}
		catch
		{
		}
	}

	private void EnsureLocalThumbLink()
	{
		if (!(modBtn == null) && !string.IsNullOrEmpty(modBtn.filePath) && (string.IsNullOrEmpty(modBtn.thumbnailPath) || !File.Exists(modBtn.thumbnailPath)))
		{
			string defaultThumbPath = GetDefaultThumbPath();
			if (File.Exists(defaultThumbPath))
			{
				modBtn.thumbnailPath = defaultThumbPath;
			}
		}
	}

	private string GetDefaultThumbPath()
	{
		string text = (string.IsNullOrEmpty(modBtn?.filePath) ? "" : Path.GetFileNameWithoutExtension(modBtn.filePath));
		return Path.Combine(Application.persistentDataPath, "Thumbnails", text + "_thumb.png");
	}

	private void SetInteractable(bool v)
	{
		if (btn != null)
		{
			btn.interactable = v;
		}
	}

	private void CancelHold()
	{
		if (holdRoutine != null)
		{
			StopCoroutine(holdRoutine);
			holdRoutine = null;
		}
	}

	private ulong ResolveWorkshopIdForPath(string localPath)
	{
		try
		{
			if (!SteamManager.Initialized)
			{
				return 0uL;
			}
			if (string.IsNullOrEmpty(localPath))
			{
				return 0uL;
			}
			uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
			if (numSubscribedItems == 0)
			{
				return 0uL;
			}
			PublishedFileId_t[] array = new PublishedFileId_t[numSubscribedItems];
			SteamUGC.GetSubscribedItems(array, numSubscribedItems);
			string fileName = Path.GetFileName(localPath);
			for (int i = 0; i < array.Length; i++)
			{
				if (!SteamUGC.GetItemInstallInfo(array[i], out var _, out var pchFolder, 1024u, out var _) || string.IsNullOrEmpty(pchFolder) || !Directory.Exists(pchFolder))
				{
					continue;
				}
				string[] files = Directory.GetFiles(pchFolder, "*", SearchOption.TopDirectoryOnly);
				for (int j = 0; j < files.Length; j++)
				{
					if (string.Equals(Path.GetFileName(files[j]), fileName, StringComparison.OrdinalIgnoreCase))
					{
						return array[i].m_PublishedFileId;
					}
				}
			}
		}
		catch
		{
		}
		return 0uL;
	}
}
