using LLMUnitySamples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UiTooltip : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerMoveHandler
{
	public RectTransform container;

	public string localizationTable = "Languages (UI)";

	public string locKey = "";

	[TextArea(1, 6)]
	public string tooltipText = "";

	public Material bubbleMaterial;

	public Sprite bubbleSprite;

	public Color bubbleColor = new Color32(29, 29, 73, 255);

	public Color fontColor = Color.white;

	public Font font;

	public int fontSize = 16;

	public int bubbleWidth = 600;

	public float textPadding = 10f;

	public float bubbleSpacing = 10f;

	public Vector2 mouseOffset = Vector2.zero;

	[Range(0f, 5f)]
	public float ShowTooltextIn;

	[Header("Hover Zone")]
	public float expandLeft;

	public float expandRight;

	public float expandTop;

	public float expandBottom;

	public bool drawHoverGizmos = true;

	private static Bubble activeBubble;

	private static UiTooltip owner;

	private Canvas rootCanvas;

	private RectTransform containerRT;

	private RectTransform selfRT;

	private bool hovering;

	private Vector2 lastScreenPos;

	private float hoverStartTime = -1f;
	private bool positioning;

	private bool IsShown
	{
		get
		{
			if (activeBubble != null)
			{
				return owner == this;
			}
			return false;
		}
	}

	private void Awake()
	{
		if (container == null)
		{
			Canvas componentInParent = GetComponentInParent<Canvas>();
			if (componentInParent != null)
			{
				container = componentInParent.transform as RectTransform;
			}
		}
		containerRT = container;
		rootCanvas = ((containerRT != null) ? containerRT.GetComponentInParent<Canvas>() : null);
		selfRT = base.transform as RectTransform;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		lastScreenPos = eventData.position;
		hoverStartTime = Time.unscaledTime;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		hovering = false;
		hoverStartTime = -1f;
		HideTooltip();
	}

	public void OnPointerMove(PointerEventData eventData)
	{
		lastScreenPos = eventData.position;
		if (hovering && IsShown)
		{
			Reposition(lastScreenPos);
		}
	}

	private void Update()
	{
		if (selfRT == null || rootCanvas == null)
		{
			return;
		}
		lastScreenPos = Input.mousePosition;
		bool flag = IsInsideExpandedZone(lastScreenPos);
		if (flag && !hovering)
		{
			hovering = true;
			hoverStartTime = Time.unscaledTime;
			if (owner != null && owner != this)
			{
				owner.HideTooltip();
			}
			if (ShowTooltextIn <= 0f)
			{
				ShowTooltip();
				Reposition(lastScreenPos);
			}
		}
		else if (!flag && hovering)
		{
			hovering = false;
			hoverStartTime = -1f;
			HideTooltip();
		}
		if (hovering && !IsShown && hoverStartTime >= 0f && Time.unscaledTime - hoverStartTime >= ShowTooltextIn)
		{
			ShowTooltip();
			Reposition(lastScreenPos);
		}
		if (hovering && IsShown)
		{
			Reposition(lastScreenPos);
		}
	}

	private void OnRectTransformDimensionsChange()
	{
		if (hovering && IsShown)
		{
			Reposition(lastScreenPos);
		}
	}

	private void ShowTooltip()
	{
		if (containerRT == null)
		{
			return;
		}
		if (owner != null && owner != this)
		{
			owner.HideTooltip();
		}
		HideTooltip();
		BubbleUI ui = new BubbleUI
		{
			sprite = bubbleSprite,
			font = font,
			fontSize = fontSize,
			fontColor = fontColor,
			bubbleColor = bubbleColor,
			bottomPosition = 0f,
			leftPosition = 0f,
			textPadding = textPadding,
			bubbleOffset = bubbleSpacing,
			bubbleWidth = bubbleWidth,
			bubbleHeight = -1f
		};
		activeBubble = new Bubble(containerRT, ui, "UiTooltip", "");
		Image component = activeBubble.GetOuterRectTransform().GetComponent<Image>();
		if (component != null)
		{
			if (bubbleMaterial != null)
			{
				component.material = bubbleMaterial;
			}
			component.type = Image.Type.Sliced;
			component.pixelsPerUnitMultiplier = 2f;
		}
        activeBubble.SetText(ResolveText());
        RectTransform rectTransform = activeBubble.GetRectTransform();
        foreach (Graphic graphic in rectTransform.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
        RectTransform outerRectTransform = activeBubble.GetOuterRectTransform();
		outerRectTransform.offsetMin = new Vector2(-textPadding, outerRectTransform.offsetMin.y);
		Canvas canvas = rectTransform.GetComponent<Canvas>();
		Canvas canvas2 = outerRectTransform.GetComponent<Canvas>();
		if (canvas == null)
		{
			canvas = rectTransform.gameObject.AddComponent<Canvas>();
		}
		if (canvas2 == null)
		{
			canvas2 = outerRectTransform.gameObject.AddComponent<Canvas>();
		}
		canvas.overrideSorting = true;
		canvas.sortingOrder = 3;
		canvas2.overrideSorting = true;
		canvas2.sortingOrder = 2;
		LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
		if (layoutElement == null)
		{
			layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
		}
		layoutElement.ignoreLayout = true;
		LayoutElement layoutElement2 = outerRectTransform.GetComponent<LayoutElement>();
		if (layoutElement2 == null)
		{
			layoutElement2 = outerRectTransform.gameObject.AddComponent<LayoutElement>();
		}
		layoutElement2.ignoreLayout = true;
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		Vector3 localPosition = rectTransform.localPosition;
		localPosition.z = 0f;
		rectTransform.localPosition = localPosition;
		Vector3 localPosition2 = outerRectTransform.localPosition;
		localPosition2.z = 0f;
		outerRectTransform.localPosition = localPosition2;
		owner = this;
	}

	private void OnDisable()
	{
		hovering = false;
		hoverStartTime = -1f;
		HideTooltip();
	}

	private void OnDestroy()
	{
		hovering = false;
		hoverStartTime = -1f;
		HideTooltip();
	}

	private void HideTooltip()
	{
		if (!(owner != this))
		{
			if (activeBubble != null)
			{
				activeBubble.Destroy();
				activeBubble = null;
			}
			owner = null;
			hovering = false;
		}
	}

	private string ResolveText()
	{
		if (!string.IsNullOrEmpty(locKey))
		{
			try
			{
				string localizedString = LocalizationSettings.StringDatabase.GetLocalizedString(localizationTable, locKey, null, FallbackBehavior.UseProjectSettings);
				if (!string.IsNullOrEmpty(localizedString))
				{
					return localizedString;
				}
			}
			catch
			{
			}
		}
		return tooltipText;
	}

	private void Reposition(Vector2 screenPos)
	{
		if (positioning) return;
		positioning = true;
		try
		{
		if (!(owner != this) && activeBubble != null && !(containerRT == null) && !(rootCanvas == null))
		{
			RectTransform rectTransform = activeBubble.GetRectTransform();
			RectTransform outerRectTransform = activeBubble.GetOuterRectTransform();
			Camera cam = ((rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera);
			Rect safe = VisibleScreenRect();
			RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, safe.min, cam, out var minimum);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, safe.max, cam, out var maximum);
			Vector2 available = maximum - minimum;
			if (available.x <= 0 || available.y <= 0) return;
			rectTransform.localScale = Vector3.one;
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
				Mathf.Min(bubbleWidth - 2 * textPadding, Mathf.Max(1, available.x - 2 * textPadding)));
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
			Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(containerRT, rectTransform);
			float scale = Mathf.Min(1, Mathf.Min(available.x / Mathf.Max(1, bounds.size.x), available.y / Mathf.Max(1, bounds.size.y)));
			rectTransform.localScale = Vector3.one * scale;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, screenPos, cam, out var localPoint);
			rectTransform.localPosition = new Vector3(localPoint.x + mouseOffset.x, localPoint.y + mouseOffset.y, 0);
			bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(containerRT, rectTransform);
			float dx = bounds.min.x < minimum.x ? minimum.x - bounds.min.x : bounds.max.x > maximum.x ? maximum.x - bounds.max.x : 0;
			float dy = bounds.min.y < minimum.y ? minimum.y - bounds.min.y : bounds.max.y > maximum.y ? maximum.y - bounds.max.y : 0;
			rectTransform.localPosition += new Vector3(dx, dy, 0);
		}
		}
		finally { positioning = false; }
	}

	private Rect VisibleScreenRect()
	{
		Rect safe = new Rect(8, 8, Mathf.Max(1, Screen.width - 16), Mathf.Max(1, Screen.height - 16));
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(wm.UnityWindow, out var window) || window.width <= 0 || window.height <= 0) return safe;
		var monitor = wm.GetMonitorRectFromPoint(wm.GetMousePosition());
		if (monitor.width <= 0) monitor = wm.GetMonitorRectFromWindow(wm.UnityWindow);
		if (monitor.width <= 0) return safe;
		float sx = (float)Screen.width / window.width, sy = (float)Screen.height / window.height;
		return Rect.MinMaxRect(Mathf.Max(safe.xMin, (monitor.xMin - window.xMin) * sx + 8),
			Mathf.Max(safe.yMin, (window.yMax - monitor.yMax) * sy + 8),
			Mathf.Min(safe.xMax, (monitor.xMax - window.xMin) * sx - 8),
			Mathf.Min(safe.yMax, (window.yMax - monitor.yMin) * sy - 8));
	}

	private bool IsInsideExpandedZone(Vector2 screenPos)
	{
		if (selfRT == null)
		{
			return false;
		}
		Camera cam = ((rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null);
		Vector3[] array = new Vector3[4];
		selfRT.GetWorldCorners(array);
		Vector2 vector = RectTransformUtility.WorldToScreenPoint(cam, array[0]);
		Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(cam, array[1]);
		Vector2 vector3 = RectTransformUtility.WorldToScreenPoint(cam, array[2]);
		Vector2 vector4 = RectTransformUtility.WorldToScreenPoint(cam, array[3]);
		float num = Mathf.Min(vector.x, vector2.x, vector3.x, vector4.x) - expandLeft;
		float num2 = Mathf.Max(vector.x, vector2.x, vector3.x, vector4.x) + expandRight;
		float num3 = Mathf.Min(vector.y, vector2.y, vector3.y, vector4.y) - expandBottom;
		float num4 = Mathf.Max(vector.y, vector2.y, vector3.y, vector4.y) + expandTop;
		if (screenPos.x >= num && screenPos.x <= num2 && screenPos.y >= num3)
		{
			return screenPos.y <= num4;
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
		if (drawHoverGizmos)
		{
			if (selfRT == null)
			{
				selfRT = base.transform as RectTransform;
			}
			Canvas canvas = ((rootCanvas != null) ? rootCanvas : GetComponentInParent<Canvas>());
			Camera cam = ((canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null);
			Vector3[] array = new Vector3[4];
			selfRT.GetWorldCorners(array);
			Vector2 vector = RectTransformUtility.WorldToScreenPoint(cam, array[0]);
			Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(cam, array[1]);
			Vector2 vector3 = RectTransformUtility.WorldToScreenPoint(cam, array[2]);
			Vector2 vector4 = RectTransformUtility.WorldToScreenPoint(cam, array[3]);
			Vector2 screenPoint = new Vector2(Mathf.Min(vector.x, vector2.x, vector3.x, vector4.x) - expandLeft, Mathf.Min(vector.y, vector2.y, vector3.y, vector4.y) - expandBottom);
			Vector2 screenPoint2 = new Vector2(Mathf.Max(vector.x, vector2.x, vector3.x, vector4.x) + expandRight, Mathf.Max(vector.y, vector2.y, vector3.y, vector4.y) + expandTop);
			Vector2 screenPoint3 = new Vector2(screenPoint.x, screenPoint2.y);
			Vector2 screenPoint4 = new Vector2(screenPoint2.x, screenPoint.y);
			RectTransform rect = selfRT;
			RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint, cam, out var worldPoint);
			RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint3, cam, out var worldPoint2);
			RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint2, cam, out var worldPoint3);
			RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint4, cam, out var worldPoint4);
			Gizmos.color = new Color(0f, 1f, 0f, 0.7f);
			Gizmos.DrawLine(worldPoint, worldPoint2);
			Gizmos.DrawLine(worldPoint2, worldPoint3);
			Gizmos.DrawLine(worldPoint3, worldPoint4);
			Gizmos.DrawLine(worldPoint4, worldPoint);
		}
	}
}
