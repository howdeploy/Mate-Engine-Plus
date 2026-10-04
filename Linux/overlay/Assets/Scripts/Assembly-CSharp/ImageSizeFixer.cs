using UnityEngine;

[ExecuteAlways]
public class ImageSizeFixer : MonoBehaviour
{
	public RectTransform startElement;

	public RectTransform endElement;

	public float topOffset = 10f;

	public float bottomOffset = 10f;

	public bool liveInEditor = true;

	private RectTransform self;

	private RectTransform parentRect;

	private Canvas rootCanvas;

	private void OnEnable()
	{
		self = GetComponent<RectTransform>();
		parentRect = ((self != null) ? (self.parent as RectTransform) : null);
		rootCanvas = ((self != null) ? self.GetComponentInParent<Canvas>() : null);
		if (Application.isPlaying)
		{
			base.enabled = false;
		}
		if (!Application.isPlaying && liveInEditor)
		{
			ApplyNow();
		}
	}

	private void OnValidate()
	{
		if (!Application.isPlaying && liveInEditor)
		{
			ApplyNow();
		}
	}

	private void Update()
	{
		if (!Application.isPlaying && liveInEditor)
		{
			ApplyNow();
		}
	}

	[ContextMenu("Apply Now")]
	public void ApplyNow()
	{
		if (!(self == null) && !(parentRect == null) && !(startElement == null) && !(endElement == null))
		{
			Vector3[] array = new Vector3[4];
			Vector3[] array2 = new Vector3[4];
			startElement.GetWorldCorners(array);
			endElement.GetWorldCorners(array2);
			Vector2 vector = WorldToParentLocal(array[1]);
			Vector2 vector2 = WorldToParentLocal(array[2]);
			Vector2 vector3 = WorldToParentLocal(array2[0]);
			Vector2 vector4 = WorldToParentLocal(array2[3]);
			float num = Mathf.Max(vector.y, vector2.y) + topOffset;
			float num2 = Mathf.Min(vector3.y, vector4.y) - bottomOffset;
			if (num2 > num)
			{
				float num3 = num;
				num = num2;
				num2 = num3;
			}
			float size = Mathf.Max(0f, num - num2);
			float num4 = (num + num2) * 0.5f;
			float num5 = Mathf.Lerp(parentRect.rect.yMin, parentRect.rect.yMax, (self.anchorMin.y + self.anchorMax.y) * 0.5f);
			self.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
			self.anchoredPosition = new Vector2(self.anchoredPosition.x, num4 - num5);
		}
	}

	private Vector2 WorldToParentLocal(Vector3 world)
	{
		Camera cam = ((rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, RectTransformUtility.WorldToScreenPoint(cam, world), cam, out var localPoint);
		return localPoint;
	}
}
