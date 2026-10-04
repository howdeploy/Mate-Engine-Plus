using System.Linq;
using CustomDancePlayer;
using UnityEngine;
using UnityEngine.UI;

public class AvatarDanceSafetyZone : MonoBehaviour
{
	public Camera cam;

	public WindowManager uniWindow;

	public string dancingParam = "isCustomDancing";

	public float marginPx = 24f;

	public float softZoneLeftPx = 220f;

	public float softZoneRightPx = 220f;

	public float panSpeedPxPerSec = 1800f;

	public bool moveWindowAlong = true;

	public bool enableSafety;

	[Header("Activate Toggle")]
	public Toggle activateToggle;

	private Animator animator;

	private Transform hip;

	private int dancingHash;

	private AvatarDanceHandler danceHandler;

	private Transform lastModelRoot;

	private bool wasActive;

	private Vector3 camPosBeforeDance;

	private Quaternion camRotBeforeDance;

	private float refetchCooldown;

	private void OnEnable()
	{
		dancingHash = Animator.StringToHash(dancingParam);
		danceHandler = Object.FindFirstObjectByType<AvatarDanceHandler>();
		if (uniWindow == null)
		{
			uniWindow = WindowManager.Instance;
		}
		if (activateToggle != null)
		{
			activateToggle.SetIsOnWithoutNotify(enableSafety);
			activateToggle.onValueChanged.AddListener(OnActivateToggleChanged);
		}
	}

	private void OnDisable()
	{
		if (activateToggle != null)
		{
			activateToggle.onValueChanged.RemoveListener(OnActivateToggleChanged);
		}
		if (wasActive)
		{
			RestoreCamera();
		}
		wasActive = false;
	}

	private void Update()
	{
		bool flag = IsActive();
		if (flag && !wasActive)
		{
			if (cam == null)
			{
				return;
			}
			camPosBeforeDance = cam.transform.position;
			camRotBeforeDance = cam.transform.rotation;
			FetchActiveAvatar(force: true);
		}
		if (!flag && wasActive)
		{
			RestoreCamera();
		}
		wasActive = flag;
		if (flag)
		{
			refetchCooldown -= Time.unscaledDeltaTime;
			if (refetchCooldown <= 0f)
			{
				FetchActiveAvatar(force: false);
				refetchCooldown = 0.5f;
			}
		}
	}

	private void LateUpdate()
	{
		if (!wasActive || cam == null || hip == null || uniWindow == null || Screen.fullScreen || uniWindow.IsWindowMaximized(uniWindow.UnityWindow))
		{
			return;
		}
		Rect rect = new Rect(0f, 0f, Screen.width, Screen.height);
		rect.xMin += marginPx;
		rect.xMax -= marginPx;
		Vector3 vector = cam.WorldToScreenPoint(hip.position);
		float num = rect.xMin + softZoneLeftPx;
		float num2 = rect.xMax - softZoneRightPx;
		float num3 = 0f;
		if (vector.x < num)
		{
			num3 = vector.x - num;
		}
		else if (vector.x > num2)
		{
			num3 = vector.x - num2;
		}
		if (!(Mathf.Abs(num3) < 0.5f))
		{
			float num4 = Mathf.Clamp(num3, (0f - panSpeedPxPerSec) * Time.unscaledDeltaTime, panSpeedPxPerSec * Time.unscaledDeltaTime);
			float z = Mathf.Max(0.01f, vector.z);
			Vector3 vector2 = cam.ScreenToWorldPoint(new Vector3(vector.x, vector.y, z));
			float x = cam.ScreenToWorldPoint(new Vector3(vector.x - num4, vector.y, z)).x - vector2.x;
			cam.transform.position -= new Vector3(x, 0f, 0f);
			if (moveWindowAlong && !Screen.fullScreen && !uniWindow.IsWindowMaximized(uniWindow.UnityWindow))
			{
				Vector2 windowPosition = uniWindow.GetWindowPosition();
				windowPosition.x += num4;
				uniWindow.SetWindowPosition(Vector2Int.RoundToInt(windowPosition));
			}
		}
	}

	private void RestoreCamera()
	{
		if (!(cam == null))
		{
			cam.transform.position = camPosBeforeDance;
			cam.transform.rotation = camRotBeforeDance;
		}
	}

	private void OnActivateToggleChanged(bool on)
	{
		enableSafety = on;
		if (!on && wasActive)
		{
			RestoreCamera();
			wasActive = false;
		}
	}

	public void SetSafetyEnabled(bool on)
	{
		enableSafety = on;
		if (activateToggle != null)
		{
			activateToggle.SetIsOnWithoutNotify(on);
		}
		if (!on && wasActive)
		{
			RestoreCamera();
			wasActive = false;
		}
	}

	private bool IsActive()
	{
		if (!enableSafety)
		{
			return false;
		}
		if (cam == null)
		{
			return false;
		}
		if (danceHandler == null)
		{
			danceHandler = Object.FindFirstObjectByType<AvatarDanceHandler>();
		}
		bool flag = danceHandler != null && danceHandler.IsPlaying;
		bool flag2 = false;
		if (animator != null)
		{
			AnimatorControllerParameter[] parameters = animator.parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].type == AnimatorControllerParameterType.Bool && parameters[i].nameHash == dancingHash)
				{
					flag2 = animator.GetBool(dancingHash);
					break;
				}
			}
		}
		return flag || flag2;
	}

	private void FetchActiveAvatar(bool force)
	{
		Transform transform = ResolveCurrentModelRoot();
		bool flag = transform != lastModelRoot;
		if (force || wasActive || flag)
		{
			lastModelRoot = transform;
			animator = ResolveAnimator(transform);
			hip = ResolveHip(animator);
		}
	}

	private Transform ResolveCurrentModelRoot()
	{
		VRMLoader vRMLoader = Object.FindFirstObjectByType<VRMLoader>();
		if (vRMLoader != null)
		{
			GameObject currentModel = vRMLoader.GetCurrentModel();
			if (currentModel != null)
			{
				return currentModel.transform;
			}
		}
		GameObject gameObject = GameObject.Find("Model");
		if (gameObject != null)
		{
			return gameObject.transform;
		}
		return null;
	}

	private Animator ResolveAnimator(Transform root)
	{
		if (root != null)
		{
			Animator animator = root.GetComponentsInChildren<Animator>(includeInactive: true).FirstOrDefault((Animator a) => (bool)a && a.gameObject.activeInHierarchy);
			if (animator != null)
			{
				return animator;
			}
		}
		return Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault((Animator a) => (bool)a && a.isActiveAndEnabled);
	}

	private Transform ResolveHip(Animator a)
	{
		if (a != null && a.isHuman)
		{
			Transform boneTransform = a.GetBoneTransform(HumanBodyBones.Hips);
			if (boneTransform != null)
			{
				return boneTransform;
			}
		}
		if (!(a != null))
		{
			return base.transform;
		}
		return a.transform;
	}
}
