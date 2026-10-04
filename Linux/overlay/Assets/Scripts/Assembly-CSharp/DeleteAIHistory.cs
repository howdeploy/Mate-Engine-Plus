using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DeleteAIHistory : MonoBehaviour
{
	[Header("UI Button to delete AI history")]
	public Button deleteButton;

	[Tooltip("Base filename for AI history. Default is 'ZomeAI'.")]
	public string fileName = "ZomeAI";

	private void Start()
	{
		if (deleteButton != null)
		{
			deleteButton.onClick.AddListener(DeleteHistoryFiles);
		}
		else
		{
			Debug.LogWarning("[DeleteAIHistory] Delete Button is not assigned.");
		}
	}

	public void DeleteHistoryFiles()
	{
		string text = Path.Combine(Application.persistentDataPath, fileName + ".json");
		string text2 = Path.Combine(Application.persistentDataPath, fileName + ".cache");
		bool flag = false;
		if (File.Exists(text))
		{
			File.Delete(text);
			Debug.Log("[DeleteAIHistory] Deleted: " + text);
			flag = true;
		}
		if (File.Exists(text2))
		{
			File.Delete(text2);
			Debug.Log("[DeleteAIHistory] Deleted: " + text2);
			flag = true;
		}
		if (!flag)
		{
			Debug.LogWarning("[DeleteAIHistory] No AI history files found at: " + text + " or " + text2);
		}
	}
}
