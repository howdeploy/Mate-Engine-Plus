using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Kirurobo
{
	public class UiSampleController : MonoBehaviour
	{
		private UniWindowController uniwinc;

		private UniWindowMoveHandle uniWinMoveHandle;

		private RectTransform canvasRect;

		private float mouseMoveSS;

		private float mouseMoveSSThreshold = 36f;

		private Vector3 lastMousePosition;

		private float lastEventOccurredTime = -5f;

		private float eventMessageTimeout = 1f;

		private float touchDuration;

		private float touchDurationThreshold = 0.5f;

		public Toggle transparentToggle;

		public Slider alphaSlider;

		public Toggle topmostToggle;

		public Toggle bottommostToggle;

		[FormerlySerializedAs("maximizedToggle")]
		public Toggle zoomedToggle;

		public Toggle dragMoveToggle;

		public Toggle allowDropToggle;

		public Dropdown fitWindowDropdown;

		public Toggle showBorderlineToggle;

		public Button widthDownButton;

		public Button widthUpButton;

		public Button heightDownButton;

		public Button heightUpButton;

		public Dropdown transparentTypeDropdown;

		public Dropdown hitTestTypeDropdown;

		public Toggle clickThroughToggle;

		public Image pickedColorImage;

		public Text pickedColorText;

		public Text messageText;

		public Text clientSizeText;

		public Button menuCloseButton;

		public RectTransform menuPanel;

		public RectTransform borderlinePanel;

		private void Start()
		{
			uniwinc = UniWindowController.current;
			uniWinMoveHandle = UnityEngine.Object.FindAnyObjectByType<UniWindowMoveHandle>();
			if ((bool)menuPanel)
			{
				canvasRect = menuPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
			}
			UpdateMonitorDropdown();
			UpdateUI();
			if ((bool)uniwinc)
			{
				transparentToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isTransparent = val;
				});
				alphaSlider?.onValueChanged.AddListener(delegate(float val)
				{
					uniwinc.alphaValue = val;
				});
				topmostToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isTopmost = val;
				});
				bottommostToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isBottommost = val;
				});
				zoomedToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isZoomed = val;
				});
				allowDropToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.allowDropFiles = val;
				});
				fitWindowDropdown?.onValueChanged.AddListener(delegate(int val)
				{
					SetFitToMonitor(val);
				});
				widthDownButton?.onClick.AddListener(delegate
				{
					uniwinc.windowSize += new Vector2(-100f, 0f);
				});
				widthUpButton?.onClick.AddListener(delegate
				{
					uniwinc.windowSize += new Vector2(100f, 0f);
				});
				heightDownButton?.onClick.AddListener(delegate
				{
					uniwinc.windowSize += new Vector2(0f, -100f);
				});
				heightUpButton?.onClick.AddListener(delegate
				{
					uniwinc.windowSize += new Vector2(0f, 100f);
				});
				clickThroughToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isClickThrough = val;
				});
				transparentTypeDropdown?.onValueChanged.AddListener(delegate(int val)
				{
					uniwinc.SetTransparentType((UniWindowController.TransparentType)val);
				});
				hitTestTypeDropdown?.onValueChanged.AddListener(delegate(int val)
				{
					uniwinc.hitTestType = (UniWindowController.HitTestType)val;
				});
				menuCloseButton?.onClick.AddListener(CloseMenu);
				if ((bool)uniWinMoveHandle)
				{
					dragMoveToggle?.onValueChanged.AddListener(delegate(bool val)
					{
						uniWinMoveHandle.enabled = val;
					});
				}
				uniwinc.OnStateChanged += delegate(UniWindowController.WindowStateEventType type)
				{
					UpdateUI();
					ShowEventMessage("State changed: " + type);
					ShowClientSize();
				};
				uniwinc.OnMonitorChanged += delegate
				{
					UpdateMonitorDropdown();
					UpdateUI();
					ShowEventMessage("Resolution changed!");
					ShowClientSize();
				};
				uniwinc.OnDropFiles += delegate(string[] files)
				{
					ShowEventMessage(string.Join(Environment.NewLine, files));
				};
			}
			showBorderlineToggle?.onValueChanged.AddListener(delegate(bool val)
			{
				borderlinePanel.gameObject.SetActive(val);
			});
		}

		private void ShowEventMessage(string message)
		{
			lastEventOccurredTime = Time.time;
			if ((bool)messageText)
			{
				messageText.text = message;
			}
			Debug.Log(message);
		}

		private void Update()
		{
			UpdateHitTestUI();
			if (lastEventOccurredTime + eventMessageTimeout < Time.time)
			{
				ShowWindowMetrics();
			}
			if (InputProxy.GetMouseButtonDown(1))
			{
				lastMousePosition = InputProxy.mousePosition;
				ResetTouchDuration();
			}
			if (InputProxy.GetMouseButton(1))
			{
				mouseMoveSS += (InputProxy.mousePosition - lastMousePosition).sqrMagnitude;
			}
			if (InputProxy.GetMouseButtonUp(1))
			{
				if (mouseMoveSS < mouseMoveSSThreshold)
				{
					ShowMenu(lastMousePosition);
				}
				mouseMoveSS = 0f;
				ResetTouchDuration();
			}
			if (Input.touchSupported && Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Began)
				{
					lastMousePosition = Input.mousePosition;
					ResetTouchDuration();
				}
				if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
				{
					mouseMoveSS += touch.deltaPosition.sqrMagnitude;
					touchDuration += touch.deltaTime;
				}
				if (touch.phase == TouchPhase.Ended)
				{
					if (mouseMoveSS < mouseMoveSSThreshold && touchDuration >= touchDurationThreshold)
					{
						ShowMenu(lastMousePosition);
					}
					mouseMoveSS = 0f;
					ResetTouchDuration();
				}
			}
			if ((bool)uniwinc)
			{
				if (InputProxy.GetKeyUp("t"))
				{
					uniwinc.isTransparent = !uniwinc.isTransparent;
				}
				if (InputProxy.GetKeyUp("f"))
				{
					uniwinc.isTopmost = !uniwinc.isTopmost;
				}
				if (InputProxy.GetKeyUp("b"))
				{
					uniwinc.isBottommost = !uniwinc.isBottommost;
				}
				if (InputProxy.GetKeyUp("z"))
				{
					uniwinc.isZoomed = !uniwinc.isZoomed;
				}
			}
			if (InputProxy.GetKeyUp("o"))
			{
				FilePanel.OpenFilePanel(new FilePanel.Settings
				{
					flags = FilePanel.Flag.AllowMultipleSelection,
					title = "Open!",
					filters = new FilePanel.Filter[2]
					{
						new FilePanel.Filter("Image files", "png", "jpg", "jpeg"),
						new FilePanel.Filter("All files", "*")
					},
					initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
					initialFile = "test.png"
				}, delegate(string[] files)
				{
					ShowEventMessage(string.Join(Environment.NewLine, files));
				});
			}
			if (InputProxy.GetKeyUp("s"))
			{
				FilePanel.SaveFilePanel(new FilePanel.Settings
				{
					flags = FilePanel.Flag.AllowMultipleSelection,
					title = "Save!",
					filters = new FilePanel.Filter[0],
					initialFile = "Test.txt",
					initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
				}, delegate(string[] files)
				{
					ShowEventMessage(string.Join(Environment.NewLine, files));
				});
			}
			if (InputProxy.GetKeyUp("escape"))
			{
				Application.Quit();
			}
		}

		private void ResetTouchDuration()
		{
			touchDuration = 0f;
		}

		private void SetFitToMonitor(int val)
		{
			if (!uniwinc)
			{
				return;
			}
			if (val < 1)
			{
				uniwinc.shouldFitMonitor = false;
				if ((bool)zoomedToggle)
				{
					zoomedToggle.interactable = true;
				}
				return;
			}
			uniwinc.monitorToFit = val - 1;
			uniwinc.shouldFitMonitor = true;
			if ((bool)zoomedToggle)
			{
				zoomedToggle.interactable = false;
			}
		}

		private void ShowWindowMetrics()
		{
			if ((bool)uniwinc)
			{
				Vector2 windowPosition = uniwinc.windowPosition;
				string[] obj = new string[8] { "Pos.: ", null, null, null, null, null, null, null };
				Vector2 vector = windowPosition;
				obj[1] = vector.ToString();
				obj[2] = "\nSize: ";
				obj[3] = uniwinc.windowSize.ToString();
				obj[4] = "\nRel. Cur.:";
				obj[5] = (uniwinc.cursorPosition - windowPosition).ToString();
				obj[6] = "\nUnity Cur.:";
				obj[7] = ((Vector2)InputProxy.mousePosition/*cast due to .constrained prefix*/).ToString();
				OutputMessage(string.Concat(obj));
				ShowClientSize();
			}
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus)
			{
				UpdateUI();
				if ((bool)uniwinc)
				{
					OutputMessage("Focused");
				}
				else
				{
					OutputMessage("No UniWindowController");
				}
			}
		}

		private void ShowMenu(Vector2 position)
		{
			if ((bool)menuPanel)
			{
				Vector2 anchoredPosition = position * (canvasRect.sizeDelta.x / (float)Screen.width);
				float width = menuPanel.rect.width;
				float height = menuPanel.rect.height;
				anchoredPosition.y = Mathf.Max(Mathf.Min(anchoredPosition.y, (float)Screen.height - height / 2f), height / 2f);
				anchoredPosition.x = Mathf.Max(Mathf.Min(anchoredPosition.x, (float)Screen.width - width / 2f), width / 2f);
				menuPanel.pivot = Vector2.one * 0.5f;
				menuPanel.anchorMin = Vector2.zero;
				menuPanel.anchorMax = Vector2.zero;
				menuPanel.anchoredPosition = anchoredPosition;
				menuPanel.gameObject.SetActive(value: true);
			}
		}

		private void CloseMenu()
		{
			if ((bool)menuPanel)
			{
				menuPanel.gameObject.SetActive(value: false);
			}
		}

		private void UpdateUI()
		{
			if ((bool)uniwinc)
			{
				if ((bool)transparentToggle)
				{
					transparentToggle.SetIsOnWithoutNotify(uniwinc.isTransparent);
				}
				if ((bool)alphaSlider)
				{
					alphaSlider.SetValueWithoutNotify(uniwinc.alphaValue);
				}
				if ((bool)topmostToggle)
				{
					topmostToggle.SetIsOnWithoutNotify(uniwinc.isTopmost);
				}
				if ((bool)bottommostToggle)
				{
					bottommostToggle.SetIsOnWithoutNotify(uniwinc.isBottommost);
				}
				if ((bool)zoomedToggle)
				{
					zoomedToggle.SetIsOnWithoutNotify(uniwinc.isZoomed);
				}
				if ((bool)allowDropToggle)
				{
					allowDropToggle.SetIsOnWithoutNotify(uniwinc.allowDropFiles);
				}
				if ((bool)dragMoveToggle)
				{
					dragMoveToggle.isOn = (bool)uniWinMoveHandle && uniWinMoveHandle.isActiveAndEnabled;
				}
				if ((bool)fitWindowDropdown)
				{
					if (uniwinc.shouldFitMonitor)
					{
						fitWindowDropdown.value = uniwinc.monitorToFit + 1;
						if ((bool)zoomedToggle)
						{
							zoomedToggle.interactable = false;
						}
					}
					else
					{
						fitWindowDropdown.value = 0;
						if ((bool)zoomedToggle)
						{
							zoomedToggle.interactable = true;
						}
					}
					fitWindowDropdown.RefreshShownValue();
				}
				if ((bool)transparentTypeDropdown)
				{
					transparentTypeDropdown.value = (int)uniwinc.transparentType;
					transparentTypeDropdown.RefreshShownValue();
				}
				if ((bool)hitTestTypeDropdown)
				{
					hitTestTypeDropdown.value = (int)uniwinc.hitTestType;
					hitTestTypeDropdown.RefreshShownValue();
				}
				UpdateHitTestUI();
			}
			if ((bool)showBorderlineToggle && (bool)borderlinePanel)
			{
				borderlinePanel.gameObject.SetActive(showBorderlineToggle.isOn);
			}
		}

		public void UpdateHitTestUI()
		{
			if (!uniwinc)
			{
				return;
			}
			if ((bool)clickThroughToggle)
			{
				clickThroughToggle.SetIsOnWithoutNotify(uniwinc.isClickThrough);
				if (uniwinc.hitTestType == UniWindowController.HitTestType.None)
				{
					clickThroughToggle.interactable = true;
				}
				else
				{
					clickThroughToggle.interactable = false;
				}
			}
			if (uniwinc.hitTestType == UniWindowController.HitTestType.Opacity && uniwinc.isTransparent)
			{
				if ((bool)pickedColorImage)
				{
					pickedColorImage.color = uniwinc.pickedColor;
				}
				if ((bool)pickedColorText)
				{
					pickedColorText.text = $"Alpha:{uniwinc.pickedColor.a:P0}";
					pickedColorText.color = Color.black;
				}
			}
			else
			{
				if ((bool)pickedColorImage)
				{
					pickedColorImage.color = Color.gray;
				}
				if ((bool)pickedColorText)
				{
					pickedColorText.text = "Color picker is disabled";
					pickedColorText.color = Color.gray;
				}
			}
		}

		private void UpdateMonitorDropdown()
		{
			if (!fitWindowDropdown)
			{
				return;
			}
			fitWindowDropdown.options.RemoveRange(1, fitWindowDropdown.options.Count - 1);
			if (!uniwinc)
			{
				fitWindowDropdown.value = 0;
				return;
			}
			int monitorCount = UniWindowController.GetMonitorCount();
			for (int i = 0; i < monitorCount; i++)
			{
				fitWindowDropdown.options.Add(new Dropdown.OptionData("Fit to Monitor " + i));
			}
			if (uniwinc.monitorToFit >= monitorCount)
			{
				uniwinc.monitorToFit = monitorCount - 1;
			}
		}

		public void OutputMessage(string text)
		{
			if ((bool)messageText)
			{
				messageText.text = text;
			}
			else
			{
				Debug.Log(text);
			}
		}

		public void ShowClientSize()
		{
			if ((bool)uniwinc)
			{
				string text = "Client " + uniwinc.clientSize.ToString();
				if ((bool)clientSizeText)
				{
					clientSizeText.text = text;
				}
				else
				{
					Debug.Log(text);
				}
			}
		}
	}
}
