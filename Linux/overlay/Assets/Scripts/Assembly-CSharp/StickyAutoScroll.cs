using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class StickyAutoScroll : MonoBehaviour
{
	[Header("Refs")]
	public ScrollRect scrollRect;

	public RectTransform content;

	public RectTransform viewport;

	[Header("Behaviour")]
	public bool forceAlways = true;

	[Range(0f, 1f)]
	public float bottomTolerance = 0.05f;

	[Header("Smoothing")]
	public float smoothDuration = 0.25f;

	public float settleTime = 0.08f;

	private float _lastHeight = -1f;

	private int _lastChildCount = -1;

	private float _settleTimer;

	private Coroutine _settleCo;

	private Coroutine _smoothCo;

	private void Reset()
	{
		scrollRect = GetComponentInParent<ScrollRect>();
		if (scrollRect != null)
		{
			content = scrollRect.content;
			viewport = scrollRect.viewport;
		}
	}

	private void Awake()
	{
		if (!scrollRect)
		{
			scrollRect = GetComponentInParent<ScrollRect>();
		}
		if ((bool)scrollRect)
		{
			if (!content)
			{
				content = scrollRect.content;
			}
			if (!viewport)
			{
				viewport = scrollRect.viewport;
			}
		}
	}

	private void OnEnable()
	{
		if (base.isActiveAndEnabled)
		{
			StartCoroutine(ScrollAfterLayout());
		}
	}

	private IEnumerator ScrollAfterLayout()
	{
		yield return null;
		Canvas.ForceUpdateCanvases();
		yield return null;
		Canvas.ForceUpdateCanvases();
		SmoothToBottom();
	}

	private void Update()
	{
		if (!scrollRect || !content || !viewport)
		{
			return;
		}
		float height = content.rect.height;
		int childCount = content.childCount;
		if (!Mathf.Approximately(height, _lastHeight) || childCount != _lastChildCount)
		{
			_settleTimer = settleTime;
			if (_settleCo == null)
			{
				_settleCo = StartCoroutine(SettleThenStick());
			}
		}
		_lastHeight = height;
		_lastChildCount = childCount;
	}

	private IEnumerator SettleThenStick()
	{
		while (_settleTimer > 0f)
		{
			_settleTimer -= Time.unscaledDeltaTime;
			yield return null;
		}
		_settleCo = null;
		if (forceAlways || IsAtBottom(bottomTolerance))
		{
			SmoothToBottom();
		}
	}

	private bool IsAtBottom(float tol)
	{
		return scrollRect.verticalNormalizedPosition <= tol;
	}

	private void SmoothToBottom()
	{
		if (_smoothCo != null)
		{
			StopCoroutine(_smoothCo);
		}
		_smoothCo = StartCoroutine(SmoothContentToBottom());
	}

	private IEnumerator SmoothContentToBottom()
	{
		if ((bool)content && (bool)viewport)
		{
			Canvas.ForceUpdateCanvases();
			float height = viewport.rect.height;
			float height2 = content.rect.height;
			float num = Mathf.Max(0f, height2 - height);
			float startY = content.anchoredPosition.y;
			float dur = Mathf.Max(0.01f, smoothDuration);
			float t = 0f;
			while (t < dur)
			{
				t += Time.unscaledDeltaTime;
				float num2 = t / dur;
				float t2 = 1f - Mathf.Pow(1f - num2, 3f);
				float y = Mathf.Lerp(startY, num, t2);
				content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
				yield return null;
				Canvas.ForceUpdateCanvases();
				height = viewport.rect.height;
				height2 = content.rect.height;
				num = Mathf.Max(0f, height2 - height);
			}
			content.anchoredPosition = new Vector2(content.anchoredPosition.x, num);
			_smoothCo = null;
		}
	}

	public void JumpToBottomImmediate()
	{
		if ((bool)content && (bool)viewport)
		{
			Canvas.ForceUpdateCanvases();
			float y = Mathf.Max(0f, content.rect.height - viewport.rect.height);
			content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
		}
	}
}
