using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColorController : MonoBehaviour
{
	public enum TargetType
	{
		Light = 0,
		ParticleSystem = 1
	}

	[Serializable]
	public class ColorTarget
	{
		public TargetType type;

		public GameObject target;

		public string id = "new-id";

		public bool enabled;

		public bool allowEnableControl = true;

		[Range(0f, 1f)]
		public float hue;

		[Range(0f, 1f)]
		public float saturation = 1f;

		[Range(0f, 100f)]
		public float intensity = 1f;

		public bool intensityOverride;

		public float maxIntensity = 10f;

		public bool swingMode;

		public HumanBodyBones targetBone = HumanBodyBones.Head;

		[Range(0f, 1f)]
		public float swingSmoothness = 0.15f;

		public bool blockYMovement;

		[HideInInspector]
		public Vector3 editorOffset;

		[HideInInspector]
		public Vector3 currentPosition;

		[HideInInspector]
		public float originalY;

		public string groupID = "";

		public List<string> exclusiveTags = new List<string>();

		[HideInInspector]
		public float fadeCurrentValue;

		[HideInInspector]
		public float fadeTarget;

		[HideInInspector]
		public bool isFading;

		[HideInInspector]
		public bool hasYSet;
	}

	public List<ColorTarget> targets = new List<ColorTarget>();

	[Range(0f, 6f)]
	public float fadeDuration = 1f;

	private Transform modelRoot;

	private GameObject currentModel;

	private AvatarAnimatorReceiver currentReceiver;

	public void SetGroupEnabled(string groupID, bool state)
	{
		List<ColorTarget> list = targets.Where((ColorTarget t) => t.groupID == groupID).ToList();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ColorTarget item in list)
		{
			foreach (string exclusiveTag in item.exclusiveTags)
			{
				hashSet.Add(exclusiveTag);
			}
		}
		if (state)
		{
			foreach (string item2 in hashSet)
			{
				foreach (ColorTarget target in targets)
				{
					if (target.groupID != groupID && target.exclusiveTags != null && target.exclusiveTags.Contains(item2))
					{
						target.fadeTarget = 0f;
						target.isFading = true;
						target.enabled = false;
					}
				}
			}
		}
		foreach (ColorTarget item3 in list)
		{
			float max = (item3.intensityOverride ? item3.maxIntensity : 1f);
			Mathf.Clamp(item3.intensity, 0f, max);
			item3.fadeTarget = (state ? 1f : 0f);
			item3.isFading = true;
			if (state)
			{
				item3.enabled = true;
			}
			if (!state)
			{
				item3.enabled = false;
			}
		}
	}

	private void Awake()
	{
		modelRoot = GameObject.Find("Model")?.transform;
		if (targets == null || !(modelRoot != null))
		{
			return;
		}
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
		AvatarAnimatorReceiver avatarAnimatorReceiver = ((gameObject != null) ? gameObject.GetComponent<AvatarAnimatorReceiver>() : null);
		if (!(avatarAnimatorReceiver != null) || !(avatarAnimatorReceiver.avatarAnimator != null))
		{
			return;
		}
		foreach (ColorTarget target in targets)
		{
			if (!(target.target == null))
			{
				Transform boneTransform = avatarAnimatorReceiver.avatarAnimator.GetBoneTransform(target.targetBone);
				if (!(boneTransform == null))
				{
					target.editorOffset = target.target.transform.position - boneTransform.position;
				}
			}
		}
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
			if (!child.gameObject.activeInHierarchy)
			{
				continue;
			}
			if (!(currentModel != child.gameObject))
			{
				break;
			}
			currentModel = child.gameObject;
			currentReceiver = currentModel.GetComponent<AvatarAnimatorReceiver>();
			{
				foreach (ColorTarget target in targets)
				{
					target.hasYSet = false;
				}
				break;
			}
		}
	}

	private void LateUpdate()
	{
		UpdateCurrentAvatar();
		float num = (Application.isPlaying ? Time.deltaTime : (1f / 60f));
		foreach (ColorTarget target in targets)
		{
			if (target.target == null)
			{
				continue;
			}
			float max = (target.intensityOverride ? target.maxIntensity : 1f);
			float num2 = Mathf.Clamp(target.intensity, 0f, max);
			if (target.isFading)
			{
				float fadeTarget = target.fadeTarget;
				float num3 = ((fadeDuration > 0.01f) ? (Mathf.Abs(fadeTarget - target.fadeCurrentValue) / fadeDuration) : 10000f);
				target.fadeCurrentValue = Mathf.MoveTowards(target.fadeCurrentValue, fadeTarget, num3 * num);
				float value = num2 * target.fadeCurrentValue;
				ApplyFadeToTarget(target, value);
				if (Mathf.Approximately(target.fadeCurrentValue, fadeTarget))
				{
					target.isFading = false;
					if (Mathf.Approximately(fadeTarget, 0f))
					{
						target.enabled = false;
						SetObjectActiveEditorSafe(target.target, value: false);
					}
					else
					{
						target.enabled = true;
						SetObjectActiveEditorSafe(target.target, value: true);
					}
				}
				else
				{
					SetObjectActiveEditorSafe(target.target, value: true);
				}
			}
			else
			{
				if (target.allowEnableControl)
				{
					SetObjectActiveEditorSafe(target.target, target.enabled);
					if (!target.enabled)
					{
						continue;
					}
				}
				else if (!target.enabled)
				{
					continue;
				}
				target.fadeCurrentValue = (target.fadeTarget = 1f);
				ApplyFadeToTarget(target, num2);
			}
			if (target.swingMode)
			{
				if (currentReceiver == null || currentReceiver.avatarAnimator == null)
				{
					continue;
				}
				Transform boneTransform = currentReceiver.avatarAnimator.GetBoneTransform(target.targetBone);
				if (boneTransform == null)
				{
					continue;
				}
				Vector3 b = boneTransform.position + target.editorOffset;
				if (target.blockYMovement)
				{
					if (!target.hasYSet)
					{
						target.originalY = target.target.transform.position.y;
						target.hasYSet = true;
					}
					b.y = target.originalY;
				}
				else
				{
					target.hasYSet = false;
				}
				target.currentPosition = Vector3.Lerp(target.currentPosition, b, 1f - target.swingSmoothness);
				target.target.transform.position = target.currentPosition;
			}
			else
			{
				target.hasYSet = false;
			}
		}
	}

	private void ApplyFadeToTarget(ColorTarget t, float value)
	{
		float S;
		float H;
		if (t.type == TargetType.Light)
		{
			Light component = t.target.GetComponent<Light>();
			if ((bool)component)
			{
				Color.RGBToHSV(component.color, out S, out H, out var V);
				component.color = Color.HSVToRGB(t.hue, t.saturation, V);
				float max = (t.intensityOverride ? t.maxIntensity : 1f);
				component.intensity = Mathf.Clamp(value, 0f, max);
			}
		}
		else if (t.type == TargetType.ParticleSystem)
		{
			ParticleSystem component2 = t.target.GetComponent<ParticleSystem>();
			if ((bool)component2)
			{
				ParticleSystem.MainModule main = component2.main;
				Color.RGBToHSV(main.startColor.color, out H, out S, out var V2);
				Color color = Color.HSVToRGB(t.hue, t.saturation, V2);
				float num = (t.intensityOverride ? t.maxIntensity : 1f);
				color.a = Mathf.Clamp01(value / num);
				main.startColor = new ParticleSystem.MinMaxGradient(color);
			}
		}
	}

	private void SetObjectActiveEditorSafe(GameObject obj, bool value)
	{
		if (obj != null && obj.activeSelf != value)
		{
			obj.SetActive(value);
		}
	}

	private void OnValidate()
	{
		LateUpdate();
	}
}
