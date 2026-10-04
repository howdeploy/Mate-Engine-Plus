using UnityEngine;
using UnityEngine.UI;

namespace Kirurobo
{
	public class FullscreenSample : MonoBehaviour
	{
		private UniWindowController uniwinc;

		private RectTransform canvasRect;

		private float mouseMoveSS;

		private float mouseMoveSSThreshold = 36f;

		private Vector3 lastMousePosition;

		private float touchDuration;

		private float touchDurationThreshold = 0.5f;

		public Toggle transparentToggle;

		public Toggle topmostToggle;

		public Toggle bottommostToggle;

		public Dropdown fitWindowDropdown;

		public Button quitButton;

		public Button menuCloseButton;

		public RectTransform menuPanel;

		private void Start()
		{
			uniwinc = Object.FindAnyObjectByType<UniWindowController>();
			if ((bool)menuPanel)
			{
				canvasRect = menuPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
			}
			UpdateMonitorDropdown();
			UpdateUI();
			CloseMenu();
			if ((bool)uniwinc)
			{
				transparentToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isTransparent = val;
				});
				topmostToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isTopmost = val;
				});
				bottommostToggle?.onValueChanged.AddListener(delegate(bool val)
				{
					uniwinc.isBottommost = val;
				});
				fitWindowDropdown?.onValueChanged.AddListener(delegate(int val)
				{
					SetFitToMonitor(val);
				});
				quitButton?.onClick.AddListener(Quit);
				menuCloseButton?.onClick.AddListener(CloseMenu);
				uniwinc.OnStateChanged += delegate
				{
					UpdateUI();
				};
				uniwinc.OnMonitorChanged += delegate
				{
					UpdateMonitorDropdown();
					UpdateUI();
				};
			}
		}

		private void Update()
		{
			if (InputProxy.GetMouseButtonDown(1))
			{
				lastMousePosition = InputProxy.mousePosition;
				touchDuration = 0f;
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
				touchDuration = 0f;
			}
			if (Input.touchSupported && Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Began)
				{
					lastMousePosition = Input.mousePosition;
					touchDuration = 0f;
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
					touchDuration = 0f;
				}
			}
			if (InputProxy.GetKeyUp("space") && (bool)menuPanel)
			{
				if (menuPanel.gameObject.activeSelf)
				{
					CloseMenu();
				}
				else
				{
					Vector2 position = new Vector2(Screen.width / 2, Screen.height / 2);
					ShowMenu(position);
				}
			}
			if (InputProxy.GetKeyUp("escape"))
			{
				Quit();
			}
		}

		private void Quit()
		{
			Application.Quit();
		}

		private void SetFitToMonitor(int val)
		{
			if ((bool)uniwinc)
			{
				if (val < 1)
				{
					uniwinc.shouldFitMonitor = false;
					return;
				}
				uniwinc.monitorToFit = val - 1;
				uniwinc.shouldFitMonitor = true;
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
			if (!uniwinc)
			{
				return;
			}
			if ((bool)transparentToggle)
			{
				transparentToggle.isOn = uniwinc.isTransparent;
			}
			if ((bool)topmostToggle)
			{
				topmostToggle.isOn = uniwinc.isTopmost;
			}
			if ((bool)bottommostToggle)
			{
				bottommostToggle.isOn = uniwinc.isBottommost;
			}
			if ((bool)fitWindowDropdown)
			{
				if (uniwinc.shouldFitMonitor)
				{
					fitWindowDropdown.value = uniwinc.monitorToFit + 1;
				}
				else
				{
					fitWindowDropdown.value = 0;
				}
				fitWindowDropdown.RefreshShownValue();
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

		private void ShowEventMessage(string message)
		{
			Debug.Log(message);
		}

		public void OutputMessage(string text)
		{
			Debug.Log(text);
		}
	}
}
