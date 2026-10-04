using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVersionObjects : MonoBehaviour
{
	public int steamAppId;

	public int ttlDays = 14;

	public float retrySeconds = 5f;

	public float maxWaitSeconds = 180f;

	public List<GameObject> steamOnlyObjects = new List<GameObject>();

	public List<GameObject> notSteamObjects = new List<GameObject>();

	private bool lastEntitled;

	private void Start()
	{
		if (!SteamDRM.Initialized)
		{
			SteamDRM.Initialize(steamAppId, ttlDays);
		}
		lastEntitled = SteamDRM.IsEntitled;
		Apply(lastEntitled);
		StartCoroutine(ReinitLoop());
	}

	private void Update()
	{
		bool isEntitled = SteamDRM.IsEntitled;
		if (isEntitled != lastEntitled)
		{
			lastEntitled = isEntitled;
			Apply(isEntitled);
		}
	}

	private IEnumerator ReinitLoop()
	{
		float t = 0f;
		while (!SteamDRM.IsEntitled && t < maxWaitSeconds)
		{
			SteamDRM.TryInitLive(steamAppId, ttlDays);
			yield return new WaitForSeconds(retrySeconds);
			t += retrySeconds;
		}
	}

	private void Apply(bool isSteam)
	{
		for (int i = 0; i < steamOnlyObjects.Count; i++)
		{
			if ((bool)steamOnlyObjects[i])
			{
				steamOnlyObjects[i].SetActive(isSteam);
			}
		}
		for (int j = 0; j < notSteamObjects.Count; j++)
		{
			if ((bool)notSteamObjects[j])
			{
				notSteamObjects[j].SetActive(!isSteam);
			}
		}
	}
}
