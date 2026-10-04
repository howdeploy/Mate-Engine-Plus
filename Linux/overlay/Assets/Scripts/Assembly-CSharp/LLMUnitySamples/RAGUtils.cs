using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LLMUnitySamples
{
	public class RAGUtils
	{
		public static List<string> SplitText(string text, int chunkSize = 300)
		{
			List<string> list = new List<string>();
			int num = 0;
			char[] anyOf = ".!;?\n\r".ToCharArray();
			while (num < text.Length)
			{
				int num2 = Math.Min(num + chunkSize, text.Length);
				if (num2 < text.Length)
				{
					int num3 = text.IndexOfAny(anyOf, num2);
					if (num3 != -1)
					{
						num2 = num3 + 1;
					}
				}
				list.Add(text.Substring(num, num2 - num).Trim());
				num = num2;
			}
			return list;
		}

		public static Dictionary<string, List<string>> ReadGutenbergFile(string text)
		{
			Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>();
			string pattern = "\\[.*?\\]";
			Regex regex = new Regex("^[A-Z and]+\\.$");
			string text2 = null;
			string name = null;
			string text3 = "";
			bool flag = false;
			int num = 0;
			int num2 = 0;
			string[] array = text.Split("\n");
			for (int i = 0; i < array.Length; i++)
			{
				string text4 = array[i];
				if (text4.Contains("***"))
				{
					flag = !flag;
				}
				if (!flag)
				{
					continue;
				}
				text4 = text4.Replace("\r", "");
				text4 = Regex.Replace(text4, pattern, "");
				string text5 = text4.Trim();
				if (text5 == "" || text5.StartsWith("Re-enter ") || text5.StartsWith("Enter ") || text5.StartsWith("SCENE"))
				{
					continue;
				}
				num += text4.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
				num2++;
				if (text4.StartsWith("ACT"))
				{
					AddMessages(text3, text2, name);
					text2 = null;
					name = null;
					text3 = "";
				}
				else if (regex.IsMatch(text4))
				{
					AddMessages(text3, text2, name);
					text3 = "";
					text2 = text4.Replace(".", "");
					if (text2.Contains("and"))
					{
						string[] array2 = text2.Split(" and ");
						text2 = array2[0];
						name = array2[1];
					}
				}
				else if (text2 != null)
				{
					if (text3 != "")
					{
						text3 += " ";
					}
					text3 += text4;
				}
			}
			return messages;
			void AddMessage(string message, string text6)
			{
				if (text6 != null)
				{
					if (!messages.ContainsKey(text6))
					{
						messages[text6] = new List<string>();
					}
					messages[text6].Add(message);
				}
			}
			void AddMessages(string message, string name2, string name3)
			{
				foreach (string item in SplitText(message))
				{
					if (!(item == ""))
					{
						AddMessage(item, name2);
						AddMessage(item, name3);
					}
				}
			}
		}
	}
}
