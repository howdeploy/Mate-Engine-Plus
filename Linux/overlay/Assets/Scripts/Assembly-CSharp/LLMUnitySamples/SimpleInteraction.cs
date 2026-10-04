using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class SimpleInteraction : MonoBehaviour
	{
		public LLMCharacter llmCharacter;

		public InputField playerText;

		public Text AIText;

		private bool onValidateWarning = true;

		private void Start()
		{
			playerText.onSubmit.AddListener(onInputFieldSubmit);
			playerText.Select();
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
		}
	}
}
