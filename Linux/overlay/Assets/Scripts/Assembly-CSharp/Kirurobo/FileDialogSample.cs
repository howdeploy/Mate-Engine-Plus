using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kirurobo
{
	public class FileDialogSample : MonoBehaviour
	{
		public Button openFileButton;

		public Button openMultipleFilesButton;

		public Button saveFileButton;

		public Text messageText;

		private void Start()
		{
			openFileButton.onClick.AddListener(OpenSingleFile);
			openMultipleFilesButton.onClick.AddListener(OpenMultipleFiles);
			saveFileButton.onClick.AddListener(OpenSaveFile);
			messageText.text = "Click a button!";
		}

		private void Update()
		{
		}

		private void OpenSingleFile()
		{
			FilePanel.Settings settings = new FilePanel.Settings
			{
				filters = new FilePanel.Filter[3]
				{
					new FilePanel.Filter("All files", "*"),
					new FilePanel.Filter("Image files (*.png;*.jpg;*.jpeg;*.tiff;*.gif;*.tga)", "png", "jpg", "jpeg", "tiff", "gif", "tga"),
					new FilePanel.Filter("Documents (*.txt;*.rtf;*.doc;*.docx)", "txt", "rtf", "doc", "docx")
				},
				title = "Open a file!",
				initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
			};
			messageText.text = "";
			FilePanel.OpenFilePanel(settings, delegate(string[] files)
			{
				messageText.text = "Open a file\n" + string.Join("\n", files);
			});
		}

		private void OpenMultipleFiles()
		{
			FilePanel.Settings settings = new FilePanel.Settings
			{
				filters = new FilePanel.Filter[3]
				{
					new FilePanel.Filter("Image files (*.png;*.jpg;*.jpeg;*.tiff;*.gif;*.tga)", "png", "jpg", "jpeg", "tiff", "gif", "tga"),
					new FilePanel.Filter("Documents (*.txt;*.rtf;*.doc;*.docx)", "txt", "rtf", "doc", "docx"),
					new FilePanel.Filter("All files", "*")
				},
				flags = FilePanel.Flag.AllowMultipleSelection,
				title = "Open multiple files!",
				initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			};
			messageText.text = "";
			FilePanel.OpenFilePanel(settings, delegate(string[] files)
			{
				messageText.text = "Open multiple files\n" + string.Join("\n", files);
			});
		}

		private void OpenSaveFile()
		{
			FilePanel.Settings settings = new FilePanel.Settings
			{
				filters = new FilePanel.Filter[3]
				{
					new FilePanel.Filter("Text file (*.txt;*.log)", "txt", "log"),
					new FilePanel.Filter("Image files (*.png;*.jpg;*.jpeg;*.tiff;*.gif;*.tga)", "png", "jpg", "jpeg", "tiff", "gif", "tga"),
					new FilePanel.Filter("All files", "*")
				},
				title = "No save is actually performed",
				initialFile = "Test.txt"
			};
			messageText.text = "";
			FilePanel.SaveFilePanel(settings, delegate(string[] files)
			{
				messageText.text = "Selected file\n" + string.Join("\n", files);
			});
		}
	}
}
