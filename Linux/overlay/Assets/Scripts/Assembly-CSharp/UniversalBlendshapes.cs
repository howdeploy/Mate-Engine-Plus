using System.Collections.Generic;
using UniVRM10;
using UnityEngine;
using VRM;

[DisallowMultipleComponent]
public class UniversalBlendshapes : MonoBehaviour
{
	private class BlendState
	{
		public float value;

		public float lastInput;

		public float lastUpdateTime;

		public float holdUntil;
	}

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float Blink;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float Blink_L;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float Blink_R;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float LookUp;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float LookDown;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float LookLeft;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float LookRight;

	[Header("Universal Preview")]
	[Range(0f, 1f)]
	public float Neutral;

	[Range(0f, 1f)]
	public float A;

	[Range(0f, 1f)]
	public float I;

	[Range(0f, 1f)]
	public float U;

	[Range(0f, 1f)]
	public float E;

	[Range(0f, 1f)]
	public float O;

	[Range(0f, 1f)]
	public float Joy;

	[Range(0f, 1f)]
	public float Angry;

	[Range(0f, 1f)]
	public float Sorrow;

	[Range(0f, 1f)]
	public float Fun;

	public float fadeSpeed = 5f;

	public float safeTimeout = 2f;

	public float minHoldTime = 0.1f;

	private VRMBlendShapeProxy proxy0;

	private Vrm10Instance vrm1;

	private Vrm10RuntimeExpression expr1;

	private readonly Dictionary<string, BlendState> states = new Dictionary<string, BlendState>();

	private readonly List<KeyValuePair<BlendShapeKey, float>> reusableList = new List<KeyValuePair<BlendShapeKey, float>>();

	private static readonly string[] keys = new string[17]
	{
		"Blink", "Blink_L", "Blink_R", "LookUp", "LookDown", "LookLeft", "LookRight", "Neutral", "A", "I",
		"U", "E", "O", "Joy", "Angry", "Sorrow", "Fun"
	};

	private static readonly BlendShapePreset[] vrm0Presets = new BlendShapePreset[17]
	{
		BlendShapePreset.Blink,
		BlendShapePreset.Blink_L,
		BlendShapePreset.Blink_R,
		BlendShapePreset.LookUp,
		BlendShapePreset.LookDown,
		BlendShapePreset.LookLeft,
		BlendShapePreset.LookRight,
		BlendShapePreset.Neutral,
		BlendShapePreset.A,
		BlendShapePreset.I,
		BlendShapePreset.U,
		BlendShapePreset.E,
		BlendShapePreset.O,
		BlendShapePreset.Joy,
		BlendShapePreset.Angry,
		BlendShapePreset.Sorrow,
		BlendShapePreset.Fun
	};

	private static readonly Dictionary<string, string> vrm10KeyMap = new Dictionary<string, string>
	{
		{ "A", "aa" },
		{ "I", "ih" },
		{ "U", "ou" },
		{ "E", "ee" },
		{ "O", "oh" },
		{ "Joy", "happy" },
		{ "Angry", "angry" },
		{ "Sorrow", "sad" },
		{ "Fun", "relaxed" },
		{ "Blink", "blink" },
		{ "Blink_L", "blinkLeft" },
		{ "Blink_R", "blinkRight" },
		{ "LookUp", "lookUp" },
		{ "LookDown", "lookDown" },
		{ "LookLeft", "lookLeft" },
		{ "LookRight", "lookRight" },
		{ "Neutral", "neutral" }
	};

	private readonly Dictionary<string, ExpressionKey> vrm1ExpressionKeyMap = new Dictionary<string, ExpressionKey>();

	private readonly float[] valueCache = new float[keys.Length];

	private void Awake()
	{
		proxy0 = GetComponent<VRMBlendShapeProxy>();
		vrm1 = GetComponentInChildren<Vrm10Instance>(includeInactive: true);
		expr1 = ((!(vrm1 != null)) ? null : vrm1.Runtime?.Expression);
		for (int i = 0; i < keys.Length; i++)
		{
			states[keys[i]] = new BlendState();
		}
		if (expr1 == null)
		{
			return;
		}
		vrm1ExpressionKeyMap.Clear();
		foreach (ExpressionKey expressionKey in expr1.ExpressionKeys)
		{
			if (!vrm1ExpressionKeyMap.ContainsKey(expressionKey.Name))
			{
				vrm1ExpressionKeyMap[expressionKey.Name] = expressionKey;
			}
		}
	}

	private void LateUpdate()
	{
		float time = Time.time;
		float deltaTime = Time.deltaTime;
		for (int i = 0; i < keys.Length; i++)
		{
			string key = keys[i];
			UpdateState(key, valueCache[i] = GetInputValue(i), time, deltaTime);
		}
		if (proxy0 != null)
		{
			reusableList.Clear();
			for (int j = 0; j < keys.Length; j++)
			{
				reusableList.Add(new KeyValuePair<BlendShapeKey, float>(BlendShapeKey.CreateFromPreset(vrm0Presets[j]), states[keys[j]].value));
			}
			proxy0.SetValues(reusableList);
			proxy0.Apply();
		}
		else
		{
			if (expr1 == null)
			{
				return;
			}
			for (int k = 0; k < keys.Length; k++)
			{
				string text = keys[k];
				if (!vrm10KeyMap.TryGetValue(text, out var value))
				{
					value = text;
				}
				if (vrm1ExpressionKeyMap.TryGetValue(value, out var value2))
				{
					expr1.SetWeight(value2, states[text].value);
				}
			}
		}
	}

	private float GetInputValue(int i)
	{
		return i switch
		{
			0 => Blink, 
			1 => Blink_L, 
			2 => Blink_R, 
			3 => LookUp, 
			4 => LookDown, 
			5 => LookLeft, 
			6 => LookRight, 
			7 => Neutral, 
			8 => A, 
			9 => I, 
			10 => U, 
			11 => E, 
			12 => O, 
			13 => Joy, 
			14 => Angry, 
			15 => Sorrow, 
			16 => Fun, 
			_ => 0f, 
		};
	}

	private void UpdateState(string key, float input, float now, float dt)
	{
		if (states.TryGetValue(key, out var value))
		{
			bool num = !Mathf.Approximately(input, value.lastInput);
			bool flag = !Mathf.Approximately(input, 0f);
			if (num || flag)
			{
				value.lastInput = input;
				value.lastUpdateTime = now;
				value.value = input;
				value.holdUntil = now + minHoldTime;
			}
			else if (now < value.holdUntil)
			{
				value.value = input;
			}
			else if (now - value.lastUpdateTime > safeTimeout)
			{
				value.value = 0f;
			}
			else
			{
				value.value = Mathf.MoveTowards(value.value, 0f, fadeSpeed * dt);
			}
		}
	}
}
