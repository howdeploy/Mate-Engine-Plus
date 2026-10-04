using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class TutorialMenu : MonoBehaviour
{
	[Serializable]
	public class TutorialStep
	{
		[Tooltip("Localization key for the title (e.g. TUTORIAL_PAGE_1_TITLE)")]
		public LocalizedString titleKey;

		[Tooltip("Localization key for the info text (e.g. TUTORIAL_PAGE_1_INFO)")]
		public LocalizedString infoKey;

		public bool showSkipButton;

		public bool showNextButton;

		public bool showBackButton;

		public bool showFinishButton;

		[Tooltip("Show the shared language dropdown in this step")]
		public bool showLanguageDropdown;
	}

	[Header("Main UI Inputs")]
	public TextMeshProUGUI titleText;

	public TextMeshProUGUI infoText;

	public Button skipButton;

	public Button nextButton;

	public Button backButton;

	public Button finishButton;

	[Tooltip("Shared language dropdown shown only if the current step requires it")]
	public TMP_Dropdown languageDropdown;

	[Tooltip("Optional: Root GameObject for the tutorial UI (disabled in editor, enabled at runtime)")]
	public GameObject tutorialRoot;

	[Header("GameObjects to hide while tutorial is active")]
	public List<GameObject> hideWhileTutorialActive = new List<GameObject>();

	[Header("Tutorial Steps")]
	public List<TutorialStep> steps = new List<TutorialStep>();

	private int currentStep;

	private LocalizedString _lastTitleKey;

	private LocalizedString _lastInfoKey;

	public static bool IsActive { get; private set; }

	private void Start()
	{
		if (SaveLoadHandler.Instance != null && SaveLoadHandler.Instance.data.tutorialDone)
		{
			if (tutorialRoot != null)
			{
				tutorialRoot.SetActive(value: false);
			}
			base.gameObject.SetActive(value: false);
			IsActive = false;
			return;
		}
		IsActive = true;
		if (tutorialRoot != null && !tutorialRoot.activeSelf)
		{
			tutorialRoot.SetActive(value: true);
		}
		SetHideTargets(show: false);
		if ((bool)skipButton)
		{
			skipButton.onClick.AddListener(FinishTutorial);
		}
		if ((bool)finishButton)
		{
			finishButton.onClick.AddListener(FinishTutorial);
		}
		if ((bool)nextButton)
		{
			nextButton.onClick.AddListener(NextStep);
		}
		if ((bool)backButton)
		{
			backButton.onClick.AddListener(PreviousStep);
		}
		currentStep = 0;
		ApplyStep();
	}

	private void ApplyStep()
	{
		if (steps == null || steps.Count == 0 || currentStep < 0 || currentStep >= steps.Count)
		{
			Debug.LogWarning("TutorialMenu: No valid steps.");
			return;
		}
		TutorialStep tutorialStep = steps[currentStep];
		if (_lastTitleKey != null)
		{
			_lastTitleKey.StringChanged -= SetTitle;
		}
		if (_lastInfoKey != null)
		{
			_lastInfoKey.StringChanged -= SetInfo;
		}
		_lastTitleKey = tutorialStep.titleKey;
		_lastInfoKey = tutorialStep.infoKey;
		if ((bool)titleText)
		{
			tutorialStep.titleKey.StringChanged += SetTitle;
			tutorialStep.titleKey.RefreshString();
		}
		if ((bool)infoText)
		{
			tutorialStep.infoKey.StringChanged += SetInfo;
			tutorialStep.infoKey.RefreshString();
		}
		if ((bool)skipButton)
		{
			skipButton.gameObject.SetActive(tutorialStep.showSkipButton);
		}
		if ((bool)nextButton)
		{
			nextButton.gameObject.SetActive(tutorialStep.showNextButton);
		}
		if ((bool)backButton)
		{
			backButton.gameObject.SetActive(tutorialStep.showBackButton);
		}
		if ((bool)finishButton)
		{
			finishButton.gameObject.SetActive(tutorialStep.showFinishButton);
		}
		if (languageDropdown != null && languageDropdown.gameObject.activeSelf != tutorialStep.showLanguageDropdown)
		{
			languageDropdown.gameObject.SetActive(tutorialStep.showLanguageDropdown);
		}
	}

	private void SetTitle(string value)
	{
		if (titleText != null && titleText.text != value)
		{
			titleText.text = value;
		}
	}

	private void SetInfo(string value)
	{
		if (infoText != null && infoText.text != value)
		{
			infoText.text = value;
		}
	}

	private void NextStep()
	{
		if (currentStep < steps.Count - 1)
		{
			currentStep++;
			ApplyStep();
		}
	}

	private void PreviousStep()
	{
		if (currentStep > 0)
		{
			currentStep--;
			ApplyStep();
		}
	}

	private void FinishTutorial()
	{
		if (SaveLoadHandler.Instance != null)
		{
			SaveLoadHandler.Instance.data.tutorialDone = true;
			SaveLoadHandler.Instance.SaveToDisk();
		}
		if (tutorialRoot != null)
		{
			tutorialRoot.SetActive(value: false);
		}
		base.gameObject.SetActive(value: false);
		IsActive = false;
		SetHideTargets(show: true);
	}

	private void SetHideTargets(bool show)
	{
		foreach (GameObject item in hideWhileTutorialActive)
		{
			if ((bool)item)
			{
				item.SetActive(show);
			}
		}
	}
}
