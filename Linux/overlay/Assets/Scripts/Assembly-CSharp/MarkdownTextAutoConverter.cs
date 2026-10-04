using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MarkdownTextAutoConverter : MonoBehaviour
{
	[Header("General")]
	[Range(1f, 60f)]
	public int updateEveryXFrames = 10;

	private int frameCounter;

	[Header("Headings")]
	public bool enableHeadingColors = true;

	[Range(30f, 200f)]
	public int heading1Size = 60;

	[Range(30f, 200f)]
	public int heading2Size = 50;

	[Range(30f, 200f)]
	public int heading3Size = 40;

	public Color heading1Color = new Color(1f, 0.6f, 0.95f);

	public Color heading2Color = new Color(0.3f, 1f, 1f);

	public Color heading3Color = new Color(0.9f, 0.9f, 1f);

	[Header("Bold")]
	public bool enableBoldColor;

	public Color boldColor = Color.white;

	[Header("Italic")]
	public bool enableItalicColor;

	public Color italicColor = Color.white;

	[Header("Strikethrough")]
	public bool enableStrikeColor;

	public Color strikeColor = Color.gray;

	private readonly Dictionary<Text, string> rawText = new Dictionary<Text, string>();

	private readonly Dictionary<Text, Color> baseTextColor = new Dictionary<Text, Color>();

	private static readonly Regex h3 = new Regex("^### (.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex h2 = new Regex("^## (.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex h1 = new Regex("^# (.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex boldItalic = new Regex("\\*\\*\\*(.+?)\\*\\*\\*", RegexOptions.Compiled);

	private static readonly Regex bold = new Regex("\\*\\*(.+?)\\*\\*", RegexOptions.Compiled);

	private static readonly Regex italic = new Regex("\\*(.+?)\\*", RegexOptions.Compiled);

	private static readonly Regex strike = new Regex("~~(.+?)~~", RegexOptions.Compiled);

	private static readonly Regex richTextTag = new Regex("<\\s*(b|i|s|color|size|u)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex stripTags = new Regex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);

	private float lastHue = -2f;

	private float lastSat = -2f;

	private void OnEnable()
	{
		frameCounter = 0;
		lastHue = (lastSat = -2f);
	}

	private void Update()
	{
		if (TryGetHueSat(out var h, out var s) && (Mathf.Abs(lastHue - h) > 0.0005f || Mathf.Abs(lastSat - s) > 0.0005f))
		{
			lastHue = h;
			lastSat = s;
			ConvertAllNow();
			frameCounter = 0;
			return;
		}
		frameCounter++;
		if (frameCounter % updateEveryXFrames == 0)
		{
			ConvertAllNow();
			frameCounter = 0;
		}
	}

	public void ConvertAllNow()
	{
		Text[] componentsInChildren = GetComponentsInChildren<Text>(includeInactive: true);
		if (rawText.Count > 0 || baseTextColor.Count > 0)
		{
			List<Text> list = new List<Text>();
			foreach (KeyValuePair<Text, string> item in rawText)
			{
				if (item.Key == null)
				{
					list.Add(item.Key);
				}
			}
			foreach (Text item2 in list)
			{
				rawText.Remove(item2);
			}
			list.Clear();
			foreach (KeyValuePair<Text, Color> item3 in baseTextColor)
			{
				if (item3.Key == null)
				{
					list.Add(item3.Key);
				}
			}
			foreach (Text item4 in list)
			{
				baseTextColor.Remove(item4);
			}
		}
		foreach (Text text in componentsInChildren)
		{
			if (!(text == null))
			{
				text.supportRichText = true;
				if (!baseTextColor.ContainsKey(text))
				{
					baseTextColor[text] = text.color;
				}
				string text2 = text.text ?? "";
				if (!rawText.TryGetValue(text, out var value))
				{
					value = (richTextTag.IsMatch(text2) ? stripTags.Replace(text2, "") : text2);
					rawText[text] = value;
				}
				else if (!richTextTag.IsMatch(text2) && text2 != value)
				{
					value = text2;
					rawText[text] = value;
				}
				text.text = ParseMarkdown(value);
				text.color = HueShiftColor(baseTextColor[text]);
			}
		}
	}

	private string ParseMarkdown(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}
		Color c = HueShiftColor(heading1Color);
		Color c2 = HueShiftColor(heading2Color);
		Color c3 = HueShiftColor(heading3Color);
		Color c4 = HueShiftColor(boldColor);
		Color c5 = HueShiftColor(italicColor);
		Color c6 = HueShiftColor(strikeColor);
		string input2 = input;
		if (enableHeadingColors)
		{
			input2 = h3.Replace(input2, $"<color={ColorToHex(c3)}><size={heading3Size}%><b>$1</b></size></color>");
			input2 = h2.Replace(input2, $"<color={ColorToHex(c2)}><size={heading2Size}%><b>$1</b></size></color>");
			input2 = h1.Replace(input2, $"<color={ColorToHex(c)}><size={heading1Size}%><b>$1</b></size></color>");
		}
		else
		{
			input2 = h3.Replace(input2, $"<size={heading3Size}%><b>$1</b></size>");
			input2 = h2.Replace(input2, $"<size={heading2Size}%><b>$1</b></size>");
			input2 = h1.Replace(input2, $"<size={heading1Size}%><b>$1</b></size>");
		}
		input2 = boldItalic.Replace(input2, "<b><i>$1</i></b>");
		input2 = (enableBoldColor ? bold.Replace(input2, "<color=" + ColorToHex(c4) + "><b>$1</b></color>") : bold.Replace(input2, "<b>$1</b>"));
		input2 = (enableItalicColor ? italic.Replace(input2, "<color=" + ColorToHex(c5) + "><i>$1</i></color>") : italic.Replace(input2, "<i>$1</i>"));
		return enableStrikeColor ? strike.Replace(input2, "<color=" + ColorToHex(c6) + "><s>$1</s></color>") : strike.Replace(input2, "<s>$1</s>");
	}

	private Color HueShiftColor(Color original)
	{
		if (!TryGetHueSat(out var h, out var s))
		{
			return original;
		}
		Color.RGBToHSV(original, out var H, out var S, out var V);
		H = (H + h) % 1f;
		S = Mathf.Clamp01(S * s);
		Color result = Color.HSVToRGB(H, S, V);
		result.a = original.a;
		return result;
	}

	private bool TryGetHueSat(out float h, out float s)
	{
		h = 0f;
		s = 1f;
		if (ThemeManager.Instance != null)
		{
			h = ThemeManager.Instance.hue;
			s = ThemeManager.Instance.saturation;
			return true;
		}
		return false;
	}

	private static string ColorToHex(Color c)
	{
		return "#" + ColorUtility.ToHtmlStringRGB(c);
	}
}
