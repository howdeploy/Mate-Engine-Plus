using System;
using UnityEngine;

[Serializable]
public class ChibiSettingsData
{
	public Vector3 chibiArmatureScale = new Vector3(0.3f, 0.3f, 0.3f);

	public Vector3 chibiHeadScale = new Vector3(2.7f, 2.7f, 2.7f);

	public Vector3 chibiUpperLegScale = new Vector3(0.6f, 0.6f, 0.6f);

	public float screenInteractionRadius = 30f;

	public float holdDuration = 2f;
}
