using LLMUnity;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class MultipleCharactersInteraction
	{
		private InputField playerText;

		private Text AIText;

		private LLMCharacter llmCharacter;

		public MultipleCharactersInteraction(InputField playerText, Text AIText, LLMCharacter llmCharacter)
		{
			this.playerText = playerText;
			this.AIText = AIText;
			this.llmCharacter = llmCharacter;
		}

		public void Start()
		{
			playerText.onSubmit.AddListener(onInputFieldSubmit);
			playerText.Select();
		}

		public void onInputFieldSubmit(string message)
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
	}
}
