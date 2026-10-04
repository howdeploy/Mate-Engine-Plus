using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Xamin;

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

	private void HandleRadialMenu()
	{
		if (radialMenu == null)
		{
			return;
		}
		if (!Input.GetKeyDown(radialMenuKey))
		{
			if (!followBone || !IsRadialOpen() || !(radialRect != null) || !(currentAnimator != null))
			{
				return;
			}
			Transform boneTransform = currentAnimator.GetBoneTransform(targetBone);
			if (boneTransform != null)
			{
				Vector3 b = mainCam.WorldToScreenPoint(boneTransform.position);
				screenPosition = Vector3.Lerp(screenPosition, b, 1f - followSmoothness);
				if (RectTransformUtility.ScreenPointToWorldPointInRectangle(radialRect.parent as RectTransform, screenPosition, mainCam, out var worldPoint))
				{
					radialRect.position = worldPoint;
				}
			}
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
