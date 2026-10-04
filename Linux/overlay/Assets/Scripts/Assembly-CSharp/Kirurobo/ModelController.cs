using System;
using UnityEngine;

namespace Kirurobo
{
	public class ModelController : MonoBehaviour
	{
		[Flags]
		public enum RotationAxes
		{
			None = 0,
			Pitch = 1,
			Yaw = 2,
			PitchAndYaw = 3
		}

		[Flags]
		public enum DragState
		{
			None = 0,
			Rotating = 1,
			Translating = 2
		}

		public RotationAxes axes = RotationAxes.PitchAndYaw;

		public float yawSensitivity = 1f;

		public float pitchSensitvity = 1f;

		public float scaleSensitivity = 0.5f;

		public Vector2 minimumAngles = new Vector2(-90f, -360f);

		public Vector2 maximumAngles = new Vector2(90f, 360f);

		[Tooltip("Restrict to move out from screen")]
		public bool confineTranslation = true;

		[Tooltip("Default is the parent transform")]
		public Transform centerTransform;

		[Tooltip("Default is the main camera")]
		public Camera currentCamera;

		internal GameObject centerObject;

		internal Vector3 rotation;

		internal Vector3 translation;

		internal Vector3 lastMousePosition;

		internal DragState dragState;

		internal Vector3 relativePosition;

		internal Quaternion relativeRotation;

		internal Vector3 originalLocalScale;

		internal float zoom;

		private void Start()
		{
			Initialize();
			SetupTransform();
		}

		private void OnDestroy()
		{
			if ((bool)centerObject)
			{
				UnityEngine.Object.Destroy(centerObject);
			}
		}

		private void Update()
		{
			if (currentCamera.isActiveAndEnabled)
			{
				HandleMouse();
			}
		}

		internal void Initialize()
		{
			if (!centerTransform)
			{
				centerTransform = base.transform.parent;
				if (!centerTransform || centerTransform == base.transform)
				{
					centerObject = new GameObject();
					centerTransform = centerObject.transform;
					centerTransform.position = Vector3.zero;
				}
			}
			if (!currentCamera)
			{
				currentCamera = Camera.main;
			}
			lastMousePosition = InputProxy.mousePosition;
		}

		internal void SetupTransform()
		{
			relativePosition = base.transform.position - centerTransform.position;
			relativeRotation = base.transform.rotation * Quaternion.Inverse(centerTransform.rotation);
			originalLocalScale = base.transform.localScale;
			ResetTransform();
		}

		public void ResetTransform()
		{
			rotation = relativeRotation.eulerAngles;
			translation = relativePosition;
			zoom = 0f;
			UpdateTransform();
		}

		internal void UpdateTransform()
		{
			Quaternion quaternion = Quaternion.Euler(rotation);
			base.transform.rotation = quaternion;
			base.transform.position = centerTransform.position + translation;
			base.transform.localScale = originalLocalScale * Mathf.Pow(10f, zoom);
		}

		internal virtual void HandleMouse()
		{
			Vector3 vector = InputProxy.mousePosition;
			if (InputProxy.GetMouseButtonDown(0))
			{
				if (dragState == DragState.None && IsHit(vector))
				{
					dragState = DragState.Translating;
					if (confineTranslation)
					{
						vector = Vector3.Max(Vector3.Min(rhs: new Vector3(Screen.width, Screen.height), lhs: vector), Vector3.zero);
					}
					lastMousePosition = vector;
				}
			}
			else if (InputProxy.GetMouseButtonDown(1) && dragState == DragState.None && IsHit(vector))
			{
				dragState = DragState.Rotating;
				lastMousePosition = vector;
			}
			if (dragState == DragState.Rotating)
			{
				if (InputProxy.GetMouseButton(1))
				{
					if ((axes & RotationAxes.Yaw) > RotationAxes.None)
					{
						rotation.y -= (vector.x - lastMousePosition.x) * 360f / (float)Screen.width * yawSensitivity;
						rotation.y = ClampAngle(rotation.y, minimumAngles.y, maximumAngles.y);
					}
					if ((axes & RotationAxes.Pitch) > RotationAxes.None)
					{
						rotation.x += (vector.y - lastMousePosition.y) * 360f / (float)Screen.height * pitchSensitvity;
						rotation.x = ClampAngle(rotation.x, minimumAngles.x, maximumAngles.x);
					}
					UpdateTransform();
				}
				else
				{
					dragState = DragState.None;
				}
			}
			if (dragState == DragState.Translating)
			{
				if (InputProxy.GetMouseButton(0))
				{
					if (confineTranslation)
					{
						vector = Vector3.Max(Vector3.Min(rhs: new Vector3(Screen.width, Screen.height), lhs: vector), Vector3.zero);
					}
					Vector3 vector2 = currentCamera.WorldToScreenPoint(base.transform.position);
					Vector3 vector3 = vector - lastMousePosition;
					vector3.z = 0f;
					Vector3 vector4 = currentCamera.ScreenToWorldPoint(vector2 + vector3);
					translation = vector4 - centerTransform.position;
					UpdateTransform();
				}
				else
				{
					dragState = DragState.None;
				}
			}
			if (!Mathf.Approximately(Input.GetAxis("Mouse ScrollWheel"), 0f) && IsHit(vector))
			{
				float num = Input.GetAxis("Mouse ScrollWheel") * scaleSensitivity;
				zoom -= num;
				zoom = Mathf.Clamp(zoom, -1f, 2f);
				UpdateTransform();
			}
			lastMousePosition = vector;
		}

		internal bool IsHit(Vector3 screenPosition)
		{
			if (Physics.Raycast(currentCamera.ScreenPointToRay(screenPosition), out var hitInfo) && hitInfo.transform.IsChildOf(base.transform))
			{
				return true;
			}
			return false;
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			if (angle < 0f - min)
			{
				angle = 0f - (0f - angle) % 360f;
			}
			if (angle > max)
			{
				angle %= 360f;
			}
			return Mathf.Clamp(angle, min, max);
		}
	}
}
