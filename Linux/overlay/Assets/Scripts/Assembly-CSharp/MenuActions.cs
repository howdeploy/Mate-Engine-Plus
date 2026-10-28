using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Xamin;

[DefaultExecutionOrder(100)]
public class MenuActions : MonoBehaviour
{
	[Header("Menus")]
	public List<MenuEntry> menuEntries = new List<MenuEntry>();

	[Header("Lock Canvas")]
	public GameObject moveCanvas;

	[Header("Radial Menu")]
	public GameObject radialMenuObject;

	public bool radialBlockMovement = true;

	public bool radialBlockHandTracking;

	public bool radialBlockReaction;

	public bool radialBlockChibiMode;

	public KeyCode radialMenuKey = KeyCode.F1;

	public bool radialDraggingBlocks = true;

	[Header("Bone Follow")]
	public bool followBone = true;

	public HumanBodyBones targetBone = HumanBodyBones.Head;

	[Range(0f, 1f)]
	public float followSmoothness = 0.15f;

	[Header("Perf")]
	public float avatarScanInterval = 0.25f;

	private static readonly List<MenuActions> Instances = new List<MenuActions>();

	private CircleSelector radialMenu;

	private RectTransform radialRect;

	private float radialSize;

	private readonly Vector3[] radialCorners = new Vector3[4];

	private Camera mainCam;

	private Transform modelRoot;

	private GameObject currentModel;

	private Animator currentAnimator;

	private Vector3 screenPosition;

	private AvatarBigScreenHandler bigScreen;

	private FieldInfo bigScreenActiveField;

	private bool lastMoveCanvasState;

	private float nextAvatarScan;

	private bool entryOpenPrevFrame;

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private void Start()
	{
		if (radialMenuObject != null)
		{
			radialMenu = radialMenuObject.GetComponent<CircleSelector>();
			radialRect = radialMenuObject.GetComponent<RectTransform>();
			if (radialMenu != null) radialSize = radialMenu.Size;
		}
		modelRoot = GameObject.Find("Model")?.transform;
		mainCam = Camera.main;
		CacheBigScreen();
		nextAvatarScan = Time.unscaledTime;
		entryOpenPrevFrame = AnyEntryOpenLocal();
	}

	private void Update()
	{
		if (Time.unscaledTime >= nextAvatarScan)
		{
			UpdateCurrentAvatar();
			nextAvatarScan = Time.unscaledTime + avatarScanInterval;
		}
		if (moveCanvas != null)
		{
			bool flag = !IsBigScreenActive() && !IsMovementBlocked() && !TutorialMenu.IsActive;
			if (flag != lastMoveCanvasState)
			{
				moveCanvas.SetActive(flag);
				lastMoveCanvasState = flag;
			}
		}
		HandleRadialMenu();
		entryOpenPrevFrame = AnyEntryOpenLocal();
	}

	private void LateUpdate()
	{
		if (radialMenu == null || radialRect == null || mainCam == null || !IsRadialOpen()) return;
		if (followBone && currentAnimator != null)
		{
			Transform target = currentAnimator.GetBoneTransform(targetBone);
			if (target != null)
				screenPosition = Vector3.Lerp(screenPosition, mainCam.WorldToScreenPoint(target.position), 1f - followSmoothness);
		}
		PlaceRadialMenu();
	}

	private void PlaceRadialMenu()
	{
		var parent = radialRect.parent as RectTransform;
		if (parent == null || mainCam == null || radialSize <= 0f) return;
		Vector2 desired = followBone ? (Vector2)screenPosition : RectTransformUtility.WorldToScreenPoint(mainCam, radialRect.position);
		Rect safe = UiTooltip.VisibleScreenRect(desired);
		if (safe.width <= 0f || safe.height <= 0f) return;
		if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, desired, mainCam, out var world)) return;
		radialRect.position = world;
		// Use the full-open size, not the animated scale (which starts at zero).
		radialRect.GetLocalCorners(radialCorners);
		Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		for (int i = 0; i < radialCorners.Length; i++)
		{
			Vector3 corner = parent.TransformPoint(radialRect.localPosition +
				radialRect.localRotation * (radialCorners[i] * radialSize));
			Vector2 point = RectTransformUtility.WorldToScreenPoint(mainCam, corner);
			minimum = Vector2.Min(minimum, point);
			maximum = Vector2.Max(maximum, point);
		}
		Vector2 size = maximum - minimum;
		float fit = Mathf.Min(1f, Mathf.Min(safe.width / Mathf.Max(1f, size.x), safe.height / Mathf.Max(1f, size.y)));
		radialMenu.Size = radialSize * fit;
		minimum = desired + (minimum - desired) * fit;
		maximum = desired + (maximum - desired) * fit;
		Vector2 correction = new Vector2(
			minimum.x < safe.xMin ? safe.xMin - minimum.x : maximum.x > safe.xMax ? safe.xMax - maximum.x : 0f,
			minimum.y < safe.yMin ? safe.yMin - minimum.y : maximum.y > safe.yMax ? safe.yMax - maximum.y : 0f);
		if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, desired + correction, mainCam, out world))
			radialRect.position = world;
	}

	private void HandleRadialMenu()
	{
		if (radialMenu == null)
		{
			return;
		}
		if (!Input.GetKeyDown(radialMenuKey))
		{
			return;
		}
		else
		{
			if (radialDraggingBlocks && currentAnimator != null && currentAnimator.GetBool("isDragging"))
			{
				return;
			}
			bool flag = AnyEntryOpenLocal();
			if (IsRadialOpen())
			{
				radialMenu.Close();
				PlayMenuCloseSound();
			}
			else if (flag)
			{
				CloseAllMenus();
				PlayMenuCloseSound();
			}
			else
			{
				if (entryOpenPrevFrame && !flag)
				{
					return;
				}
				CloseOtherRadials();
				if (followBone && currentAnimator != null)
				{
					Transform boneTransform2 = currentAnimator.GetBoneTransform(targetBone);
					if (boneTransform2 != null)
					{
						screenPosition = mainCam.WorldToScreenPoint(boneTransform2.position);
						if (RectTransformUtility.ScreenPointToWorldPointInRectangle(radialRect.parent as RectTransform, screenPosition, mainCam, out var worldPoint2))
						{
							radialRect.position = worldPoint2;
						}
					}
				}
				if (radialMenu.Open())
				{
					PlaceRadialMenu();
					PlayMenuOpenSound();
				}
			}
		}
	}

	private void CacheBigScreen()
	{
		bigScreen = Object.FindFirstObjectByType<AvatarBigScreenHandler>();
		if (bigScreen != null)
		{
			bigScreenActiveField = typeof(AvatarBigScreenHandler).GetField("isBigScreenActive", BindingFlags.Instance | BindingFlags.NonPublic);
		}
	}

	private bool IsBigScreenActive()
	{
		if (bigScreen == null || bigScreenActiveField == null)
		{
			return false;
		}
		object value = bigScreenActiveField.GetValue(bigScreen);
		bool flag = default(bool);
		int num;
		if (value is bool)
		{
			flag = (bool)value;
			num = 1;
		}
		else
		{
			num = 0;
		}
		return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
	}

	private void UpdateCurrentAvatar()
	{
		if (!modelRoot)
		{
			return;
		}
		for (int i = 0; i < modelRoot.childCount; i++)
		{
			GameObject gameObject = modelRoot.GetChild(i).gameObject;
			if (gameObject.activeInHierarchy)
			{
				if (!(currentModel == gameObject))
				{
					currentModel = gameObject;
					currentAnimator = currentModel.GetComponent<Animator>();
				}
				break;
			}
		}
	}

	private bool AnyEntryOpenLocal()
	{
		for (int i = 0; i < menuEntries.Count; i++)
		{
			GameObject menu = menuEntries[i].menu;
			if ((bool)menu && menu.activeInHierarchy)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsRadialOpen()
	{
		if ((bool)radialMenuObject)
		{
			return radialMenuObject.transform.localScale.x > 0.01f;
		}
		return false;
	}

	private void CloseOtherRadials()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (!(menuActions == null) && !(menuActions == this) && menuActions.IsRadialOpen())
			{
				menuActions.radialMenu?.Close();
			}
		}
	}

	public void CloseAllMenus()
	{
		for (int i = 0; i < menuEntries.Count; i++)
		{
			GameObject menu = menuEntries[i].menu;
			if ((bool)menu)
			{
				menu.SetActive(value: false);
			}
		}
		if (IsRadialOpen())
		{
			radialMenu?.Close();
		}
	}

	public static bool IsMovementBlocked()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (menuActions == null)
			{
				continue;
			}
			if (menuActions.IsRadialOpen() && menuActions.radialBlockMovement)
			{
				return true;
			}
			List<MenuEntry> list = menuActions.menuEntries;
			for (int j = 0; j < list.Count; j++)
			{
				if ((bool)list[j].menu && list[j].menu.activeInHierarchy && list[j].blockMovement)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsHandTrackingBlocked()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (menuActions == null)
			{
				continue;
			}
			if (menuActions.IsRadialOpen() && menuActions.radialBlockHandTracking)
			{
				return true;
			}
			List<MenuEntry> list = menuActions.menuEntries;
			for (int j = 0; j < list.Count; j++)
			{
				if ((bool)list[j].menu && list[j].menu.activeInHierarchy && list[j].blockHandTracking)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsReactionBlocked()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (menuActions == null)
			{
				continue;
			}
			if (menuActions.IsRadialOpen() && menuActions.radialBlockReaction)
			{
				return true;
			}
			List<MenuEntry> list = menuActions.menuEntries;
			for (int j = 0; j < list.Count; j++)
			{
				if ((bool)list[j].menu && list[j].menu.activeInHierarchy && list[j].blockReaction)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsChibiModeBlocked()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (menuActions == null)
			{
				continue;
			}
			if (menuActions.IsRadialOpen() && menuActions.radialBlockChibiMode)
			{
				return true;
			}
			List<MenuEntry> list = menuActions.menuEntries;
			for (int j = 0; j < list.Count; j++)
			{
				if ((bool)list[j].menu && list[j].menu.activeInHierarchy && list[j].blockChibiMode)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool IsAnyMenuOpen()
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			MenuActions menuActions = Instances[i];
			if (menuActions == null)
			{
				continue;
			}
			if (menuActions.IsRadialOpen())
			{
				return true;
			}
			List<MenuEntry> list = menuActions.menuEntries;
			for (int j = 0; j < list.Count; j++)
			{
				GameObject menu = list[j].menu;
				if ((bool)menu && menu.activeInHierarchy)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void PlayMenuOpenSound()
	{
		Object.FindFirstObjectByType<MenuAudioHandler>()?.PlayOpenSound();
	}

	private void PlayMenuCloseSound()
	{
		Object.FindFirstObjectByType<MenuAudioHandler>()?.PlayCloseSound();
	}
}
