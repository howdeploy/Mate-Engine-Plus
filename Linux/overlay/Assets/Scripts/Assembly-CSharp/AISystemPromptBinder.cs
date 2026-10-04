using System;
using System.IO;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class AISystemPromptBinder : MonoBehaviour
{
	[Header("References")]
	public InputField input;

	public LLMCharacter target;

	[Header("Behavior")]
	public bool liveSave = true;

	private void Reset()
	{
		if (!input)
		{
			input = GetComponent<InputField>();
		}
		if (!target)
		{
			target = UnityEngine.Object.FindObjectOfType<LLMCharacter>();
		}
	}

	private void Awake()
	{
		if (!input)
		{
			input = GetComponent<InputField>();
		}
		string fixedPromptPath = GetFixedPromptPath();
		string text = (target ? target.prompt : "");
		try
		{
			if (File.Exists(fixedPromptPath))
			{
				text = File.ReadAllText(fixedPromptPath);
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(fixedPromptPath));
				File.WriteAllText(fixedPromptPath, text);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[AI Prompt] Read/Create failed: " + ex);
		}
		input.onValueChanged.RemoveListener(OnValueChanged);
		input.onEndEdit.RemoveListener(OnEndEdit);
		input.text = text;
		ApplyToLLM(text);
		input.onValueChanged.AddListener(OnValueChanged);
		input.onEndEdit.AddListener(OnEndEdit);
	}

	private void OnDestroy()
	{
		if (input != null)
		{
			input.onValueChanged.RemoveListener(OnValueChanged);
			input.onEndEdit.RemoveListener(OnEndEdit);
		}
	}

	private void OnValueChanged(string s)
	{
		if (liveSave)
		{
			Save(s);
		}
	}

	private void OnEndEdit(string s)
	{
		if (!liveSave)
		{
			Save(s);
		}
	}

	private void Save(string s)
	{
		string fixedPromptPath = GetFixedPromptPath();
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(fixedPromptPath));
			File.WriteAllText(fixedPromptPath, s);
		}
		catch (Exception ex)
		{
			Debug.LogError("[AI Prompt] Write failed: " + ex);
		}
		ApplyToLLM(s);
	}

	private void ApplyToLLM(string s)
	{
		if (target != null)
		{
			target.SetPrompt(s);
		}
	}

	private static string GetFixedPromptPath()
	{
		return Path.Combine(Application.persistentDataPath, "ZomeAI_prompt.txt");
	}
}
