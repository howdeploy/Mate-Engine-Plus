using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CanvasSampleSaveFileText : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public Text output;

	private string _data = "Example text created by StandaloneFileBrowser";

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	private void Start()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	public void OnClick()
	{
		string text = StandaloneFileBrowser.SaveFilePanel("Title", "", "sample", "txt");
		if (!string.IsNullOrEmpty(text))
		{
			File.WriteAllText(text, _data);
		}
	}
}
