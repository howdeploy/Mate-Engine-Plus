using UnityEngine;

public class AvatarDragSoundHandler : MonoBehaviour
{
	[Header("Sound Settings")]
	public AudioSource dragStartSound;

	[Header("Sound Settings")]
	public AudioSource dragStopSound;

	[Range(0f, 100f)]
	public float maxHighPitchPercent = 10f;

	[Range(0f, 100f)]
	public float maxLowPitchPercent = 10f;

	private bool wasDragging;

	private AvatarAnimatorController avatarController;

	private void Start()
	{
		avatarController = GetComponent<AvatarAnimatorController>();
		if (!avatarController)
		{
			Debug.LogError("AvatarAnimatorController script not found on this GameObject.");
		}
	}

	private void Update()
	{
		if (!avatarController)
		{
			return;
		}
		bool isDragging = avatarController.isDragging;
		if (isDragging != wasDragging)
		{
			if (isDragging)
			{
				PlaySound(dragStartSound);
			}
			else
			{
				PlaySound(dragStopSound);
			}
			wasDragging = isDragging;
		}
	}

	private void PlaySound(AudioSource audio)
	{
		if ((bool)audio)
		{
			float minInclusive = 1f - maxLowPitchPercent / 100f;
			float maxInclusive = 1f + maxHighPitchPercent / 100f;
			audio.pitch = Random.Range(minInclusive, maxInclusive);
			audio.Play();
		}
	}
}
