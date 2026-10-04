using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class RAGSample : MonoBehaviour
	{
		public RAG rag;

		public InputField playerText;

		public Text AIText;

		public TextAsset HamletText;

		private List<string> phrases;

		private string ragPath = "RAGSample.zip";

		private bool onValidateWarning = true;

		private async void Start()
		{
			CheckLLMs(debug: false);
			playerText.interactable = false;
			LoadPhrases();
			await CreateEmbeddings();
			playerText.onSubmit.AddListener(onInputFieldSubmit);
			AIReplyComplete();
		}

		public void LoadPhrases()
		{
			phrases = RAGUtils.ReadGutenbergFile(HamletText.text)["HAMLET"];
		}

		public async Task CreateEmbeddings()
		{
			if (!(await rag.Load(ragPath)))
			{
				throw new Exception("The embeddings could not be found!");
			}
		}

		protected virtual async void onInputFieldSubmit(string message)
		{
			playerText.interactable = false;
			AIText.text = "...";
			string[] item = (await rag.Search(message, 1)).Item1;
			AIText.text = item[0];
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

		public void ExitGame()
		{
			Debug.Log("Exit button clicked");
			Application.Quit();
		}

		protected void CheckLLM(LLMCaller llmCaller, bool debug)
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

		protected virtual void CheckLLMs(bool debug)
		{
			CheckLLM(rag.search.llmEmbedder, debug);
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
