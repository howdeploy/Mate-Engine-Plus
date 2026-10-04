using UnityEngine;

public class ResetPosition : MonoBehaviour
{
	public float distanceToReset = 5f;

	private Vector3 startPosition;

	private void Start()
	{
		startPosition = base.transform.position;
	}

	private void Update()
	{
		if (Vector3.Distance(startPosition, base.transform.position) >= distanceToReset)
		{
			base.transform.position = startPosition;
		}
	}
}
