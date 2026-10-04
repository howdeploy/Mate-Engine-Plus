using UnityEngine;

public class CoffeMug : MonoBehaviour
{
	[SerializeField]
	private GameObject coffeeMesh;

	public bool isFilled => coffeeMesh.activeSelf;

	public void SetMugFilled(bool state)
	{
		coffeeMesh.SetActive(state);
	}
}
