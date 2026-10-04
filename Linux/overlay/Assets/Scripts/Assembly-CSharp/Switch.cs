using UnityEngine;

public class Switch : MonoBehaviour
{
	public float period = 2f;

	public Material lightOnMat;

	public Material lightOffMat;

	public Light light;

	public bool lightOn = true;

	private float curTime;

	private void SwitchLight()
	{
		if (light != null)
		{
			light.enabled = lightOn;
		}
		if (lightOn)
		{
			GetComponent<Renderer>().material = lightOnMat;
		}
		else
		{
			GetComponent<Renderer>().material = lightOffMat;
		}
	}

	private void Start()
	{
		SwitchLight();
	}

	private void Update()
	{
		curTime += Time.deltaTime;
		if (curTime > period)
		{
			curTime = 0f;
			lightOn = !lightOn;
			SwitchLight();
		}
	}
}
