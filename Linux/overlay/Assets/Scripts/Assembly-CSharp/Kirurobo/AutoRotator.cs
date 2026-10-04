using UnityEngine;

namespace Kirurobo
{
	public class AutoRotator : MonoBehaviour
	{
		public float angularVelocity = 90f;

		private Vector3 rotationAxis = Vector3.up;

		private Quaternion initialLocalRotation;

		private void Start()
		{
			initialLocalRotation = base.transform.localRotation;
		}

		private void Update()
		{
			Quaternion quaternion = Quaternion.Euler(0f, Time.time * angularVelocity, 0f);
			base.transform.localRotation = initialLocalRotation * quaternion;
		}
	}
}
