using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Xamin
{
	public class CircleSelector : MonoBehaviour
	{
		public enum ControlType
		{
			mouseAndTouch = 0,
			gamepad = 1,
			customVector = 2
		}

		public enum ButtonSource
		{
			prefabs = 0,
			scene = 1
		}

		public enum AnimationType
		{
			zoomIn = 0,
			zoomOut = 1
		}

		[Range(2f, 10f)]
		private int buttonCount;

		private int startButCount;

		[Header("Customization")]
		public Color AccentColor = Color.red;

		[Header("Customization")]
		public Color DisabledColor = Color.gray;

		[Header("Customization")]
		public Color BackgroundColor = Color.white;

		[Space(10f)]
		public bool UseSeparators = true;

		[SerializeField]
		private GameObject separatorPrefab;

		[Header("Animations")]
		[Range(0.0001f, 1f)]
		public float LerpAmount = 0.145f;

		public AnimationType OpenAnimation;

		public AnimationType CloseAnimation;

		public float Size = 1f;

		private Image _cursor;

		private Image _background;

		private float _desiredFill;

		private float radius = 120f;

		[Header("Interaction")]
		public List<GameObject> Buttons = new List<GameObject>();

		public ButtonSource buttonSource;

		private List<Button> buttonsInstances = new List<Button>();

		private Vector2 _menuCenter;

		public bool RaiseOnSelection;

		private GameObject _selectedSegment;

		private bool _previousUseSeparators;

		public bool flip = true;

		public bool selectOnlyOnHover;

		public float pieThickness = 85f;

		public bool snap;

		public bool tiltTowardsMouse;

		public float tiltAmount = 15f;

		private bool opened;

		[Header("Controls")]
		public string activationButton = "Fire1";

		public ControlType controlType;

		public string gamepadAxisX;

		public string gamepadAxisY;

		public Vector2 CustomInputVector;

		private Dictionary<GameObject, Button> instancedButtons;

		private AvatarAnimatorReceiver animatorReceiver;

		public float zRotation = 180f;

		public bool rotateButtons;

		private List<bool> lastButtonVisibility = new List<bool>();

		[HideInInspector]
		public GameObject SelectedSegment
		{
			get
			{
				return _selectedSegment;
			}
			set
			{
				if (value != null && value != SelectedSegment)
				{
					_selectedSegment = value;
				}
			}
		}

		private void Start()
		{
			instancedButtons = new Dictionary<GameObject, Button>();
			base.transform.localScale = Vector3.zero;
			_cursor = base.transform.Find("Cursor").GetComponent<Image>();
			_background = base.transform.Find("Background").GetComponent<Image>();
			EnsureAnimatorReceiver();
			BuildButtons();
		}

		public bool Open()
		{
			RefreshAllButtonColorsDelayed();
			EnsureAnimatorReceiver();
			BuildButtons();
			if (buttonsInstances.Count == 0)
			{
				opened = false;
				base.transform.localScale = Vector3.zero;
				return false;
			}
			_menuCenter = new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
			opened = true;
			base.transform.localScale = ((OpenAnimation == AnimationType.zoomIn) ? Vector3.zero : (Vector3.one * 10f));
			return true;
		}

		public bool Open(Vector2 origin)
		{
			if (!Open())
			{
				return false;
			}
			_menuCenter = origin;
			base.transform.localPosition = _menuCenter - new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
			return true;
		}

		public void Close()
		{
			opened = false;
		}

		public Button GetButtonWithId(string id)
		{
			return buttonsInstances.Find((Button btn) => btn.id == id);
		}

		private void ChangeSeparatorsState()
		{
			Transform transform = base.transform.Find("Separators");
			if ((bool)transform)
			{
				transform.gameObject.SetActive(UseSeparators);
			}
			else
			{
				Debug.LogError("Can't find Separators");
			}
		}

		private void Update()
		{
			if (opened && Buttons != null && Buttons.Count > 0)
			{
				bool flag = false;
				if (lastButtonVisibility.Count != Buttons.Count)
				{
					lastButtonVisibility = new List<bool>(new bool[Buttons.Count]);
				}
				for (int i = 0; i < Buttons.Count; i++)
				{
					Button button = ((Buttons[i] != null) ? Buttons[i].GetComponent<Button>() : null);
					bool flag2 = button != null && !ShouldHideButton(button);
					if (flag2 != lastButtonVisibility[i])
					{
						flag = true;
						lastButtonVisibility[i] = flag2;
					}
				}
				if (flag)
				{
					BuildButtons();
					return;
				}
			}
			if (opened)
			{
				base.transform.localScale = Vector3.Lerp(base.transform.localScale, Vector3.one * Size, 0.2f);
				if (Vector3.Distance(base.transform.localScale, Vector3.one * Size) < 0.001f)
				{
					base.transform.localScale = Vector3.one * Size;
				}
				_background.color = BackgroundColor;
				if (UseSeparators != _previousUseSeparators)
				{
					ChangeSeparatorsState();
				}
				if (base.transform.localScale.x >= Size - 0.2f)
				{
					buttonCount = buttonsInstances.Count;
					if (startButCount != buttonCount && buttonSource == ButtonSource.prefabs)
					{
						Start();
						return;
					}
					_cursor.fillAmount = Mathf.Lerp(_cursor.fillAmount, _desiredFill, 0.2f);
					Vector3 vector = Camera.main.WorldToScreenPoint(base.transform.position);
					Vector2 vector2 = Input.mousePosition - vector;
					if (tiltTowardsMouse)
					{
						float num = vector2.x / vector.x;
						Vector3 euler = new Vector3(vector2.y / vector.y, 0f - num, 0f) * (0f - tiltAmount) + new Vector3(0f, 0f, zRotation);
						base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, Quaternion.Euler(euler), LerpAmount);
					}
					else
					{
						base.transform.localRotation = Quaternion.Euler(Vector3.forward * zRotation);
					}
					float num2 = zRotation + 57.29578f * ((controlType == ControlType.mouseAndTouch) ? Mathf.Atan2(vector2.x, vector2.y) : ((controlType == ControlType.gamepad) ? Mathf.Atan2(Input.GetAxis(gamepadAxisX), Input.GetAxis(gamepadAxisY)) : Mathf.Atan2(CustomInputVector.x, CustomInputVector.y)));
					if (num2 < 0f)
					{
						num2 += 360f;
					}
					float z = 0f - (num2 - _cursor.fillAmount * 360f / 2f) + zRotation;
					float num3 = Vector2.Distance(Camera.main.WorldToScreenPoint(base.transform.position), Input.mousePosition);
					if ((selectOnlyOnHover && controlType == ControlType.mouseAndTouch && num3 > pieThickness) || (selectOnlyOnHover && controlType == ControlType.gamepad && Mathf.Abs(Input.GetAxisRaw(gamepadAxisX) + Mathf.Abs(Input.GetAxisRaw(gamepadAxisY))) != 0f) || !selectOnlyOnHover)
					{
						_cursor.enabled = true;
						float num4 = 3.4028235E+38f;
						GameObject selectedSegment = null;
						for (int j = 0; j < buttonCount; j++)
						{
							GameObject gameObject = buttonsInstances[j].gameObject;
							gameObject.transform.localScale = Vector3.one;
							float num5 = Mathf.Abs(Convert.ToSingle(gameObject.name) - num2);
							if (num5 < num4)
							{
								selectedSegment = gameObject;
								num4 = num5;
							}
							if (rotateButtons)
							{
								gameObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f - zRotation);
							}
						}
						SelectedSegment = selectedSegment;
						if (snap && SelectedSegment != null)
						{
							z = 0f - (Convert.ToSingle(SelectedSegment.name) - _cursor.fillAmount * 360f / 2f);
						}
						_cursor.transform.localRotation = Quaternion.Slerp(_cursor.transform.localRotation, Quaternion.Euler(0f, 0f, z), LerpAmount);
						if (SelectedSegment != null && instancedButtons.ContainsKey(SelectedSegment))
						{
							instancedButtons[SelectedSegment].SetColor(Color.Lerp(instancedButtons[SelectedSegment].currentColor, BackgroundColor, LerpAmount));
						}
						for (int k = 0; k < buttonCount; k++)
						{
							Button button2 = buttonsInstances[k];
							if (button2.gameObject != SelectedSegment)
							{
								button2.SetColor(Color.Lerp(button2.currentColor, (!button2.unlocked) ? DisabledColor : (button2.useCustomColor ? button2.customColor : AccentColor), LerpAmount));
							}
						}
						try
						{
							if (SelectedSegment != null && instancedButtons.TryGetValue(SelectedSegment, out var value) && value.unlocked)
							{
								_cursor.color = Color.Lerp(_cursor.color, value.useCustomColor ? value.customColor : AccentColor, LerpAmount);
							}
							else
							{
								_cursor.color = Color.Lerp(_cursor.color, DisabledColor, LerpAmount);
							}
						}
						catch
						{
						}
					}
					else if (_cursor.enabled && SelectedSegment != null)
					{
						_cursor.enabled = false;
						if (instancedButtons.TryGetValue(SelectedSegment, out var value2))
						{
							value2.SetColor((!value2.unlocked) ? DisabledColor : (value2.useCustomColor ? value2.customColor : AccentColor));
						}
						for (int l = 0; l < buttonCount; l++)
						{
							Button button3 = buttonsInstances[l];
							if (button3.gameObject != SelectedSegment)
							{
								button3.SetColor((!button3.unlocked) ? DisabledColor : (buttonsInstances[(SelectedSegment != null) ? buttonsInstances.IndexOf(value2) : 0].useCustomColor ? buttonsInstances[(SelectedSegment != null) ? buttonsInstances.IndexOf(value2) : 0].customColor : AccentColor));
							}
						}
					}
					if (_cursor.isActiveAndEnabled)
					{
						CheckForInput();
					}
					else if (Input.GetButtonUp(activationButton))
					{
						Close();
					}
				}
				_previousUseSeparators = UseSeparators;
			}
			else
			{
				base.transform.localScale = Vector3.Lerp(base.transform.localScale, (CloseAnimation == AnimationType.zoomIn) ? Vector3.zero : (Vector3.one * 10f), 0.2f);
				Vector3 vector3 = ((CloseAnimation == AnimationType.zoomIn) ? Vector3.zero : (Vector3.one * 10f));
				if (Vector3.Distance(base.transform.localScale, vector3) < 0.001f)
				{
					base.transform.localScale = vector3;
				}
				_cursor.color = Color.Lerp(_cursor.color, Color.clear, LerpAmount / 3f);
				_background.color = Color.Lerp(_background.color, Color.clear, LerpAmount / 3f);
			}
		}

		public void RefreshAllButtonColors()
		{
			if (buttonsInstances.Count == 0)
			{
				return;
			}
			foreach (Button buttonsInstance in buttonsInstances)
			{
				buttonsInstance.SetColor(Color.Lerp(buttonsInstance.currentColor, buttonsInstance.unlocked ? (buttonsInstance.useCustomColor ? buttonsInstance.customColor : AccentColor) : DisabledColor, LerpAmount));
			}
		}

		public void RefreshAllButtonColorsDelayed()
		{
			StartCoroutine(DoRefreshAllButtonColorsDelayed());
		}

		private IEnumerator DoRefreshAllButtonColorsDelayed()
		{
			yield return null;
			RefreshAllButtonColors();
		}

		private void CheckForInput()
		{
			if (SelectedSegment == null || instancedButtons == null || !instancedButtons.ContainsKey(SelectedSegment))
			{
				return;
			}
			Button button = instancedButtons[SelectedSegment];
			_cursor.rectTransform.localPosition = Vector3.Lerp(_cursor.rectTransform.localPosition, Input.GetButton(activationButton) ? new Vector3(0f, 0f, RaiseOnSelection ? (-10) : 0) : Vector3.zero, LerpAmount);
			if (Input.GetButton(activationButton) && button.unlocked)
			{
				SelectedSegment.transform.localScale = new Vector2(0.8f, 0.8f);
			}
			if (!Input.GetButtonUp(activationButton))
			{
				return;
			}
			if (button.unlocked)
			{
				button.ExecuteAction();
				MenuAudioHandler menuAudioHandler = UnityEngine.Object.FindFirstObjectByType<MenuAudioHandler>();
				if (menuAudioHandler != null)
				{
					menuAudioHandler.PlayButtonSound();
				}
			}
			Close();
		}

		private void EnsureAnimatorReceiver()
		{
			RefreshAllButtonColorsDelayed();
			animatorReceiver = null;
			AvatarAnimatorReceiver[] array = UnityEngine.Object.FindObjectsByType<AvatarAnimatorReceiver>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (AvatarAnimatorReceiver avatarAnimatorReceiver in array)
			{
				if (avatarAnimatorReceiver != null && avatarAnimatorReceiver.isActiveAndEnabled && avatarAnimatorReceiver.gameObject.activeInHierarchy && avatarAnimatorReceiver.avatarAnimator != null && avatarAnimatorReceiver.avatarAnimator.isActiveAndEnabled && avatarAnimatorReceiver.avatarAnimator.gameObject.activeInHierarchy)
				{
					animatorReceiver = avatarAnimatorReceiver;
					break;
				}
			}
		}

		private void BuildButtons()
		{
			RefreshAllButtonColorsDelayed();
			foreach (Transform item in base.transform.Find("Buttons"))
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
			foreach (Transform item2 in base.transform.Find("Separators"))
			{
				UnityEngine.Object.Destroy(item2.gameObject);
			}
			buttonsInstances.Clear();
			instancedButtons = new Dictionary<GameObject, Button>();
			int num = 0;
			List<GameObject> list = new List<GameObject>();
			foreach (GameObject button in Buttons)
			{
				GameObject gameObject = ((buttonSource == ButtonSource.prefabs) ? UnityEngine.Object.Instantiate(button, Vector2.zero, base.transform.rotation) : button);
				Button component = gameObject.GetComponent<Button>();
				if (ShouldHideButton(component))
				{
					UnityEngine.Object.Destroy(gameObject);
					continue;
				}
				list.Add(gameObject);
				num++;
			}
			buttonCount = num;
			if (buttonCount > 0 && buttonCount < 11)
			{
				startButCount = buttonCount;
				_desiredFill = 1f / (float)buttonCount;
				float num2 = _desiredFill * 360f;
				float num3 = 0f;
				for (int i = 0; i < num; i++)
				{
					GameObject gameObject2 = list[i];
					Button component2 = gameObject2.GetComponent<Button>();
					gameObject2.transform.SetParent(base.transform.Find("Buttons"));
					float num4 = num3 + num2 / 2f;
					num3 = num4 + num2 / 2f;
					GameObject obj = UnityEngine.Object.Instantiate(separatorPrefab, Vector3.zero, Quaternion.identity);
					obj.transform.SetParent(base.transform.Find("Separators"));
					obj.transform.localScale = Vector3.one;
					obj.transform.localPosition = Vector3.zero;
					obj.transform.localRotation = Quaternion.Euler(0f, 0f, num3);
					gameObject2.transform.localPosition = new Vector2(radius * Mathf.Cos((num4 - 90f) * ((float)Math.PI / 180f)), (0f - radius) * Mathf.Sin((num4 - 90f) * ((float)Math.PI / 180f)));
					gameObject2.transform.localScale = Vector3.one;
					if (num4 > 360f)
					{
						num4 -= 360f;
					}
					gameObject2.name = num4.ToString();
					if ((bool)component2)
					{
						instancedButtons[gameObject2] = component2;
						component2.SetColor(component2.useCustomColor ? component2.customColor : AccentColor);
						buttonsInstances.Add(component2);
					}
					else
					{
						gameObject2.GetComponent<Image>().color = DisabledColor;
					}
				}
			}
			SelectedSegment = ((buttonsInstances.Count != 0) ? buttonsInstances[buttonsInstances.Count - 1].gameObject : null);
			if (SelectedSegment == null)
			{
				opened = false;
				base.transform.localScale = Vector3.zero;
			}
		}

		private bool ShouldHideButton(Button btn)
		{
			if (animatorReceiver == null || animatorReceiver.avatarAnimator == null)
			{
				return false;
			}
			Animator avatarAnimator = animatorReceiver.avatarAnimator;
			if (btn.showOnlyIfAnimatorBool != null && btn.showOnlyIfAnimatorBool.Length != 0)
			{
				bool flag = false;
				string[] showOnlyIfAnimatorBool = btn.showOnlyIfAnimatorBool;
				foreach (string text in showOnlyIfAnimatorBool)
				{
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					AnimatorControllerParameter[] parameters = avatarAnimator.parameters;
					foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
					{
						if (animatorControllerParameter.type == AnimatorControllerParameterType.Bool && animatorControllerParameter.name == text && avatarAnimator.GetBool(text))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (!flag)
				{
					return true;
				}
			}
			if (btn.showOnlyIfStateName != null && btn.showOnlyIfStateName.Length != 0)
			{
				AnimatorStateInfo currentAnimatorStateInfo = avatarAnimator.GetCurrentAnimatorStateInfo(0);
				bool flag2 = false;
				string[] showOnlyIfAnimatorBool = btn.showOnlyIfStateName;
				foreach (string value in showOnlyIfAnimatorBool)
				{
					if (!string.IsNullOrEmpty(value) && currentAnimatorStateInfo.IsName(value))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					return true;
				}
			}
			if (btn.hideIfAnimatorBool != null)
			{
				string[] showOnlyIfAnimatorBool = btn.hideIfAnimatorBool;
				foreach (string text2 in showOnlyIfAnimatorBool)
				{
					if (string.IsNullOrEmpty(text2))
					{
						continue;
					}
					AnimatorControllerParameter[] parameters = avatarAnimator.parameters;
					foreach (AnimatorControllerParameter animatorControllerParameter2 in parameters)
					{
						if (animatorControllerParameter2.type == AnimatorControllerParameterType.Bool && animatorControllerParameter2.name == text2 && avatarAnimator.GetBool(text2))
						{
							return true;
						}
					}
				}
			}
			if (btn.hideIfStateName != null)
			{
				AnimatorStateInfo currentAnimatorStateInfo2 = avatarAnimator.GetCurrentAnimatorStateInfo(0);
				string[] showOnlyIfAnimatorBool = btn.hideIfStateName;
				foreach (string value2 in showOnlyIfAnimatorBool)
				{
					if (!string.IsNullOrEmpty(value2) && currentAnimatorStateInfo2.IsName(value2))
					{
						return true;
					}
				}
			}
			if (btn != null && btn.id == "clothes")
			{
				GameObject gameObject = animatorReceiver?.avatarAnimator?.gameObject;
				if (!(gameObject != null) || !((gameObject.GetComponent<MEClothes>() ?? gameObject.GetComponentInChildren<MEClothes>(includeInactive: true)) != null))
				{
					return true;
				}
			}
			return false;
		}
	}
}
