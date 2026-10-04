using System;
using UnityEngine;

public class MEDanceModCreator : MonoBehaviour
{
	[Serializable]
	public class DanceMeta
	{
		public string songName;

		public string songAuthor;

		public string mmdAuthor;

		public float songLength;

		public string placeholderClipName;
	}

	public RuntimeAnimatorController controller;

	public AnimationClip danceClip;

	public AudioClip song;

	public string songName;

	public string songAuthor;

	public string mmdAuthor;

	public string placeholderClipName = "CUSTOM_DANCE";

	public AnimationClip GetDanceClip()
	{
		if (danceClip != null)
		{
			return danceClip;
		}
		if (controller == null)
		{
			return null;
		}
		AnimationClip[] animationClips = new AnimatorOverrideController(controller).animationClips;
		for (int i = 0; i < animationClips.Length; i++)
		{
			if (animationClips[i] != null && animationClips[i].name == placeholderClipName)
			{
				return animationClips[i];
			}
		}
		if (animationClips.Length == 0)
		{
			return null;
		}
		return animationClips[0];
	}

	public AudioClip GetAudioClip()
	{
		return song;
	}

	public AnimatorOverrideController BuildOverrideForExport()
	{
		if (controller == null)
		{
			return null;
		}
		AnimatorOverrideController animatorOverrideController = controller as AnimatorOverrideController;
		if (animatorOverrideController == null)
		{
			animatorOverrideController = new AnimatorOverrideController(controller);
		}
		AnimationClip animationClip = GetDanceClip();
		if (animationClip != null)
		{
			bool flag = false;
			AnimationClip[] animationClips = animatorOverrideController.animationClips;
			for (int i = 0; i < animationClips.Length; i++)
			{
				if (animationClips[i] != null && animationClips[i].name == placeholderClipName)
				{
					animatorOverrideController[placeholderClipName] = animationClip;
					flag = true;
					break;
				}
			}
			if (!flag && animationClips.Length != 0)
			{
				animatorOverrideController[animationClips[0].name] = animationClip;
			}
		}
		return animatorOverrideController;
	}

	public DanceMeta GetMetadata()
	{
		return new DanceMeta
		{
			songName = ((string.IsNullOrWhiteSpace(songName) && song != null) ? song.name : songName),
			songAuthor = songAuthor,
			mmdAuthor = mmdAuthor,
			songLength = ((song != null) ? song.length : 0f),
			placeholderClipName = placeholderClipName
		};
	}

	private void OnValidate()
	{
		if (string.IsNullOrWhiteSpace(songName) && song != null)
		{
			songName = song.name;
		}
		if (!(danceClip == null) || !(controller is AnimatorOverrideController animatorOverrideController))
		{
			return;
		}
		try
		{
			AnimationClip[] animationClips = animatorOverrideController.animationClips;
			for (int i = 0; i < animationClips.Length; i++)
			{
				if (animationClips[i] != null && animationClips[i].name == placeholderClipName)
				{
					AnimationClip animationClip = animatorOverrideController[placeholderClipName];
					if (animationClip != null)
					{
						danceClip = animationClip;
					}
					break;
				}
			}
		}
		catch
		{
		}
	}
}
