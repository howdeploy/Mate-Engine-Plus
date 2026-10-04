using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
	private static readonly Queue<Action> queue = new Queue<Action>();

	public static void Enqueue(Action a)
	{
		lock (queue)
		{
			queue.Enqueue(a);
		}
	}

	private void Update()
	{
		lock (queue)
		{
			while (queue.Count > 0)
			{
				queue.Dequeue()?.Invoke();
			}
		}
	}
}
