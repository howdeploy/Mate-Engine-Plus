using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

public class PetVoiceReactionHandler : MonoBehaviour
{
	[Serializable]
	public class VoiceRegion
	{
		public string name;

		public bool IsHusbando;

		public HumanBodyBones targetBone;

		public Vector3 offset;

		public Vector3 worldOffset;

		public float hoverRadius = 50f;

		public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.25f);

		public List<AudioClip> voiceClips = new List<AudioClip>();

		public string hoverAnimationState;

		public string hoverAnimationLayer;

		public string hoverAnimationParameter;

		public string faceAnimationState;

		public string faceAnimationLayer;

		public string faceAnimationParameter;

		public bool enableHoverObject;

		public bool bindHoverObjectToBone;

		public bool enableLayeredSound;

		public GameObject hoverObject;

		[Range(0.1f, 10f)]
		public float despawnAfterSeconds = 5f;

		public List<AudioClip> layeredVoiceClips = new List<AudioClip>();

		[HideInInspector]
		public bool wasHovering;

		[HideInInspector]
		public Transform bone;

		[HideInInspector]
		public int hoverLayerIndex;

		[HideInInspector]
		public int faceLayerIndex;

		[HideInInspector]
		public bool hasHoverBool;

		[HideInInspector]
		public bool hasFaceBool;

		[Header("Pat Mode")]
		public bool patMode;

		[Range(90f, 1080f)]
		public float patCircleDegrees = 540f;

		[Range(0f, 50f)]
		public float patMinRadius = 12f;

		[Range(0.1f, 2f)]
		public float patResetTime = 0.6f;

		[Range(0f, 1f)]
		public float patCooldown = 0.5f;

		[Range(20f, 600f)]
		public float patWiggleDistance = 180f;

		[Range(1f, 10f)]
		public int patWiggleDirectionChanges = 3;

		[Header("Per-Region State Whitelist")]
		[SerializeField]
		public List<string> stateWhitelist = new List<string>();

		[HideInInspector]
		public HashSet<int> whitelistHashes = new HashSet<int>();

		[HideInInspector]
		public bool patHasLast;

		[HideInInspector]
		public Vector2 patLastPos;

		[HideInInspector]
		public Vector2 patLastMove;

		[HideInInspector]
		public float patLastAngle;

		[HideInInspector]
		public float patCircleAccum;

		[HideInInspector]
		public float patWiggleAccumDist;

		[HideInInspector]
		public int patWiggleChanges;

		[HideInInspector]
		public float patExpireAt;

		[HideInInspector]
		public float patCooldownUntil;
	}

	private class HoverInstance
	{
		public GameObject obj;

		public float despawnTime;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	public static bool GlobalHoverObjectsEnabled = true;

	public Animator avatarAnimator;

	public List<VoiceRegion> regions = new List<VoiceRegion>();

	public AudioSource voiceAudioSource;

	public AudioSource layeredAudioSource;

	public bool showDebugGizmos = true;

	[Header("Global State Whitelist")]
	[SerializeField]
	public List<string> stateWhitelist = new List<string>();

	[Range(0f, 0.1f)]
	public float checkInterval = 0.02f;

	[Header("OS Occlusion")]
	public bool blockWhenCovered = true;

	private IntPtr _unityHwnd;

	private bool _hwndCached;

	private Camera cachedCamera;

	private readonly Dictionary<VoiceRegion, List<HoverInstance>> pool = new Dictionary<VoiceRegion, List<HoverInstance>>();

	private bool hasSetup;

	private static readonly int isMaleHash = Animator.StringToHash("isMale");

	private readonly HashSet<int> boolParams = new HashSet<int>();

	private bool hasIsMaleParam;

	private readonly HashSet<int> globalWhitelistHashes = new HashSet<int>();

	private AvatarBigScreenHandler cachedBigScreen;

	private FieldInfo bigScreenFlag;

	private bool bigScreenBlocked;

	private float nextCheck;

	private const uint GA_ROOT = 2u;

	private void Start()
	{
		if (!hasSetup)
		{
			TrySetup();
		}
	}

	public void SetAnimator(Animator a)
	{
		avatarAnimator = a;
		hasSetup = false;
	}

	private void TrySetup()
	{
		if (!avatarAnimator)
		{
			return;
		}
		if (!voiceAudioSource)
		{
			voiceAudioSource = base.gameObject.AddComponent<AudioSource>();
		}
		if (!layeredAudioSource)
		{
			layeredAudioSource = base.gameObject.AddComponent<AudioSource>();
		}
		cachedCamera = Camera.main;
		boolParams.Clear();
		AnimatorControllerParameter[] parameters = avatarAnimator.parameters;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i].type == AnimatorControllerParameterType.Bool)
			{
				boolParams.Add(parameters[i].nameHash);
			}
		}
		hasIsMaleParam = false;
		for (int j = 0; j < parameters.Length; j++)
		{
			if (parameters[j].nameHash == isMaleHash)
			{
				hasIsMaleParam = true;
				break;
			}
		}
		globalWhitelistHashes.Clear();
		for (int k = 0; k < stateWhitelist.Count; k++)
		{
			if (!string.IsNullOrEmpty(stateWhitelist[k]))
			{
				globalWhitelistHashes.Add(Animator.StringToHash(stateWhitelist[k]));
			}
		}
		for (int l = 0; l < regions.Count; l++)
		{
			VoiceRegion voiceRegion = regions[l];
			voiceRegion.whitelistHashes.Clear();
			for (int m = 0; m < voiceRegion.stateWhitelist.Count; m++)
			{
				if (!string.IsNullOrEmpty(voiceRegion.stateWhitelist[m]))
				{
					voiceRegion.whitelistHashes.Add(Animator.StringToHash(voiceRegion.stateWhitelist[m]));
				}
			}
			voiceRegion.bone = avatarAnimator.GetBoneTransform(voiceRegion.targetBone);
			voiceRegion.hoverLayerIndex = GetLayerIndexByName(voiceRegion.hoverAnimationLayer);
			voiceRegion.faceLayerIndex = GetLayerIndexByName(voiceRegion.faceAnimationLayer);
			voiceRegion.hasHoverBool = !string.IsNullOrEmpty(voiceRegion.hoverAnimationParameter) && boolParams.Contains(Animator.StringToHash(voiceRegion.hoverAnimationParameter));
			voiceRegion.hasFaceBool = !string.IsNullOrEmpty(voiceRegion.faceAnimationParameter) && boolParams.Contains(Animator.StringToHash(voiceRegion.faceAnimationParameter));
			if (!voiceRegion.enableHoverObject || !voiceRegion.hoverObject)
			{
				continue;
			}
			List<HoverInstance> list = new List<HoverInstance>();
			for (int n = 0; n < 4; n++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(voiceRegion.hoverObject);
				if (voiceRegion.bindHoverObjectToBone && (bool)voiceRegion.bone)
				{
					gameObject.transform.SetParent(voiceRegion.bone, worldPositionStays: false);
					gameObject.transform.localPosition = Vector3.zero;
				}
				gameObject.SetActive(value: false);
				list.Add(new HoverInstance
				{
					obj = gameObject,
					despawnTime = -1f
				});
			}
			pool[voiceRegion] = list;
		}
		if (cachedBigScreen == null)
		{
			cachedBigScreen = UnityEngine.Object.FindFirstObjectByType<AvatarBigScreenHandler>();
		}
		if (cachedBigScreen != null && bigScreenFlag == null)
		{
			bigScreenFlag = cachedBigScreen.GetType().GetField("isBigScreenActive", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		hasSetup = true;
	}

	private void Update()
	{
		if (!hasSetup)
		{
			TrySetup();
		}
		if (cachedCamera == null || avatarAnimator == null)
		{
			return;
		}
		if (Time.time >= nextCheck)
		{
			if (cachedBigScreen == null && bigScreenFlag == null)
			{
				cachedBigScreen = UnityEngine.Object.FindFirstObjectByType<AvatarBigScreenHandler>();
				if (cachedBigScreen != null)
				{
					bigScreenFlag = cachedBigScreen.GetType().GetField("isBigScreenActive", BindingFlags.Instance | BindingFlags.NonPublic);
				}
			}
			bigScreenBlocked = false;
			if (cachedBigScreen != null && bigScreenFlag != null)
			{
				bigScreenBlocked = bigScreenFlag.GetValue(cachedBigScreen) as bool? == true;
			}
			nextCheck = Time.time + checkInterval;
		}
		Vector2 vector = Input.mousePosition;
		bool num = MenuActions.IsReactionBlocked();
		bool flag = blockWhenCovered && IsOccludedByOS();
		bool flag2 = num || bigScreenBlocked || flag;
		for (int i = 0; i < regions.Count; i++)
		{
			VoiceRegion voiceRegion = regions[i];
			if (voiceRegion.bone == null)
			{
				continue;
			}
			Vector3 vector2 = voiceRegion.bone.position + voiceRegion.bone.TransformVector(voiceRegion.offset) + voiceRegion.worldOffset;
			Vector2 vector3 = cachedCamera.WorldToScreenPoint(vector2);
			float magnitude = voiceRegion.bone.lossyScale.magnitude;
			float num2 = voiceRegion.hoverRadius * magnitude;
			Vector2 vector4 = cachedCamera.WorldToScreenPoint(vector2 + cachedCamera.transform.right * num2);
			Vector2 vector5 = vector - vector3;
			Vector2 vector6 = vector3 - vector4;
			float sqrMagnitude = vector5.sqrMagnitude;
			float sqrMagnitude2 = vector6.sqrMagnitude;
			bool flag3 = sqrMagnitude <= sqrMagnitude2;
			bool flag4 = IsRegionAllowedByGender(voiceRegion);
			bool flag5 = IsStateAllowedForRegion(voiceRegion);
			if (flag3 && !voiceRegion.wasHovering && flag5 && !flag2 && flag4)
			{
				bool flag6 = true;
				if (voiceRegion.patMode)
				{
					flag6 = ProcessPat(voiceRegion, vector3, vector);
				}
				if (flag6)
				{
					voiceRegion.wasHovering = true;
					TriggerAnim(voiceRegion, state: true);
					PlayRandomVoice(voiceRegion);
					if (GlobalHoverObjectsEnabled && voiceRegion.enableHoverObject && voiceRegion.hoverObject != null)
					{
						List<HoverInstance> list = pool[voiceRegion];
						HoverInstance hoverInstance = null;
						for (int j = 0; j < list.Count; j++)
						{
							if (!list[j].obj.activeSelf)
							{
								hoverInstance = list[j];
								break;
							}
						}
						if (hoverInstance == null)
						{
							float num3 = 3.4028235E+38f;
							for (int k = 0; k < list.Count; k++)
							{
								if (list[k].despawnTime < num3)
								{
									num3 = list[k].despawnTime;
									hoverInstance = list[k];
								}
							}
						}
						if (hoverInstance != null)
						{
							if (!voiceRegion.bindHoverObjectToBone)
							{
								hoverInstance.obj.transform.position = vector2;
							}
							hoverInstance.obj.SetActive(value: false);
							hoverInstance.obj.SetActive(value: true);
							hoverInstance.despawnTime = Time.time + voiceRegion.despawnAfterSeconds;
						}
					}
				}
			}
			else if ((!flag3 || flag2 || !flag4) && voiceRegion.wasHovering)
			{
				voiceRegion.wasHovering = false;
				TriggerAnim(voiceRegion, state: false);
			}
			if (!flag3 || flag2)
			{
				ResetPat(voiceRegion);
			}
		}
		foreach (VoiceRegion region in regions)
		{
			if (!region.enableHoverObject || !pool.ContainsKey(region))
			{
				continue;
			}
			List<HoverInstance> list2 = pool[region];
			for (int l = 0; l < list2.Count; l++)
			{
				if (list2[l].obj.activeSelf && Time.time >= list2[l].despawnTime)
				{
					list2[l].obj.SetActive(value: false);
					list2[l].despawnTime = -1f;
				}
			}
		}
	}

	private bool ProcessPat(VoiceRegion r, Vector2 center, Vector2 mouse)
	{
		float time = Time.time;
		if (time >= r.patExpireAt)
		{
			ResetPat(r);
		}
		float num = 4f;
		if (!r.patHasLast)
		{
			r.patHasLast = true;
			r.patLastPos = mouse;
			r.patLastMove = Vector2.zero;
			r.patLastAngle = Mathf.Atan2(mouse.y - center.y, mouse.x - center.x) * 57.29578f;
			r.patExpireAt = time + r.patResetTime;
			return false;
		}
		Vector2 patLastMove = mouse - r.patLastPos;
		if (patLastMove.sqrMagnitude >= num * num)
		{
			float num2 = Mathf.Atan2(mouse.y - center.y, mouse.x - center.x) * 57.29578f;
			float f = Mathf.DeltaAngle(r.patLastAngle, num2);
			float magnitude = (r.patLastPos - center).magnitude;
			float magnitude2 = (mouse - center).magnitude;
			if (magnitude >= r.patMinRadius || magnitude2 >= r.patMinRadius)
			{
				r.patCircleAccum += Mathf.Abs(f);
			}
			if (r.patLastMove.sqrMagnitude > 0f && Vector2.Dot(patLastMove.normalized, r.patLastMove.normalized) < -0.2f)
			{
				r.patWiggleChanges++;
			}
			r.patWiggleAccumDist += patLastMove.magnitude;
			r.patLastAngle = num2;
			r.patLastMove = patLastMove;
			r.patLastPos = mouse;
			r.patExpireAt = time + r.patResetTime;
		}
		bool num3 = r.patCircleAccum >= r.patCircleDegrees;
		bool flag = r.patWiggleAccumDist >= r.patWiggleDistance && r.patWiggleChanges >= r.patWiggleDirectionChanges;
		if ((num3 || flag) && time >= r.patCooldownUntil)
		{
			r.patCooldownUntil = time + r.patCooldown;
			ResetPat(r);
			return true;
		}
		return false;
	}

	private void ResetPat(VoiceRegion r)
	{
		r.patHasLast = false;
		r.patLastPos = Vector2.zero;
		r.patLastMove = Vector2.zero;
		r.patLastAngle = 0f;
		r.patCircleAccum = 0f;
		r.patWiggleAccumDist = 0f;
		r.patWiggleChanges = 0;
		r.patExpireAt = Time.time + r.patResetTime;
	}

	private void TriggerAnim(VoiceRegion region, bool state)
	{
		if (!(avatarAnimator == null) && (!HasBoolHash(Animator.StringToHash("isCustomDancing")) || !avatarAnimator.GetBool("isCustomDancing")))
		{
			if (region.hasHoverBool)
			{
				avatarAnimator.SetBool(region.hoverAnimationParameter, state);
			}
			else if (!string.IsNullOrEmpty(region.hoverAnimationState))
			{
				avatarAnimator.CrossFadeInFixedTime(region.hoverAnimationState, 0.1f, region.hoverLayerIndex);
			}
			if (region.hasFaceBool)
			{
				avatarAnimator.SetBool(region.faceAnimationParameter, state);
			}
			else if (!string.IsNullOrEmpty(region.faceAnimationState))
			{
				avatarAnimator.CrossFadeInFixedTime(region.faceAnimationState, 0.1f, region.faceLayerIndex);
			}
		}
	}

	private void PlayRandomVoice(VoiceRegion region)
	{
		if (region.voiceClips.Count > 0 && !voiceAudioSource.isPlaying)
		{
			voiceAudioSource.clip = region.voiceClips[UnityEngine.Random.Range(0, region.voiceClips.Count)];
			voiceAudioSource.Play();
		}
		if (region.enableLayeredSound && region.layeredVoiceClips.Count > 0)
		{
			layeredAudioSource.PlayOneShot(region.layeredVoiceClips[UnityEngine.Random.Range(0, region.layeredVoiceClips.Count)]);
		}
	}

	private bool IsStateAllowedForRegion(VoiceRegion region)
	{
		if (avatarAnimator == null)
		{
			return false;
		}
		int shortNameHash = avatarAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
		if (region.whitelistHashes != null && region.whitelistHashes.Count > 0)
		{
			return region.whitelistHashes.Contains(shortNameHash);
		}
		if (globalWhitelistHashes.Count > 0)
		{
			return globalWhitelistHashes.Contains(shortNameHash);
		}
		return false;
	}

	private bool IsRegionAllowedByGender(VoiceRegion region)
	{
		if (avatarAnimator == null)
		{
			return true;
		}
		if (!hasIsMaleParam)
		{
			return true;
		}
		bool flag = avatarAnimator.GetFloat(isMaleHash) > 0.5f;
		if (!region.IsHusbando)
		{
			return !flag;
		}
		return flag;
	}

	private int GetLayerIndexByName(string layerName)
	{
		if (string.IsNullOrEmpty(layerName))
		{
			return 0;
		}
		int layerCount = avatarAnimator.layerCount;
		for (int i = 0; i < layerCount; i++)
		{
			if (avatarAnimator.GetLayerName(i) == layerName)
			{
				return i;
			}
		}
		return 0;
	}

	private bool HasBoolHash(int hash)
	{
		return boolParams.Contains(hash);
	}

	public void ResetAfterDance()
	{
		if (avatarAnimator == null)
		{
			return;
		}
		for (int i = 0; i < regions.Count; i++)
		{
			regions[i].wasHovering = false;
			if (regions[i].hasHoverBool)
			{
				avatarAnimator.SetBool(regions[i].hoverAnimationParameter, value: false);
			}
			if (regions[i].hasFaceBool)
			{
				avatarAnimator.SetBool(regions[i].faceAnimationParameter, value: false);
			}
		}
	}

	private void ResolveWindowHandle()
	{
		if (!_hwndCached)
		{
			_unityHwnd = WindowManager.Instance != null ? WindowManager.Instance.UnityWindow : IntPtr.Zero;
			_hwndCached = _unityHwnd != IntPtr.Zero;
		}
	}

	private bool IsOccludedByOS()
	{
		ResolveWindowHandle();
		if (_unityHwnd == IntPtr.Zero)
		{
			return false;
		}
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetMousePosition(out var point))
		{
			return false;
		}
		var windows = wm.GetClientStackingList();
		for (int i = windows.Count - 1; i >= 0; i--)
		{
			var window = windows[i];
			if (wm.IsWindowVisible(window) && wm.GetWindowRect(window, out var rect) && rect.Contains(point))
				return window != _unityHwnd;
		}
		return true;
	}
}
