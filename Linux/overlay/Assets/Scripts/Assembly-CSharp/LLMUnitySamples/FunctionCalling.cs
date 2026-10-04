using System.Collections.Generic;
using System.Reflection;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class FunctionCalling : MonoBehaviour
	{
		public LLMCharacter llmCharacter;

		public InputField playerText;

		public Text AIText;

		private bool onValidateWarning = true;

		private void Start()
		{
			playerText.onSubmit.AddListener(onInputFieldSubmit);
			playerText.Select();
			llmCharacter.grammarString = MultipleChoiceGrammar();
		}

		private string[] GetFunctionNames()
		{
			List<string> list = new List<string>();
			MethodInfo[] methods = typeof(Functions).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				list.Add(methodInfo.Name);
			}
			return list.ToArray();
		}

		private string MultipleChoiceGrammar()
		{
			return "root ::= (\"" + string.Join("\" | \"", GetFunctionNames()) + "\")";
		}

		private string ConstructPrompt(string message)
		{
			string text = "Which of the following choices matches best the input?\n\n";
			text = text + "Input:" + message + "\n\n";
			text += "Choices:\n";
			string[] functionNames = GetFunctionNames();
			foreach (string text2 in functionNames)
			{
				text = text + "- " + text2 + "\n";
			}
			return text + "\nAnswer directly with the choice";
		}

		private string CallFunction(string functionName)
		{
			return (string)typeof(Functions).GetMethod(functionName).Invoke(null, null);
		}

		private async void onInputFieldSubmit(string message)
		{
			playerText.interactable = false;
			string text = await llmCharacter.Chat(ConstructPrompt(message));
			string text2 = CallFunction(text);
			AIText.text = "Calling " + text + "\n" + text2;
			playerText.interactable = true;
		}

		public void CancelRequests()
		{
			llmCharacter.CancelRequests();
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
