using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollHelper : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Header("Smoothness (0 = instant, 1 = ultra slow)")]
	[Range(0f, 1f)]
	public float smoothFactor = 0.1f;

	[Header("Speed")]
	[Tooltip("Wieviele Pixel pro Mausrad-Notch gescrollt werden (unabhängig von der Contentgröße).")]
	public float pixelsPerNotch = 80f;

	[Tooltip("Optionaler Multiplikator für Touchpads/hohe Auflösung (wirkt auf Input.GetAxis-Wert).")]
	public float inputMultiplier = 1f;

	private ScrollRect scrollRect;

	private RectTransform contentRT;

	private RectTransform viewportRT;

	private bool isPointerOver;

	private float pixelVelocity;

	private void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
		contentRT = scrollRect.content;
		viewportRT = ((scrollRect.viewport != null) ? scrollRect.viewport : GetComponent<RectTransform>());
		scrollRect.inertia = false;
	}

	private void Update()
	{
		if (contentRT == null || viewportRT == null)
		{
			return;
		}
		if (isPointerOver)
		{
			float axis = Input.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(axis) > 0.0001f)
			{
				pixelVelocity += axis * pixelsPerNotch * inputMultiplier;
			}
		}
		if (Mathf.Abs(pixelVelocity) > 0.001f)
		{
			float num = Mathf.Max(1f, contentRT.rect.height - viewportRT.rect.height);
			float num2 = pixelVelocity / num;
			float value = scrollRect.verticalNormalizedPosition + num2;
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(value);
			float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(smoothFactor), Time.unscaledDeltaTime * 60f);
			pixelVelocity = Mathf.Lerp(pixelVelocity, 0f, t);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		isPointerOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		isPointerOver = false;
	}
}
