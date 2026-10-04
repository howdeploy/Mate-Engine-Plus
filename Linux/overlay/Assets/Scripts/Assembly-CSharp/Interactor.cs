using UnityEngine;
using UnityEngine.UI;
using Xamin;
using Xamin.Demo;

[RequireComponent(typeof(Camera))]
public class Interactor : MonoBehaviour
{
	public MouseLook mouseLook;

	public CircleSelector menu;

	public Text guiText;

	private Camera _cam;

	private CoffeMug currentMug;

	private void Start()
	{
		_cam = GetComponent<Camera>();
	}

	private void Update()
	{
		if (Physics.Raycast(_cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hitInfo))
		{
			MonoBehaviour.print("I'm looking at " + hitInfo.collider.tag);
			if (hitInfo.collider.CompareTag("CoffeMug"))
			{
				currentMug = hitInfo.collider.GetComponent<CoffeMug>();
				guiText.gameObject.SetActive(value: true);
				if (Input.GetKeyDown(KeyCode.E))
				{
					guiText.text = "Click on the option or release E";
					mouseLook.enabled = false;
					Cursor.lockState = CursorLockMode.None;
					menu.GetButtonWithId("drink").unlocked = currentMug.isFilled;
					menu.GetButtonWithId("refill").unlocked = !currentMug.isFilled;
					menu.Open();
				}
				else if (Input.GetKeyUp(KeyCode.E))
				{
					menu.Close();
					Cursor.lockState = CursorLockMode.Locked;
					mouseLook.enabled = true;
					guiText.gameObject.SetActive(value: false);
					guiText.text = "Hold E to interact";
				}
			}
			else
			{
				menu.Close();
				guiText.gameObject.SetActive(value: false);
				mouseLook.enabled = true;
				guiText.text = "Hold E to interact";
			}
		}
		else
		{
			guiText.text = "Hold E to interact";
			MonoBehaviour.print("I'm looking at nothing!");
			mouseLook.enabled = true;
			menu.Close();
		}
	}

	public void SetFillStateOfCup(bool isFilled)
	{
		currentMug.SetMugFilled(isFilled);
	}
}
