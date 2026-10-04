using System;
using UnityEngine;

[Serializable]
public class SwingFollowEntry
{
	public string label = "Follow";

	public GameObject menuObject;

	public HumanBodyBones targetBone = HumanBodyBones.Head;

	[Range(0f, 1f)]
	public float smoothness = 0.15f;

	[HideInInspector]
	public Vector2 baseOffset;

	[HideInInspector]
	public bool hasBaseOffset;

	[HideInInspector]
	public Vector2 currentPosition;

	[HideInInspector]
	public float originalY;

	public bool blockYMovement;

	[Range(0f, 100f)]
	[Tooltip("0 - 100")]
	public float yThreshold;
}
