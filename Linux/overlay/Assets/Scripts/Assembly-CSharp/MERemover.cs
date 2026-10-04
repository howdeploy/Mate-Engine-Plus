using System;
using System.Collections.Generic;
using UnityEngine;

public class MERemover : MonoBehaviour
{
	[Serializable]
	public class TargetEntry
	{
		public GameObject target;

		[HideInInspector]
		public bool hasBeenDisabled;
	}

	[SerializeField]
	private List<TargetEntry> targets = new List<TargetEntry>();

	private void Update()
	{
		for (int i = 0; i < targets.Count; i++)
		{
			TargetEntry targetEntry = targets[i];
			if (!targetEntry.hasBeenDisabled && targetEntry.target != null && targetEntry.target.activeSelf)
			{
				targetEntry.target.SetActive(value: false);
				targetEntry.hasBeenDisabled = true;
			}
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < targets.Count; i++)
		{
			TargetEntry targetEntry = targets[i];
			if (targetEntry.hasBeenDisabled && targetEntry.target != null)
			{
				targetEntry.target.SetActive(value: true);
				targetEntry.hasBeenDisabled = false;
			}
		}
	}
}
