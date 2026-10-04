using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class KnowledgeBaseGame : KnowledgeBaseGameUI
	{
		[Header("Models")]
		public LLMCharacter llmCharacter;

		public RAG rag;

		public int numRAGResults = 3;

		private string ragPath = "KnowledgeBaseGame.zip";

		private Dictionary<string, Dictionary<string, string>> botQuestionAnswers = new Dictionary<string, Dictionary<string, string>>();

		private Dictionary<string, RawImage> botImages = new Dictionary<string, RawImage>();

		private string currentBotName;

		private bool onValidateWarning = true;

		private new async void Start()
		{
			base.Start();
			CheckLLMs(debug: false);
			InitElements();
			await InitRAG();
			InitLLM();
		}

		private void InitElements()
		{
			PlayerText.interactable = false;
			botImages["Butler"] = ButlerImage;
			botImages["Maid"] = MaidImage;
			botImages["Chef"] = ChefImage;
			botQuestionAnswers["Butler"] = LoadQuestionAnswers(ButlerText.text);
			botQuestionAnswers["Maid"] = LoadQuestionAnswers(MaidText.text);
			botQuestionAnswers["Chef"] = LoadQuestionAnswers(ChefText.text);
		}

		private async Task InitRAG()
		{
			await CreateEmbeddings();
			DropdownChange(CharacterSelect.value);
		}

		private void InitLLM()
		{
			PlayerText.text += "Warming up the model...";
			llmCharacter.Warmup(AIReplyComplete);
		}

		public Dictionary<string, string> LoadQuestionAnswers(string questionAnswersText)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = questionAnswersText.Split("\n");
			foreach (string text in array)
			{
				if (!(text == ""))
				{
					string[] array2 = text.Split("|");
					dictionary[array2[0]] = array2[1];
				}
			}
			return dictionary;
		}

		public async Task CreateEmbeddings()
		{
			if (!(await rag.Load(ragPath)))
			{
				throw new Exception("The embeddings could not be found!");
			}
		}

		public async Task<List<string>> Retrieval(string question)
		{
			string[] item = (await rag.Search(question, numRAGResults, currentBotName)).Item1;
			List<string> list = new List<string>();
			string[] array = item;
			foreach (string key in array)
			{
				list.Add(botQuestionAnswers[currentBotName][key]);
			}
			return list;
		}

		public async Task<string> ConstructPrompt(string question)
		{
			List<string> obj = await Retrieval(question);
			string text = "";
			foreach (string item in obj)
			{
				text = text + "\n- " + item;
			}
			return string.Concat("Question: " + question + "\n\n", "Possible Answers: ", text);
		}

		protected override async void OnInputFieldSubmit(string question)
		{
			PlayerText.interactable = false;
			SetAIText("...");
			string query = await ConstructPrompt(question);
			llmCharacter.Chat(query, SetAIText, AIReplyComplete);
		}

		protected override void DropdownChange(int selection)
		{
			if (!string.IsNullOrEmpty(currentBotName))
			{
				botImages[currentBotName].gameObject.SetActive(value: false);
			}
			currentBotName = CharacterSelect.options[selection].text;
			botImages[currentBotName].gameObject.SetActive(value: true);
			Debug.Log($"{currentBotName}: {rag.Count(currentBotName)} phrases available");
			llmCharacter.AIName = currentBotName;
		}

		private void SetAIText(string text)
		{
			AIText.text = text;
		}

		private void AIReplyComplete()
		{
			PlayerText.interactable = true;
			PlayerText.Select();
			PlayerText.text = "";
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

		private void CheckLLM(LLMCaller llmCaller, bool debug)
		{
			if (!llmCaller.remote && llmCaller.llm != null && llmCaller.llm.model == "")
			{
				string message = "Please select a llm model in the " + llmCaller.llm.gameObject.name + " GameObject!";
				if (!debug)
				{
					throw new Exception(message);
				}
				Debug.LogWarning(message);
			}
		}

		private void CheckLLMs(bool debug)
		{
			CheckLLM(rag.search.llmEmbedder, debug);
			CheckLLM(llmCharacter, debug);
		}

		private void OnValidate()
		{
			if (onValidateWarning)
			{
				CheckLLMs(debug: true);
				onValidateWarning = false;
			}
		}
	}
}
