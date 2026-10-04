using UnityEngine;
using UnityEngine.UI;

public class AvatarScaleController : MonoBehaviour
{
	[Header("UI")]
	[SerializeField]
	private Slider avatarSizeSlider;

	[Header("Scroll Settings")]
	[SerializeField]
	private float scrollSensitivity = 0.1f;

	[SerializeField]
	private float smoothFactor = 0.1f;

	private float minSize;

	private float maxSize;

	private float targetSize;

	private Transform modelRoot;

	private GameObject currentModel;

	private AvatarAnimatorController controller;

	private void Start()
	{
		if (!(avatarSizeSlider == null))
		{
			minSize = avatarSizeSlider.minValue;
			maxSize = avatarSizeSlider.maxValue;
			targetSize = avatarSizeSlider.value;
			GameObject gameObject = GameObject.Find("Model");
			if (gameObject != null)
			{
				modelRoot = gameObject.transform;
			}
			avatarSizeSlider.onValueChanged.AddListener(delegate(float v)
			{
				targetSize = v;
			});
		}
	}

	public void SyncWithSlider()
	{
		if (avatarSizeSlider != null)
		{
			targetSize = avatarSizeSlider.value;
		}
	}

	private void Update()
	{
		if (avatarSizeSlider == null || MenuActions.IsMovementBlocked())
		{
			return;
		}
		if (modelRoot != null)
		{
			GameObject gameObject = null;
			for (int i = 0; i < modelRoot.childCount; i++)
			{
				Transform child = modelRoot.GetChild(i);
				if (child.gameObject.activeInHierarchy)
				{
					gameObject = child.gameObject;
					break;
				}
			}
			if (gameObject != currentModel)
			{
				currentModel = gameObject;
				controller = ((currentModel != null) ? currentModel.GetComponent<AvatarAnimatorController>() : null);
			}
		}
		if (!(controller != null) || !controller.isDragging)
		{
			float y = Input.mouseScrollDelta.y;
			if (y != 0f)
			{
				targetSize = Mathf.Clamp(targetSize + y * scrollSensitivity, minSize, maxSize);
			}
			float value = avatarSizeSlider.value;
			float num = Mathf.Lerp(value, targetSize, 1f - Mathf.Pow(1f - smoothFactor, Time.deltaTime * 60f));
			if (Mathf.Abs(num - value) > 0.0001f)
			{
				avatarSizeSlider.SetValueWithoutNotify(num);
				avatarSizeSlider.value = num;
				SaveLoadHandler.Instance.data.avatarSize = num;
				SaveLoadHandler.Instance.SaveToDisk();
				SaveLoadHandler.ApplyAllSettingsToAllAvatars();
			}
		}
	}
}
