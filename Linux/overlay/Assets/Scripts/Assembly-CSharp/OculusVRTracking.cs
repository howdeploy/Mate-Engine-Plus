using UnityEngine;
using Xamin;

public class OculusVRTracking : MonoBehaviour
{
	public CircleSelector pieMenu;

	[SerializeField]
	private Transform controller;

	public static bool leftHanded { get; private set; }

	private void Start()
	{
		Debug.Log("[IMPORTANT] To use this script you should import the Oculus Plugin and remove the comments below");
		pieMenu.controlType = CircleSelector.ControlType.customVector;
	}
}
