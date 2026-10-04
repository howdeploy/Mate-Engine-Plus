using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class MobileDemo : MonoBehaviour
	{
		public LLMCharacter llmCharacter;

		public GameObject ChatPanel;

		public InputField playerText;

		public Text AIText;

		public GameObject ErrorText;

		public GameObject DownloadPanel;

		public Scrollbar progressBar;

		public Text progressText;

		private bool onValidateWarning = true;

		private bool onValidateInfo = true;

		private async void Start()
		{
			playerText.onSubmit.AddListener(onInputFieldSubmit);
			playerText.interactable = false;
			await DownloadThenWarmup();
		}

		private async Task DownloadThenWarmup()
		{
			ChatPanel.SetActive(value: false);
			DownloadPanel.SetActive(value: true);
			if (!(await LLM.WaitUntilModelSetup(SetProgress)))
			{
				ErrorText.SetActive(value: true);
				return;
			}
			DownloadPanel.SetActive(value: false);
			ChatPanel.SetActive(value: true);
			await WarmUp();
		}

		private async Task WarmUp()
		{
			AIText.text += "Warming up the model...";
			await llmCharacter.Warmup();
			AIText.text = "";
			AIReplyComplete();
		}

		private void SetProgress(float progress)
		{
			progressText.text = (int)(progress * 100f) + "%";
			progressBar.size = progress;
		}

		private void onInputFieldSubmit(string message)
		{
			playerText.interactable = false;
			AIText.text = "...";
			llmCharacter.Chat(message, SetAIText, AIReplyComplete);
		}

		public void SetAIText(string text)
		{
			AIText.text = text;
		}

		public void AIReplyComplete()
		{
			playerText.interactable = true;
			playerText.Select();
			playerText.text = "";
		}

		public void CancelRequests()
		{
			llmCharacter.CancelRequests();
			AIReplyComplete();
		}

		public void ExitGame()
		{
			Debug.Log("Exit button clicked");
			Application.Quit();
		}

		private void OnValidate()
		{
			if (onValidateWarning && !llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
			{
				Debug.LogWarning("Please select a model in the " + llmCharacter.llm.gameObject.name + " GameObject!");
				onValidateWarning = false;
			}
			if (onValidateInfo)
			{
				Debug.Log("Select 'Download On Start' in the " + llmCharacter.llm.gameObject.name + " GameObject to download the models when the app starts.");
				onValidateInfo = false;
			}
		}
	}
}
