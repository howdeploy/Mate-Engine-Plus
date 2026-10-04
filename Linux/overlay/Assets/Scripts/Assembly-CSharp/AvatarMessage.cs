using System;
using UnityEngine;

[Serializable]
public class AvatarMessage
{
	[TextArea(1, 3)]
	public string text = "Hello!";

	public string locKey = "";

	public string state = "Idle";

	public bool onActive;

	public bool isHusbando;
}
