using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
	public class KnowledgeBaseGameUI : MonoBehaviour
	{
		[Header("UI elements")]
		public Dropdown CharacterSelect;

		public InputField PlayerText;

		public Text AIText;

		[Header("Bot texts")]
		public TextAsset ButlerText;

		public TextAsset MaidText;

		public TextAsset ChefText;

		[Header("Bot images")]
		public RawImage ButlerImage;

		public RawImage MaidImage;

		public RawImage ChefImage;

		[Header("Buttons")]
		public Button NotesButton;

		public Button MapButton;

		public Button SolveButton;

		public Button HelpButton;

		public Button SubmitButton;

		[Header("Panels")]
		public RawImage NotebookImage;

		public GameObject NotesPanel;

		public GameObject SolvePanel;

		public GameObject HelpPanel;

		public RawImage MapImage;

		public RawImage SuccessImage;

		public Text FailText;

		public Dropdown Answer1;

		public Dropdown Answer2;

		public Dropdown Answer3;

		protected void Start()
		{
			AddListeners();
		}

		private void OnValueChanged(string newText)
		{
			if (Input.GetKey(KeyCode.Return) && PlayerText.text.Trim() == "")
			{
				PlayerText.text = "";
			}
		}

		protected virtual void AddListeners()
		{
			CharacterSelect.onValueChanged.AddListener(DropdownChange);
			NotesButton.onClick.AddListener(ShowNotes);
			MapButton.onClick.AddListener(ShowMap);
			SolveButton.onClick.AddListener(ShowSolve);
			HelpButton.onClick.AddListener(ShowHelp);
			SubmitButton.onClick.AddListener(SubmitAnswer);
			Answer1.onValueChanged.AddListener(HideFail);
			Answer2.onValueChanged.AddListener(HideFail);
			Answer3.onValueChanged.AddListener(HideFail);
			PlayerText.onSubmit.AddListener(OnInputFieldSubmit);
			PlayerText.onValueChanged.AddListener(OnValueChanged);
		}

		protected virtual void DropdownChange(int selection)
		{
		}

		protected virtual void OnInputFieldSubmit(string question)
		{
		}

		private void ShowNotes()
		{
			NotesPanel.gameObject.SetActive(value: true);
			HelpPanel.gameObject.SetActive(value: false);
			SolvePanel.gameObject.SetActive(value: false);
			NotebookImage.gameObject.SetActive(value: true);
		}

		private void ShowMap()
		{
			MapImage.gameObject.SetActive(value: true);
		}

		private void HideFail(int selection)
		{
			FailText.gameObject.SetActive(value: false);
		}

		private void ShowSolve()
		{
			HideFail(0);
			NotesPanel.gameObject.SetActive(value: false);
			HelpPanel.gameObject.SetActive(value: false);
			SolvePanel.gameObject.SetActive(value: true);
			NotebookImage.gameObject.SetActive(value: true);
		}

		private void ShowHelp()
		{
			NotesPanel.gameObject.SetActive(value: false);
			HelpPanel.gameObject.SetActive(value: true);
			SolvePanel.gameObject.SetActive(value: false);
			NotebookImage.gameObject.SetActive(value: true);
		}

		private void SubmitAnswer()
		{
			if (Answer1.options[Answer1.value].text == "Professor Pluot" && Answer2.options[Answer2.value].text == "Living Room" && Answer3.options[Answer3.value].text == "A Hollow Bible")
			{
				NotebookImage.gameObject.SetActive(value: false);
				SuccessImage.gameObject.SetActive(value: true);
			}
			else
			{
				FailText.gameObject.SetActive(value: true);
			}
		}

		private void Update()
		{
			if (!Input.GetMouseButtonDown(0))
			{
				return;
			}
			RawImage[] array = new RawImage[3] { NotebookImage, MapImage, SuccessImage };
			foreach (RawImage rawImage in array)
			{
				if (rawImage.IsActive() && !RectTransformUtility.RectangleContainsScreenPoint(rawImage.rectTransform, Input.mousePosition))
				{
					rawImage.gameObject.SetActive(value: false);
				}
			}
		}
	}
}
