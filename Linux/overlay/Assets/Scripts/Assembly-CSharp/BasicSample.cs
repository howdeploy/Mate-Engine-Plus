using SFB;
using UnityEngine;

public class BasicSample : MonoBehaviour
{
	private string _path;

	private void OnGUI()
	{
		GUI.matrix = Matrix4x4.TRS(s: new Vector3((float)Screen.width / 800f, (float)Screen.height / 600f, 1f), pos: Vector3.zero, q: Quaternion.identity);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(20f);
		GUILayout.BeginVertical();
		if (GUILayout.Button("Open File"))
		{
			WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", "", "", multiselect: false));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Async"))
		{
			StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", "", multiselect: false, delegate(string[] paths3)
			{
				WriteResult(paths3);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Multiple"))
		{
			WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", "", "", multiselect: true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Extension"))
		{
			WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", "", "txt", multiselect: true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Directory"))
		{
			WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", Application.dataPath, "", multiselect: true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Filter"))
		{
			ExtensionFilter[] extensions = new ExtensionFilter[3]
			{
				new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
				new ExtensionFilter("Sound Files", "mp3", "wav"),
				new ExtensionFilter("All Files", "*")
			};
			WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, multiselect: true));
		}
		GUILayout.Space(15f);
		if (GUILayout.Button("Open Folder"))
		{
			string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", multiselect: true);
			WriteResult(paths);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open Folder Async"))
		{
			StandaloneFileBrowser.OpenFolderPanelAsync("Select Folder", "", multiselect: true, delegate(string[] paths3)
			{
				WriteResult(paths3);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open Folder Directory"))
		{
			string[] paths2 = StandaloneFileBrowser.OpenFolderPanel("Select Folder", Application.dataPath, multiselect: true);
			WriteResult(paths2);
		}
		GUILayout.Space(15f);
		if (GUILayout.Button("Save File"))
		{
			_path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "", "");
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Async"))
		{
			StandaloneFileBrowser.SaveFilePanelAsync("Save File", "", "", "", delegate(string path)
			{
				WriteResult(path);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Default Name"))
		{
			_path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "MySaveFile", "");
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Default Name Ext"))
		{
			_path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "MySaveFile", "dat");
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Directory"))
		{
			_path = StandaloneFileBrowser.SaveFilePanel("Save File", Application.dataPath, "", "");
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Filter"))
		{
			ExtensionFilter[] extensions2 = new ExtensionFilter[2]
			{
				new ExtensionFilter("Binary", "bin"),
				new ExtensionFilter("Text", "txt")
			};
			_path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "MySaveFile", extensions2);
		}
		GUILayout.EndVertical();
		GUILayout.Space(20f);
		GUILayout.Label(_path);
		GUILayout.EndHorizontal();
	}

	public void WriteResult(string[] paths)
	{
		if (paths.Length != 0)
		{
			_path = "";
			foreach (string text in paths)
			{
				_path = _path + text + "\n";
			}
		}
	}

	public void WriteResult(string path)
	{
		_path = path;
	}
}
