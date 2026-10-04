using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class MultipleCharacters : MonoBehaviour
	{
		public LLMCharacter llmCharacter1;

		public InputField playerText1;

		public Text AIText1;

		private MultipleCharactersInteraction interaction1;

		public LLMCharacter llmCharacter2;

		public InputField playerText2;

		public Text AIText2;

		private MultipleCharactersInteraction interaction2;

		private bool onValidateWarning = true;

		private void Start()
		{
			interaction1 = new MultipleCharactersInteraction(playerText1, AIText1, llmCharacter1);
			interaction2 = new MultipleCharactersInteraction(playerText2, AIText2, llmCharacter2);
			interaction1.Start();
			interaction2.Start();
		}

		public void CancelRequests()
		{
			llmCharacter1.CancelRequests();
			llmCharacter2.CancelRequests();
			interaction1.AIReplyComplete();
			interaction2.AIReplyComplete();
		}

		public void ExitGame()
		{
			Debug.Log("Exit button clicked");
			Application.Quit();
		}

		private void OnValidate()
		{
			if (onValidateWarning && !llmCharacter1.remote && llmCharacter1.llm != null && llmCharacter1.llm.model == "")
			{
				Debug.LogWarning("Please select a model in the " + llmCharacter1.llm.gameObject.name + " GameObject!");
				onValidateWarning = false;
			}
		}
	}
}
