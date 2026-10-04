using UnityEngine;

namespace uWindowCapture
{
	public class UwcRayCastExample : MonoBehaviour
	{
		[SerializeField]
		private Transform from;

		[SerializeField]
		private Transform to;

		[SerializeField]
		private LayerMask layerMask;

		[SerializeField]
		private Vector2 windowCoord;

		[SerializeField]
		private Vector2 desktopCoord;

		private void Update()
		{
			Vector3 vector = to.position - from.position;
			Vector3 normalized = vector.normalized;
			float magnitude = vector.magnitude;
			RayCastResult rayCastResult = UwcWindowTexture.RayCast(from.position, normalized, magnitude, layerMask);
			if (rayCastResult.hit)
			{
				Debug.DrawLine(from.position, to.position, Color.red);
				Debug.DrawRay(rayCastResult.position, rayCastResult.normal, Color.green);
				windowCoord = rayCastResult.windowCoord;
				desktopCoord = rayCastResult.desktopCoord;
			}
			else
			{
				Debug.DrawLine(from.position, to.position, Color.yellow);
				windowCoord = new Vector2(-1f, -1f);
				desktopCoord = new Vector2(-1f, -1f);
			}
		}
	}
}
