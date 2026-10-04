using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AvatarFoodController : MonoBehaviour
{
	[Header("Feature")]
	public bool featureEnabled;

	[Header("Disable When Feature Off")]
	public List<GameObject> disableWhenOff = new List<GameObject>();

	public List<FoodEntry> foods = new List<FoodEntry>();

	public AudioSource spawnDespawnAudio;

	public AudioSource spawnMainAudio;

	public AudioSource spawnLayerAudio;

	public AudioSource interactionAudio;

	[Header("Interaction Radius")]
	public bool useWorldRadius = true;

	[Range(0f, 2f)]
	public float interactRadiusWorld = 0.18f;

	[Range(10f, 1000f)]
	public float interactRadiusPx = 90f;

	[Range(0f, 100f)]
	public float bigScreenRadiusScale = 100f;

	public float interactCooldown = 0.35f;

	public bool showHeadGizmo = true;

	public Color headGizmoColor = new Color(0.2f, 0.8f, 1f, 0.35f);

	[Header("Head Center")]
	public Vector3 headLocalOffset = Vector3.zero;

	public Vector3 headWorldOffset = Vector3.zero;

	public bool enableSway = true;

	public bool swayUseLocalRotation = true;

	public float mouseSensitivity = 0.6f;

	public bool invertHorizontal;

	public bool invertVertical;

	public float horizontalVelocityToLean = 0.25f;

	public float verticalVelocityToPitch = 0.15f;

	public float maxLeanZ = 25f;

	public float maxLeanX = 12f;

	public float springFrequency = 2.6f;

	public float dampingRatio = 0.35f;

	public float swayBlendSpeed = 8f;

	public float avatarProbeInterval = 0.25f;

	private Camera cam;

	private Animator animator;

	private Transform head;

	private FoodEntry activeEntry;

	private string activeIdNorm;

	private Coroutine scaleRoutine;

	private float depthZ = 1f;

	private float nextInteractAt;

	private bool wasInside;

	private Vector2 prevMousePos;

	private Vector2 filteredDelta;

	private float leanZ;

	private float leanZVel;

	private float leanX;

	private float leanXVel;

	private float swayWeight;

	private Quaternion baseLocalRot;

	private Quaternion baseWorldRot;

	private VRMLoader loader;

	private GameObject currentAvatarRoot;

	private float probeTimer;

	private void Awake()
	{
		featureEnabled = (bool)SaveLoadHandler.Instance && SaveLoadHandler.Instance.data.enableFeedSystem;
		if (!featureEnabled)
		{
			DeactivateAll();
		}
		TrySetup();
		DeactivateAll();
		prevMousePos = Input.mousePosition;
		ProbeAvatarNow();
	}

	private void TrySetup()
	{
		if (!cam)
		{
			cam = (Camera.main ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>());
		}
		if (!spawnDespawnAudio)
		{
			spawnDespawnAudio = base.gameObject.AddComponent<AudioSource>();
		}
		if (!spawnMainAudio)
		{
			spawnMainAudio = base.gameObject.AddComponent<AudioSource>();
		}
		if (!spawnLayerAudio)
		{
			spawnLayerAudio = base.gameObject.AddComponent<AudioSource>();
		}
		if (!interactionAudio)
		{
			interactionAudio = base.gameObject.AddComponent<AudioSource>();
		}
		if (!loader)
		{
			loader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		}
		UpdateDepthFromHead();
	}

	private void UpdateDepthFromHead()
	{
		if ((bool)cam && (bool)head)
		{
			Vector3 vector = cam.WorldToScreenPoint(GetHeadCenterWorld());
			depthZ = ((vector.z <= 0f) ? 1f : vector.z);
		}
	}

	private void LateUpdate()
	{
		probeTimer -= Time.unscaledDeltaTime;
		if (probeTimer <= 0f)
		{
			ProbeAvatarNow();
			probeTimer = Mathf.Max(0.05f, avatarProbeInterval);
		}
		if (!featureEnabled)
		{
			if (activeEntry != null)
			{
				DeactivateAll();
			}
			return;
		}
		if (activeEntry != null && (bool)activeEntry.obj && activeEntry.followMouse)
		{
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.z = depthZ;
			Vector3 position = cam.ScreenToWorldPoint(mousePosition) + activeEntry.worldOffset;
			activeEntry.obj.transform.position = position;
		}
		UpdateSway();
		ApplySway();
		HeadInteractCheck();
	}

	private void UpdateSway()
	{
		if (!enableSway || activeEntry == null || activeEntry.obj == null)
		{
			swayWeight = Mathf.MoveTowards(swayWeight, 0f, swayBlendSpeed * Time.deltaTime);
			return;
		}
		Vector2 vector = Input.mousePosition;
		Vector2 b = (vector - prevMousePos) * mouseSensitivity;
		prevMousePos = vector;
		float deltaTime = Time.deltaTime;
		filteredDelta = Vector2.Lerp(filteredDelta, b, 1f - Mathf.Exp(-12f * deltaTime));
		float num = (invertHorizontal ? 1f : (-1f));
		float num2 = (invertVertical ? (-1f) : 1f);
		float xt = Mathf.Clamp(num * filteredDelta.x * horizontalVelocityToLean, 0f - maxLeanZ, maxLeanZ);
		float xt2 = Mathf.Clamp(num2 * filteredDelta.y * verticalVelocityToPitch, 0f - maxLeanX, maxLeanX);
		Spring(ref leanZ, ref leanZVel, xt, springFrequency, dampingRatio, deltaTime);
		Spring(ref leanX, ref leanXVel, xt2, springFrequency, dampingRatio, deltaTime);
		swayWeight = Mathf.MoveTowards(swayWeight, 1f, swayBlendSpeed * deltaTime);
	}

	private void ApplySway()
	{
		if (!(swayWeight <= 0f) && activeEntry != null && !(activeEntry.obj == null))
		{
			float num = leanX * swayWeight;
			float num2 = leanZ * swayWeight;
			if (swayUseLocalRotation)
			{
				activeEntry.obj.transform.localRotation = baseLocalRot * Quaternion.Euler(num, 0f, num2);
				return;
			}
			Transform transform = base.transform;
			Quaternion quaternion = Quaternion.AngleAxis(num, transform.right) * Quaternion.AngleAxis(num2, transform.forward);
			activeEntry.obj.transform.rotation = quaternion * baseWorldRot;
		}
	}

	private void HeadInteractCheck()
	{
		if ((bool)cam && (bool)head)
		{
			Vector2 vector = Input.mousePosition;
			Vector3 headCenterWorld = GetHeadCenterWorld();
			Vector2 vector2 = cam.WorldToScreenPoint(headCenterWorld);
			float num = ComputeScreenRadiusPx(headCenterWorld);
			bool flag = (vector - vector2).sqrMagnitude <= num * num;
			if (flag && !wasInside && Time.time >= nextInteractAt)
			{
				PlayRandom((activeEntry != null) ? activeEntry.interactClips : null, interactionAudio, activeEntry);
				nextInteractAt = Time.time + interactCooldown;
			}
			wasInside = flag;
		}
	}

	private Vector3 GetHeadCenterWorld()
	{
		if (!head)
		{
			return base.transform.position;
		}
		return head.position + head.TransformVector(headLocalOffset) + headWorldOffset;
	}

	private float ComputeScreenRadiusPx(Vector3 centerWorld)
	{
		if (!cam || !head)
		{
			return interactRadiusPx;
		}
		if (!useWorldRadius)
		{
			return interactRadiusPx;
		}
		float magnitude = head.lossyScale.magnitude;
		float num = ((animator != null && animator.GetBool("isBigScreen")) ? (bigScreenRadiusScale * 0.01f) : 1f);
		float num2 = Mathf.Max(0f, interactRadiusWorld) * Mathf.Max(0.0001f, magnitude) * num;
		Vector2 vector = cam.WorldToScreenPoint(centerWorld);
		Vector2 vector2 = cam.WorldToScreenPoint(centerWorld + cam.transform.right * num2);
		return (vector - vector2).magnitude;
	}

	public void ToggleById(string id)
	{
		if (!featureEnabled || string.IsNullOrEmpty(id))
		{
			return;
		}
		if (HasActive())
		{
			DespawnActive();
			return;
		}
		string idNorm = NormalizeId(id);
		FoodEntry foodEntry = PickRandomById(idNorm);
		if (foodEntry != null)
		{
			Spawn(foodEntry, idNorm);
		}
	}

	public void ToggleByIndex(int index)
	{
		if (featureEnabled && index >= 0 && index < foods.Count)
		{
			if (HasActive())
			{
				DespawnActive();
				return;
			}
			FoodEntry foodEntry = foods[index];
			Spawn(foodEntry, NormalizeId(foodEntry?.id));
		}
	}

	public void SpawnByIndex(int index)
	{
		if (featureEnabled && index >= 0 && index < foods.Count)
		{
			FoodEntry foodEntry = foods[index];
			Spawn(foodEntry, NormalizeId(foodEntry?.id));
		}
	}

	public void SpawnById(string id)
	{
		if (featureEnabled)
		{
			string idNorm = NormalizeId(id);
			FoodEntry foodEntry = PickRandomById(idNorm);
			if (foodEntry != null)
			{
				Spawn(foodEntry, idNorm);
			}
		}
	}

	public void DespawnActive()
	{
		if (activeEntry != null && !(activeEntry.obj == null))
		{
			if (scaleRoutine != null)
			{
				StopCoroutine(scaleRoutine);
			}
			PlayRandom(activeEntry.despawnClips, spawnDespawnAudio, activeEntry);
			Transform transform = activeEntry.obj.transform;
			Vector3 localScale = transform.localScale;
			scaleRoutine = StartCoroutine(ScaleTo(transform, localScale, Vector3.zero, activeEntry.despawnDuration, delegate
			{
				ResetPose(activeEntry);
				activeEntry.obj.SetActive(value: false);
				activeEntry = null;
				activeIdNorm = null;
				ResetSwayState();
			}));
		}
	}

	public void PlayInteract()
	{
		if (featureEnabled)
		{
			PlayRandom((activeEntry != null) ? activeEntry.interactClips : null, interactionAudio, activeEntry);
			nextInteractAt = Time.time + interactCooldown;
		}
	}

	public bool HasActive()
	{
		if (activeEntry != null && (bool)activeEntry.obj)
		{
			return activeEntry.obj.activeSelf;
		}
		return false;
	}

	private void Spawn(FoodEntry entry, string idNorm)
	{
		DeactivateAll();
		if (entry != null && !(entry.obj == null))
		{
			ResetPose(entry);
			activeEntry = entry;
			activeIdNorm = idNorm;
			activeEntry.obj.SetActive(value: true);
			activeEntry.obj.transform.localScale = Vector3.zero;
			baseLocalRot = Quaternion.identity;
			baseWorldRot = Quaternion.identity;
			ResetSwayState();
			UpdateDepthFromHead();
			MoveOnceToCursor();
			PlaySpawnSounds(activeEntry);
			if (scaleRoutine != null)
			{
				StopCoroutine(scaleRoutine);
			}
			scaleRoutine = StartCoroutine(ScaleTo(activeEntry.obj.transform, Vector3.zero, Vector3.one, activeEntry.spawnDuration, null));
			wasInside = false;
		}
	}

	private void PlaySpawnSounds(FoodEntry e)
	{
		AudioSource src = (spawnMainAudio ? spawnMainAudio : spawnDespawnAudio);
		AudioSource src2 = (spawnLayerAudio ? spawnLayerAudio : spawnDespawnAudio);
		PlayRandom(e.spawnClips, src, e);
		PlayRandom(e.spawnLayerClips, src2, e);
	}

	private void MoveOnceToCursor()
	{
		if (activeEntry != null && !(cam == null))
		{
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.z = depthZ;
			Vector3 position = cam.ScreenToWorldPoint(mousePosition) + activeEntry.worldOffset;
			activeEntry.obj.transform.position = position;
		}
	}

	private void DeactivateAll()
	{
		for (int i = 0; i < foods.Count; i++)
		{
			if ((bool)foods[i].obj)
			{
				ResetPose(foods[i]);
				foods[i].obj.SetActive(value: false);
			}
		}
		activeEntry = null;
		activeIdNorm = null;
		ResetSwayState();
	}

	private void ResetPose(FoodEntry e)
	{
		if (e != null && !(e.obj == null))
		{
			Transform obj = e.obj.transform;
			obj.localRotation = Quaternion.identity;
			obj.rotation = Quaternion.identity;
		}
	}

	private void ResetSwayState()
	{
		leanX = 0f;
		leanZ = 0f;
		leanXVel = 0f;
		leanZVel = 0f;
		filteredDelta = Vector2.zero;
		swayWeight = 0f;
	}

	private IEnumerator ScaleTo(Transform t, Vector3 from, Vector3 to, float dur, Action onDone)
	{
		if (dur <= 0f)
		{
			t.localScale = to;
			onDone?.Invoke();
			yield break;
		}
		float e = 0f;
		t.localScale = from;
		while (e < dur)
		{
			e += Time.unscaledDeltaTime;
			float t2 = Mathf.Clamp01(e / dur);
			t.localScale = Vector3.LerpUnclamped(from, to, t2);
			yield return null;
		}
		t.localScale = to;
		onDone?.Invoke();
	}

	private void PlayRandom(List<AudioClip> clips, AudioSource src, FoodEntry entry)
	{
		if (!(src == null) && clips != null && clips.Count != 0)
		{
			float num = entry?.minPitch ?? 1f;
			float num2 = entry?.maxPitch ?? 1f;
			if (num2 < num)
			{
				float num3 = num;
				num = num2;
				num2 = num3;
			}
			src.pitch = UnityEngine.Random.Range(num, num2);
			int index = UnityEngine.Random.Range(0, clips.Count);
			src.PlayOneShot(clips[index]);
		}
	}

	private static void Spring(ref float x, ref float v, float xt, float f, float z, float dt)
	{
		float num = Mathf.Max(0.01f, f) * 2f * (float)Math.PI;
		float num2 = num * num * (xt - x) - 2f * z * num * v;
		v += num2 * dt;
		x += v * dt;
	}

	private FoodEntry PickRandomById(string idNorm)
	{
		List<FoodEntry> list = foods.Where((FoodEntry f) => f != null && f.obj != null && string.Equals(NormalizeId(f.id), idNorm, StringComparison.OrdinalIgnoreCase)).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		return list[index];
	}

	private string NormalizeId(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Trim();
		}
		return "";
	}

	private void ProbeAvatarNow()
	{
		if (!loader)
		{
			loader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		}
		GameObject gameObject = null;
		if ((bool)loader)
		{
			gameObject = ((loader.GetCurrentModel() != null) ? loader.GetCurrentModel() : loader.mainModel);
		}
		if (!gameObject)
		{
			Animator animator = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault((Animator a) => (bool)a && a.isActiveAndEnabled);
			gameObject = (animator ? animator.gameObject : null);
		}
		if (gameObject != currentAvatarRoot)
		{
			currentAvatarRoot = gameObject;
			Animator animator2 = (gameObject ? gameObject.GetComponentInChildren<Animator>(includeInactive: true) : null);
			if (animator2 != this.animator)
			{
				this.animator = animator2;
				head = (this.animator ? this.animator.GetBoneTransform(HumanBodyBones.Head) : null);
				UpdateDepthFromHead();
			}
		}
	}

	public void SetFeatureEnabled(bool on)
	{
		featureEnabled = on;
		foreach (GameObject item in disableWhenOff)
		{
			if ((bool)item)
			{
				item.SetActive(on);
			}
		}
		if (!featureEnabled)
		{
			DeactivateAll();
		}
	}

	public void EnableFeature()
	{
		SetFeatureEnabled(on: true);
	}

	public void DisableFeature()
	{
		SetFeatureEnabled(on: false);
	}

	public void ToggleFeature()
	{
		SetFeatureEnabled(!featureEnabled);
	}
}
