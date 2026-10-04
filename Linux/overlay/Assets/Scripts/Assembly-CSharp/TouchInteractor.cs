using UnityEngine;
using Xamin;

[RequireComponent(typeof(Camera))]
public class TouchInteractor : MonoBehaviour
{
	public CircleSelector menu;

	private Camera _cam;

	private void Start()
	{
		_cam = GetComponent<Camera>();
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			menu.Open(Input.mousePosition);
		}
	}
}
