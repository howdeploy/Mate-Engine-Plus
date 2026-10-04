using System.Collections.Generic;
using UnityEngine;

public class SwingController : MonoBehaviour
{
	public List<SwingFollowEntry> follows = new List<SwingFollowEntry>();

	private Camera mainCam;

	private Transform modelRoot;

	private GameObject currentModel;

	private AvatarAnimatorReceiver currentReceiver;

	private AvatarMinecraftMessages currentMinecraftMessages;

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
					currentMinecraftMessages = currentModel.GetComponent<AvatarMinecraftMessages>();
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
		if (mainCam == null || currentReceiver == null || currentReceiver.avatarAnimator == null)
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
				if (currentMinecraftMessages != null && currentMinecraftMessages.chatContainer == component)
				{
					PositionMinecraftBubble(component, boneTransform.position);
					continue;
				}
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

	private void PositionMinecraftBubble(RectTransform rect, Vector3 headPosition)
	{
		RectTransform parent = rect.parent as RectTransform;
		Canvas canvas = rect.GetComponentInParent<Canvas>();
		if (parent == null || canvas == null) return;
		Vector3 screen = mainCam.WorldToScreenPoint(headPosition);
		if (screen.z <= 0f) return;
		Canvas root = canvas.rootCanvas;
		Camera uiCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var point)) return;
		point.x -= Mathf.Max(0f, currentMinecraftMessages.headClearance);

		// Bubbles grow left/up from the container's bottom-right corner. Follow
		// the live head instead of retaining the scene's Y=600 and partial Y lag.
		Vector2 anchor = parent.rect.min + Vector2.Scale(parent.rect.size,
			new Vector2(Mathf.Lerp(rect.anchorMin.x, rect.anchorMax.x, rect.pivot.x),
				Mathf.Lerp(rect.anchorMin.y, rect.anchorMax.y, rect.pivot.y)));
		Vector3 corner = rect.localRotation * Vector3.Scale(
			new Vector3(rect.rect.xMax, rect.rect.yMin, 0f), rect.localScale);
		rect.anchoredPosition = point - anchor - new Vector2(corner.x, corner.y);
	}

	private Vector2 ScreenToLocal(RectTransform rect, Vector3 screen)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform, screen, mainCam, out var localPoint);
		return localPoint;
	}
}
