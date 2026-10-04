using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSLimiter : MonoBehaviour
{
	[Range(15f, 165f)]
	public int targetFPS = 60;

	[Header("UI References")]
	public Slider fpsSlider;

	public TextMeshProUGUI fpsLabel;

	private int previousFPS;

	private void Start()
	{
		targetFPS = PlayerPrefs.GetInt("FPSLimit", targetFPS);
		if ((bool)fpsSlider)
		{
			fpsSlider.minValue = 15f;
			fpsSlider.maxValue = 165f;
			fpsSlider.value = targetFPS;
			fpsSlider.onValueChanged.AddListener(SetFPSLimit);
		}
		ApplyFPSLimit();
		UpdateFPSLabel(targetFPS);
	}

	private void Update()
	{
		if (targetFPS != previousFPS)
		{
			ApplyFPSLimit();
		}
	}

	public void ApplyFPSLimit()
	{
		Application.targetFrameRate = targetFPS;
		QualitySettings.vSyncCount = 0;
		previousFPS = targetFPS;
		PlayerPrefs.SetInt("FPSLimit", targetFPS);
		PlayerPrefs.Save();
		UpdateFPSLabel(targetFPS);
		Debug.Log("FPS set to: " + targetFPS);
	}

	public void SetFPSLimit(float fps)
	{
		targetFPS = Mathf.RoundToInt(Mathf.Clamp(fps, 15f, 165f));
		ApplyFPSLimit();
	}

	private void UpdateFPSLabel(int fpsValue)
	{
		if ((bool)fpsLabel)
		{
			fpsLabel.text = $"{fpsValue}";
		}
	}
}
