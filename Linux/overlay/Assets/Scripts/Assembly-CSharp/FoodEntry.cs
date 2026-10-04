using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodEntry
{
	public string id;

	public GameObject obj;

	[Range(0.01f, 2f)]
	public float spawnDuration = 0.2f;

	[Range(0.01f, 2f)]
	public float despawnDuration = 0.15f;

	public List<AudioClip> spawnClips = new List<AudioClip>();

	public List<AudioClip> spawnLayerClips = new List<AudioClip>();

	public List<AudioClip> despawnClips = new List<AudioClip>();

	public List<AudioClip> interactClips = new List<AudioClip>();

	public Vector3 worldOffset = Vector3.zero;

	public bool followMouse = true;

	[Range(-3f, 3f)]
	public float minPitch = 0.95f;

	[Range(-3f, 3f)]
	public float maxPitch = 1.05f;
}
