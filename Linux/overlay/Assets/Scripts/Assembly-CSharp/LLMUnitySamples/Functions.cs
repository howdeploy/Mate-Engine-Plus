using System;

namespace LLMUnitySamples
{
	public static class Functions
	{
		private static Random random = new Random();

		public static string Weather()
		{
			string[] array = new string[4] { "sunny", "rainy", "cloudy", "snowy" };
			return "The weather is " + array[random.Next(array.Length)];
		}

		public static string Time()
		{
			return "The time is " + random.Next(24).ToString("D2") + ":" + random.Next(60).ToString("D2");
		}

		public static string Emotion()
		{
			string[] array = new string[4] { "happy", "sad", "exhilarated", "ok" };
			return "I am feeling " + array[random.Next(array.Length)];
		}
	}
}
