using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using UnityEngine;

public class MemoryTrim : MonoBehaviour
{
	public bool enableAutoTrim;

	public void SetAutoTrimEnabled(bool enabled)
	{
		enableAutoTrim = enabled;
		CancelInvoke("StartupTrim");
		CancelInvoke("PeriodicTrim");
		if (enableAutoTrim)
		{
			TrimNow();
			Invoke("StartupTrim", 10f);
			InvokeRepeating("PeriodicTrim", 600f, 600f);
		}
	}

	private void DelayedStartupTrim()
	{
		if (enableAutoTrim)
		{
			TrimNow();
		}
	}

	private void Awake()
	{
		if (enableAutoTrim)
		{
			TrimNow();
			Invoke("StartupTrim", 10f);
			InvokeRepeating("PeriodicTrim", 600f, 600f);
			Invoke("DelayedStartupTrim", 15f);
		}
	}

	private void OnDisable()
	{
		CancelInvoke("StartupTrim");
		CancelInvoke("PeriodicTrim");
	}

	public void TrimNow()
	{
		StartCoroutine(TrimRoutine());
	}

	private void StartupTrim()
	{
		TrimNow();
	}

	private void PeriodicTrim()
	{
		TrimNow();
	}

	private IEnumerator TrimRoutine()
	{
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
		AsyncOperation op = Resources.UnloadUnusedAssets();
		while (!op.isDone)
		{
			yield return null;
		}
		TrimWorkingSet();
	}

	private static void TrimWorkingSet()
	{
		malloc_trim(UIntPtr.Zero);
	}

	[DllImport("libc.so.6")]
	private static extern int malloc_trim(UIntPtr pad);
}
