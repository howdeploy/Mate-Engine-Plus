using UnityEngine;

public class FixedPosition : MonoBehaviour
{
	private Vector3 fixedPosition;

	private void Start()
	{
		fixedPosition = base.transform.position;
	}

	private void LateUpdate()
	{
		base.transform.position = fixedPosition;
	}
}
