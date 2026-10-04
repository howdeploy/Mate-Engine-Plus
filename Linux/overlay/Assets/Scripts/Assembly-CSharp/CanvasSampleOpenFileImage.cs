using System;
using System.Collections;
using SFB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileImage : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public RawImage output;

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	private void Start()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		string[] array = StandaloneFileBrowser.OpenFilePanel("Title", "", ".png", multiselect: false);
		if (array.Length != 0)
		{
			StartCoroutine(OutputRoutine(new Uri(array[0]).AbsoluteUri));
		}
	}

	private IEnumerator OutputRoutine(string url)
	{
		UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.Success)
		{
			output.texture = DownloadHandlerTexture.GetContent(request);
		}
		else
		{
			Debug.LogError("[CanvasSampleOpenFileImage] Failed to load texture: " + request.error);
		}
	}
}
