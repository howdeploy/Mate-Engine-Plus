using LLMUnity;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class RAGAndLLMSample : RAGSample
	{
		public LLMCharacter llmCharacter;

		public Toggle ParaphraseWithLLM;

		protected override async void onInputFieldSubmit(string message)
		{
			playerText.interactable = false;
			AIText.text = "...";
			string[] item = (await rag.Search(message, 1)).Item1;
			string text = item[0];
			if (!ParaphraseWithLLM.isOn)
			{
				AIText.text = text;
				AIReplyComplete();
			}
			else
			{
				llmCharacter.Chat("Paraphrase the following phrase: " + text, base.SetAIText, base.AIReplyComplete);
			}
		}

		public void CancelRequests()
		{
			llmCharacter.CancelRequests();
			AIReplyComplete();
		}

		protected override void CheckLLMs(bool debug)
		{
			base.CheckLLMs(debug);
			CheckLLM(llmCharacter, debug);
		}
	}
}
