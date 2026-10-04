using UnityEngine;

public class Rotate : MonoBehaviour
{
	[Range(-200f, 200f)]
	public float speed = 10f;

	public Transform center;

	public Transform lookAt;

	private void Start()
	{
	}

	private void Update()
	{
		base.transform.RotateAround(center.position, Vector3.up, speed * Time.deltaTime);
		if (lookAt != null)
		{
			base.transform.LookAt(lookAt, Vector3.up);
		}
	}
}
