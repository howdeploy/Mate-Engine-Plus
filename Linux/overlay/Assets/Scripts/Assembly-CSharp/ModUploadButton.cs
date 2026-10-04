using UnityEngine;
using UnityEngine.UI;

public class ModUploadButton : MonoBehaviour
{
	public Button button;

	public string filePath;

	public Slider progressBar;

	public string displayName;

	public string author;

	public bool isNSFW;

	public string thumbnailPath;

	private bool inFlight;

	private void Awake()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
	}

	public void UploadNow()
	{
		if (!inFlight && !(SteamWorkshopHandler.Instance == null) && !string.IsNullOrEmpty(filePath))
		{
			inFlight = true;
			SteamWorkshopHandler.Instance.UploadMod(filePath, displayName, author, isNSFW, thumbnailPath, 0uL, progressBar);
			Invoke("ResetInFlight", 2f);
		}
	}

	private void ResetInFlight()
	{
		inFlight = false;
	}
}
