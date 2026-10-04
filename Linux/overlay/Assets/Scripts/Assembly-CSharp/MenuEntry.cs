using System;
using UnityEngine;

[Serializable]
public class MenuEntry
{
	public GameObject menu;

	public bool blockMovement = true;

	public bool blockHandTracking;

	public bool blockReaction;

	public bool blockChibiMode;
}
