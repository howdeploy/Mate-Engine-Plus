using System;
using System.Collections;
using System.Collections.Generic;
using SFB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileTextMultiple : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public Text output;

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	private void Start()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		string[] array = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", multiselect: true);
		if (array.Length != 0)
		{
			List<string> list = new List<string>(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				list.Add(new Uri(array[i]).AbsoluteUri);
			}
			StartCoroutine(OutputRoutine(list.ToArray()));
		}
	}

	private IEnumerator OutputRoutine(string[] urlArr)
	{
		string outputText = "";
		for (int i = 0; i < urlArr.Length; i++)
		{
			UnityWebRequest request = UnityWebRequest.Get(urlArr[i]);
			yield return request.SendWebRequest();
			if (request.result == UnityWebRequest.Result.Success)
			{
				outputText += request.downloadHandler.text;
			}
			else
			{
				Debug.LogError("[CanvasSampleOpenFileTextMultiple] Failed to load: " + request.error);
			}
		}
		output.text = outputText;
	}
}
