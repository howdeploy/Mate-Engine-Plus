using UnityEngine;

public class AvatarAnimatorReceiver : MonoBehaviour
{
	[Header("Animator to be used by menus, etc.")]
	public Animator avatarAnimator;

	public string avatarName = "Default";

	private void Awake()
	{
		if (avatarAnimator == null)
		{
			avatarAnimator = GetComponent<Animator>();
		}
		LogWithColor("[Receiver] Awake! Avatar: " + avatarName, "#bada55");
	}

	public void SetAnimator(Animator newAnimator)
	{
		avatarAnimator = newAnimator;
		LogWithColor("[Receiver] SetAnimator called! Neuer Animator gesetzt für: " + avatarName + " (" + base.gameObject.name + ")", "#93FF4F");
	}

	private void OnEnable()
	{
		LogWithColor("[Receiver] Aktiviert: " + avatarName + " (" + base.gameObject.name + ")", "#4FFFD7");
	}

	private void OnDisable()
	{
		LogWithColor("[Receiver] Deaktiviert: " + avatarName + " (" + base.gameObject.name + ")", "#FFBC4F");
	}

	private void OnDestroy()
	{
		LogWithColor("[Receiver] Destroyed: " + avatarName + " (" + base.gameObject.name + ")", "#FF4848");
	}

	private void LogWithColor(string msg, string hexColor)
	{
		Debug.Log("<color=" + hexColor + ">" + msg + "</color>", this);
	}
}
