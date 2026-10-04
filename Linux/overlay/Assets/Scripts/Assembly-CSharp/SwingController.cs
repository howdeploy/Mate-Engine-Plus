using System.Collections.Generic;
using UnityEngine;

public class SwingController : MonoBehaviour
{
	public List<SwingFollowEntry> follows = new List<SwingFollowEntry>();

	private Camera mainCam;

	private Transform modelRoot;

	private GameObject currentModel;

	private AvatarAnimatorReceiver currentReceiver;

	private void Awake()
	{
		mainCam = Camera.main;
		modelRoot = GameObject.Find("Model")?.transform;
	}

	private void UpdateCurrentAvatar()
	{
		if (!modelRoot)
		{
			return;
		}
		for (int i = 0; i < modelRoot.childCount; i++)
		{
			Transform child = modelRoot.GetChild(i);
			if (child.gameObject.activeInHierarchy)
			{
				if (currentModel != child.gameObject)
				{
					currentModel = child.gameObject;
					currentReceiver = currentModel.GetComponent<AvatarAnimatorReceiver>();
				}
				break;
			}
		}
	}

	private void LateUpdate()
	{
		if (mainCam == null)
		{
			mainCam = Camera.main;
		}
		UpdateCurrentAvatar();
		if (currentReceiver == null || currentReceiver.avatarAnimator == null)
		{
			return;
		}
		foreach (SwingFollowEntry follow in follows)
		{
			if (follow.menuObject == null)
			{
				continue;
			}
			Transform boneTransform = currentReceiver.avatarAnimator.GetBoneTransform(follow.targetBone);
			if (boneTransform == null)
			{
				continue;
			}
			RectTransform component = follow.menuObject.GetComponent<RectTransform>();
			if (!(component == null) && !(component.parent == null))
			{
				Vector3 position = boneTransform.position;
				Vector3 screen = mainCam.WorldToScreenPoint(position);
				Vector2 vector = ScreenToLocal(component, screen);
				if (!follow.hasBaseOffset)
				{
					Vector2 anchoredPosition = component.anchoredPosition;
					follow.baseOffset.x = anchoredPosition.x - vector.x;
					follow.baseOffset.y = 0f;
					follow.currentPosition = anchoredPosition;
					follow.originalY = anchoredPosition.y;
					follow.hasBaseOffset = true;
				}
				Vector2 b = vector + follow.baseOffset;
				if (follow.blockYMovement || follow.yThreshold >= 100f)
				{
					b.y = follow.originalY;
				}
				else
				{
					float num = vector.y - follow.originalY;
					float num2 = 1f - follow.yThreshold / 100f;
					b.y = follow.originalY + num * num2;
				}
				follow.currentPosition = Vector2.Lerp(follow.currentPosition, b, 1f - follow.smoothness);
				component.anchoredPosition = follow.currentPosition;
			}
		}
	}

	private Vector2 ScreenToLocal(RectTransform rect, Vector3 screen)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform, screen, mainCam, out var localPoint);
		return localPoint;
	}
}
