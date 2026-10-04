using System;
using System.Collections;
using System.Collections.Generic;
using LLMUnitySamples;
using UnityEngine;

[DisallowMultipleComponent]
public class MEVoicePack : MonoBehaviour
{
	public enum MappingMode
	{
		ReplaceAll = 0,
		MatchExistingCount_Cycle = 1
	}

	[Serializable]
	public class PetRegionOverride
	{
		public string regionName;

		public int fallbackRegionIndex = -1;

		public bool overrideVoiceClips = true;

		public MappingMode voiceMapping = MappingMode.MatchExistingCount_Cycle;

		public List<AudioClip> voiceClips = new List<AudioClip>();

		public bool overrideLayeredClips = true;

		public MappingMode layeredMapping = MappingMode.MatchExistingCount_Cycle;

		public List<AudioClip> layeredClips = new List<AudioClip>();
	}

	private class PetRegionSnapshot
	{
		public PetVoiceReactionHandler handler;

		public int regionIndex;

		public List<AudioClip> origVoice;

		public List<AudioClip> origLayered;
	}

	[Header("Drag Sounds")]
	public AudioClip dragStartClip;

	public AudioClip dragStopClip;

	[Header("Reaction Sounds")]
	public List<PetRegionOverride> petRegionOverrides = new List<PetRegionOverride>();

	[Header("Macaroon Sounds")]
	public AudioClip bubbleEnableClip;

	public AudioClip bubbleDisableClip;

	[Header("Event Message Sounds")]
	public AudioClip randomStreamClip;

	[Header("Alarm Sounds")]
	public List<AudioClip> bigScreenAlarmClips = new List<AudioClip>();

	public AudioClip bigScreenStreamClip;

	[Header("Chatbot Sounds")]
	public AudioClip chatBotStreamClip;

	[Header("Minecraft Sounds")]
	public AudioClip minecraftStreamClip;

	[Header("Ui Sounds")]
	public List<AudioClip> menuStartupClips = new List<AudioClip>();

	public List<AudioClip> menuOpenClips = new List<AudioClip>();

	public List<AudioClip> menuCloseClips = new List<AudioClip>();

	public List<AudioClip> menuButtonClips = new List<AudioClip>();

	public List<AudioClip> menuToggleClips = new List<AudioClip>();

	public List<AudioClip> menuSliderClips = new List<AudioClip>();

	public List<AudioClip> menuDropdownClips = new List<AudioClip>();

	[Header("Options")]
	public bool applyOnEnable = true;

	public bool revertOnDisable;

	public bool autoWatchForNewHandlers = true;

	public bool autoBindAnimator = true;

	public bool fixEmptyStateWhitelist = true;

	[Range(0.1f, 5f)]
	public float watchInterval = 0.5f;

	private readonly List<(AudioSource src, AudioClip original)> _dragOriginals = new List<(AudioSource, AudioClip)>();

	private readonly List<PetRegionSnapshot> _petOriginals = new List<PetRegionSnapshot>();

	private readonly List<(AvatarBubbleHandler h, AudioClip enable, AudioClip disable)> _bubbleOriginals = new List<(AvatarBubbleHandler, AudioClip, AudioClip)>();

	private readonly List<(AudioSource src, AudioClip clip)> _streamOriginals = new List<(AudioSource, AudioClip)>();

	private readonly List<(AvatarBigScreenTimer t, List<AudioClip> alarm)> _bigAlarmOriginals = new List<(AvatarBigScreenTimer, List<AudioClip>)>();

	private readonly List<(MenuAudioHandler h, List<AudioClip> startup, List<AudioClip> open, List<AudioClip> close, List<AudioClip> button, List<AudioClip> toggle, List<AudioClip> slider, List<AudioClip> dropdown)> _menuOriginals = new List<(MenuAudioHandler, List<AudioClip>, List<AudioClip>, List<AudioClip>, List<AudioClip>, List<AudioClip>, List<AudioClip>, List<AudioClip>)>();

	private readonly HashSet<int> _processedDrag = new HashSet<int>();

	private readonly HashSet<int> _processedPet = new HashSet<int>();

	private readonly HashSet<int> _processedBubble = new HashSet<int>();

	private readonly HashSet<int> _processedRandMsg = new HashSet<int>();

	private readonly HashSet<int> _processedBigTimer = new HashSet<int>();

	private readonly HashSet<int> _processedChatBot = new HashSet<int>();

	private readonly HashSet<int> _processedMenu = new HashSet<int>();

	private readonly HashSet<int> _processedMinecraft = new HashSet<int>();

	private bool _applied;

	private Coroutine _watcher;

	private void OnEnable()
	{
		if (applyOnEnable)
		{
			Apply();
		}
		if (autoWatchForNewHandlers)
		{
			_watcher = StartCoroutine(WatchForNewHandlers());
		}
	}

	private void OnDisable()
	{
		if (_watcher != null)
		{
			StopCoroutine(_watcher);
			_watcher = null;
		}
		_processedDrag.Clear();
		_processedPet.Clear();
		_processedBubble.Clear();
		_processedRandMsg.Clear();
		_processedBigTimer.Clear();
		_processedChatBot.Clear();
		_processedMenu.Clear();
		_processedMinecraft.Clear();
		if (revertOnDisable)
		{
			Revert();
		}
	}

	[ContextMenu("Apply Voice Pack")]
	public void Apply()
	{
		AvatarDragSoundHandler[] handlers = UnityEngine.Object.FindObjectsByType<AvatarDragSoundHandler>(FindObjectsSortMode.None);
		PetVoiceReactionHandler[] petHandlers = UnityEngine.Object.FindObjectsByType<PetVoiceReactionHandler>(FindObjectsSortMode.None);
		AvatarBubbleHandler[] handlers2 = UnityEngine.Object.FindObjectsByType<AvatarBubbleHandler>(FindObjectsSortMode.None);
		AvatarRandomMessages[] handlers3 = UnityEngine.Object.FindObjectsByType<AvatarRandomMessages>(FindObjectsSortMode.None);
		AvatarBigScreenTimer[] timers = UnityEngine.Object.FindObjectsByType<AvatarBigScreenTimer>(FindObjectsSortMode.None);
		ChatBot[] bots = UnityEngine.Object.FindObjectsByType<ChatBot>(FindObjectsSortMode.None);
		MenuAudioHandler[] menus = UnityEngine.Object.FindObjectsByType<MenuAudioHandler>(FindObjectsSortMode.None);
		AvatarMinecraftMessages[] handlers4 = UnityEngine.Object.FindObjectsByType<AvatarMinecraftMessages>(FindObjectsSortMode.None);
		ApplyDragOverridesTo(handlers);
		ApplyPetOverridesTo(petHandlers);
		ApplyBubbleOverridesTo(handlers2);
		ApplyRandomMessagesOverridesTo(handlers3);
		ApplyBigScreenTimerOverridesTo(timers);
		ApplyChatBotOverridesTo(bots);
		ApplyMenuAudioOverridesTo(menus);
		ApplyMinecraftOverridesTo(handlers4);
		_applied = true;
	}

	[ContextMenu("Revert Voice Pack")]
	public void Revert()
	{
		foreach (var (audioSource, clip) in _dragOriginals)
		{
			if ((bool)audioSource)
			{
				audioSource.clip = clip;
			}
		}
		_dragOriginals.Clear();
		foreach (PetRegionSnapshot petOriginal in _petOriginals)
		{
			if ((bool)petOriginal.handler)
			{
				List<PetVoiceReactionHandler.VoiceRegion> regions = petOriginal.handler.regions;
				if (regions != null && petOriginal.regionIndex >= 0 && petOriginal.regionIndex < regions.Count)
				{
					PetVoiceReactionHandler.VoiceRegion voiceRegion = regions[petOriginal.regionIndex];
					voiceRegion.voiceClips = ((petOriginal.origVoice != null) ? new List<AudioClip>(petOriginal.origVoice) : new List<AudioClip>());
					voiceRegion.layeredVoiceClips = ((petOriginal.origLayered != null) ? new List<AudioClip>(petOriginal.origLayered) : new List<AudioClip>());
				}
			}
		}
		_petOriginals.Clear();
		foreach (var bubbleOriginal in _bubbleOriginals)
		{
			if ((bool)bubbleOriginal.h)
			{
				bubbleOriginal.h.enableSound = bubbleOriginal.enable;
				bubbleOriginal.h.disableSound = bubbleOriginal.disable;
			}
		}
		_bubbleOriginals.Clear();
		foreach (var (audioSource2, clip2) in _streamOriginals)
		{
			if ((bool)audioSource2)
			{
				audioSource2.clip = clip2;
			}
		}
		_streamOriginals.Clear();
		foreach (var (avatarBigScreenTimer, list) in _bigAlarmOriginals)
		{
			if ((bool)avatarBigScreenTimer)
			{
				avatarBigScreenTimer.alarmClips = ((list != null) ? new List<AudioClip>(list) : new List<AudioClip>());
			}
		}
		_bigAlarmOriginals.Clear();
		foreach (var menuOriginal in _menuOriginals)
		{
			if ((bool)menuOriginal.h)
			{
				menuOriginal.h.startupSounds = ((menuOriginal.startup != null) ? new List<AudioClip>(menuOriginal.startup) : new List<AudioClip>());
				menuOriginal.h.openMenuSounds = ((menuOriginal.open != null) ? new List<AudioClip>(menuOriginal.open) : new List<AudioClip>());
				menuOriginal.h.closeMenuSounds = ((menuOriginal.close != null) ? new List<AudioClip>(menuOriginal.close) : new List<AudioClip>());
				menuOriginal.h.buttonSounds = ((menuOriginal.button != null) ? new List<AudioClip>(menuOriginal.button) : new List<AudioClip>());
				menuOriginal.h.toggleSounds = ((menuOriginal.toggle != null) ? new List<AudioClip>(menuOriginal.toggle) : new List<AudioClip>());
				menuOriginal.h.sliderSounds = ((menuOriginal.slider != null) ? new List<AudioClip>(menuOriginal.slider) : new List<AudioClip>());
				menuOriginal.h.dropdownSounds = ((menuOriginal.Rest.Item1 != null) ? new List<AudioClip>(menuOriginal.Rest.Item1) : new List<AudioClip>());
			}
		}
		_menuOriginals.Clear();
		_applied = false;
	}

	private IEnumerator WatchForNewHandlers()
	{
		WaitForSeconds wait = new WaitForSeconds(watchInterval);
		while (base.enabled)
		{
			AvatarDragSoundHandler[] array = UnityEngine.Object.FindObjectsByType<AvatarDragSoundHandler>(FindObjectsSortMode.None);
			PetVoiceReactionHandler[] array2 = UnityEngine.Object.FindObjectsByType<PetVoiceReactionHandler>(FindObjectsSortMode.None);
			AvatarBubbleHandler[] array3 = UnityEngine.Object.FindObjectsByType<AvatarBubbleHandler>(FindObjectsSortMode.None);
			AvatarRandomMessages[] array4 = UnityEngine.Object.FindObjectsByType<AvatarRandomMessages>(FindObjectsSortMode.None);
			AvatarBigScreenTimer[] array5 = UnityEngine.Object.FindObjectsByType<AvatarBigScreenTimer>(FindObjectsSortMode.None);
			ChatBot[] array6 = UnityEngine.Object.FindObjectsByType<ChatBot>(FindObjectsSortMode.None);
			MenuAudioHandler[] array7 = UnityEngine.Object.FindObjectsByType<MenuAudioHandler>(FindObjectsSortMode.None);
			AvatarMinecraftMessages[] array8 = UnityEngine.Object.FindObjectsByType<AvatarMinecraftMessages>(FindObjectsSortMode.None);
			AvatarDragSoundHandler[] array9 = array;
			foreach (AvatarDragSoundHandler avatarDragSoundHandler in array9)
			{
				int instanceID = avatarDragSoundHandler.GetInstanceID();
				if (!_processedDrag.Contains(instanceID))
				{
					ApplyDragOverridesTo(new AvatarDragSoundHandler[1] { avatarDragSoundHandler });
					_processedDrag.Add(instanceID);
				}
			}
			PetVoiceReactionHandler[] array10 = array2;
			foreach (PetVoiceReactionHandler petVoiceReactionHandler in array10)
			{
				int instanceID2 = petVoiceReactionHandler.GetInstanceID();
				if (_processedPet.Contains(instanceID2))
				{
					continue;
				}
				if (autoBindAnimator && petVoiceReactionHandler.avatarAnimator == null)
				{
					Animator componentInParent = petVoiceReactionHandler.GetComponentInParent<Animator>();
					if ((bool)componentInParent)
					{
						petVoiceReactionHandler.SetAnimator(componentInParent);
					}
				}
				if (fixEmptyStateWhitelist)
				{
					EnsureStateWhitelistNotEmpty(petVoiceReactionHandler);
				}
				ApplyPetOverridesTo(new PetVoiceReactionHandler[1] { petVoiceReactionHandler });
				_processedPet.Add(instanceID2);
			}
			AvatarBubbleHandler[] array11 = array3;
			foreach (AvatarBubbleHandler avatarBubbleHandler in array11)
			{
				int instanceID3 = avatarBubbleHandler.GetInstanceID();
				if (!_processedBubble.Contains(instanceID3))
				{
					ApplyBubbleOverridesTo(new AvatarBubbleHandler[1] { avatarBubbleHandler });
					_processedBubble.Add(instanceID3);
				}
			}
			AvatarRandomMessages[] array12 = array4;
			foreach (AvatarRandomMessages avatarRandomMessages in array12)
			{
				int instanceID4 = avatarRandomMessages.GetInstanceID();
				if (!_processedRandMsg.Contains(instanceID4))
				{
					ApplyRandomMessagesOverridesTo(new AvatarRandomMessages[1] { avatarRandomMessages });
					_processedRandMsg.Add(instanceID4);
				}
			}
			AvatarBigScreenTimer[] array13 = array5;
			foreach (AvatarBigScreenTimer avatarBigScreenTimer in array13)
			{
				int instanceID5 = avatarBigScreenTimer.GetInstanceID();
				if (!_processedBigTimer.Contains(instanceID5))
				{
					ApplyBigScreenTimerOverridesTo(new AvatarBigScreenTimer[1] { avatarBigScreenTimer });
					_processedBigTimer.Add(instanceID5);
				}
			}
			ChatBot[] array14 = array6;
			foreach (ChatBot chatBot in array14)
			{
				int instanceID6 = chatBot.GetInstanceID();
				if (!_processedChatBot.Contains(instanceID6))
				{
					ApplyChatBotOverridesTo(new ChatBot[1] { chatBot });
					_processedChatBot.Add(instanceID6);
				}
			}
			MenuAudioHandler[] array15 = array7;
			foreach (MenuAudioHandler menuAudioHandler in array15)
			{
				int instanceID7 = menuAudioHandler.GetInstanceID();
				if (!_processedMenu.Contains(instanceID7))
				{
					ApplyMenuAudioOverridesTo(new MenuAudioHandler[1] { menuAudioHandler });
					_processedMenu.Add(instanceID7);
				}
			}
			AvatarMinecraftMessages[] array16 = array8;
			foreach (AvatarMinecraftMessages avatarMinecraftMessages in array16)
			{
				int instanceID8 = avatarMinecraftMessages.GetInstanceID();
				if (!_processedMinecraft.Contains(instanceID8))
				{
					ApplyMinecraftOverridesTo(new AvatarMinecraftMessages[1] { avatarMinecraftMessages });
					_processedMinecraft.Add(instanceID8);
				}
			}
			yield return wait;
		}
	}

	private void ApplyDragOverridesTo(AvatarDragSoundHandler[] handlers)
	{
		if (handlers == null || handlers.Length == 0)
		{
			return;
		}
		if (!_applied)
		{
			_dragOriginals.Clear();
		}
		foreach (AvatarDragSoundHandler avatarDragSoundHandler in handlers)
		{
			if ((bool)dragStartClip && (bool)avatarDragSoundHandler.dragStartSound)
			{
				if (!_applied)
				{
					_dragOriginals.Add((avatarDragSoundHandler.dragStartSound, avatarDragSoundHandler.dragStartSound.clip));
				}
				avatarDragSoundHandler.dragStartSound.clip = dragStartClip;
				avatarDragSoundHandler.dragStartSound.playOnAwake = false;
			}
			if ((bool)dragStopClip && (bool)avatarDragSoundHandler.dragStopSound)
			{
				if (!_applied)
				{
					_dragOriginals.Add((avatarDragSoundHandler.dragStopSound, avatarDragSoundHandler.dragStopSound.clip));
				}
				avatarDragSoundHandler.dragStopSound.clip = dragStopClip;
				avatarDragSoundHandler.dragStopSound.playOnAwake = false;
			}
		}
	}

	private void ApplyPetOverridesTo(PetVoiceReactionHandler[] petHandlers)
	{
		if (petHandlers == null || petHandlers.Length == 0)
		{
			return;
		}
		if (!_applied)
		{
			_petOriginals.Clear();
		}
		foreach (PetVoiceReactionHandler petVoiceReactionHandler in petHandlers)
		{
			if (autoBindAnimator && petVoiceReactionHandler.avatarAnimator == null)
			{
				Animator componentInParent = petVoiceReactionHandler.GetComponentInParent<Animator>();
				if ((bool)componentInParent)
				{
					petVoiceReactionHandler.SetAnimator(componentInParent);
				}
			}
			List<PetVoiceReactionHandler.VoiceRegion> regions = petVoiceReactionHandler.regions;
			if (regions == null || regions.Count == 0)
			{
				continue;
			}
			foreach (PetRegionOverride petRegionOverride in petRegionOverrides)
			{
				int num = ResolveRegionIndex(regions, petRegionOverride);
				if (num >= 0 && num < regions.Count)
				{
					PetVoiceReactionHandler.VoiceRegion voiceRegion = regions[num];
					if (!_applied)
					{
						_petOriginals.Add(new PetRegionSnapshot
						{
							handler = petVoiceReactionHandler,
							regionIndex = num,
							origVoice = ((voiceRegion.voiceClips != null) ? new List<AudioClip>(voiceRegion.voiceClips) : new List<AudioClip>()),
							origLayered = ((voiceRegion.layeredVoiceClips != null) ? new List<AudioClip>(voiceRegion.layeredVoiceClips) : new List<AudioClip>())
						});
					}
					if (petRegionOverride.overrideVoiceClips && petRegionOverride.voiceClips != null && petRegionOverride.voiceClips.Count > 0)
					{
						voiceRegion.voiceClips = BuildMappedList(petRegionOverride.voiceClips, (voiceRegion.voiceClips != null) ? voiceRegion.voiceClips.Count : petRegionOverride.voiceClips.Count, petRegionOverride.voiceMapping);
					}
					if (petRegionOverride.overrideLayeredClips && petRegionOverride.layeredClips != null && petRegionOverride.layeredClips.Count > 0)
					{
						voiceRegion.layeredVoiceClips = BuildMappedList(petRegionOverride.layeredClips, (voiceRegion.layeredVoiceClips != null) ? voiceRegion.layeredVoiceClips.Count : petRegionOverride.layeredClips.Count, petRegionOverride.layeredMapping);
					}
				}
			}
		}
	}

	private void ApplyBubbleOverridesTo(AvatarBubbleHandler[] handlers)
	{
		if (handlers == null || handlers.Length == 0)
		{
			return;
		}
		if (!_applied)
		{
			_bubbleOriginals.Clear();
		}
		foreach (AvatarBubbleHandler avatarBubbleHandler in handlers)
		{
			if (!_applied)
			{
				_bubbleOriginals.Add((avatarBubbleHandler, avatarBubbleHandler.enableSound, avatarBubbleHandler.disableSound));
			}
			if ((bool)bubbleEnableClip)
			{
				avatarBubbleHandler.enableSound = bubbleEnableClip;
			}
			if ((bool)bubbleDisableClip)
			{
				avatarBubbleHandler.disableSound = bubbleDisableClip;
			}
		}
	}

	private void ApplyRandomMessagesOverridesTo(AvatarRandomMessages[] handlers)
	{
		if (handlers == null || handlers.Length == 0)
		{
			return;
		}
		foreach (AvatarRandomMessages avatarRandomMessages in handlers)
		{
			if ((bool)avatarRandomMessages.streamAudioSource && (bool)randomStreamClip)
			{
				if (!_applied)
				{
					_streamOriginals.Add((avatarRandomMessages.streamAudioSource, avatarRandomMessages.streamAudioSource.clip));
				}
				avatarRandomMessages.streamAudioSource.clip = randomStreamClip;
				avatarRandomMessages.streamAudioSource.playOnAwake = false;
			}
		}
	}

	private void ApplyBigScreenTimerOverridesTo(AvatarBigScreenTimer[] timers)
	{
		if (timers == null || timers.Length == 0)
		{
			return;
		}
		foreach (AvatarBigScreenTimer avatarBigScreenTimer in timers)
		{
			if (!_applied)
			{
				_bigAlarmOriginals.Add((avatarBigScreenTimer, (avatarBigScreenTimer.alarmClips != null) ? new List<AudioClip>(avatarBigScreenTimer.alarmClips) : new List<AudioClip>()));
			}
			if (bigScreenAlarmClips != null && bigScreenAlarmClips.Count > 0)
			{
				avatarBigScreenTimer.alarmClips = new List<AudioClip>(bigScreenAlarmClips);
			}
			if ((bool)avatarBigScreenTimer.streamAudioSource && (bool)bigScreenStreamClip)
			{
				if (!_applied)
				{
					_streamOriginals.Add((avatarBigScreenTimer.streamAudioSource, avatarBigScreenTimer.streamAudioSource.clip));
				}
				avatarBigScreenTimer.streamAudioSource.clip = bigScreenStreamClip;
				avatarBigScreenTimer.streamAudioSource.playOnAwake = false;
			}
		}
	}

	private void ApplyChatBotOverridesTo(ChatBot[] bots)
	{
		if (bots == null || bots.Length == 0)
		{
			return;
		}
		foreach (ChatBot chatBot in bots)
		{
			if (!chatBot)
			{
				continue;
			}
			AudioSource streamAudioSource = chatBot.streamAudioSource;
			if ((bool)streamAudioSource && (bool)chatBotStreamClip)
			{
				if (!_applied)
				{
					_streamOriginals.Add((streamAudioSource, streamAudioSource.clip));
				}
				streamAudioSource.clip = chatBotStreamClip;
				streamAudioSource.playOnAwake = false;
			}
		}
	}

	private void ApplyMinecraftOverridesTo(AvatarMinecraftMessages[] handlers)
	{
		if (handlers == null || handlers.Length == 0)
		{
			return;
		}
		foreach (AvatarMinecraftMessages avatarMinecraftMessages in handlers)
		{
			if (!avatarMinecraftMessages)
			{
				continue;
			}
			AudioSource streamAudioSource = avatarMinecraftMessages.streamAudioSource;
			if ((bool)streamAudioSource && (bool)minecraftStreamClip)
			{
				if (!_applied)
				{
					_streamOriginals.Add((streamAudioSource, streamAudioSource.clip));
				}
				streamAudioSource.clip = minecraftStreamClip;
				streamAudioSource.playOnAwake = false;
			}
		}
	}

	private void ApplyMenuAudioOverridesTo(MenuAudioHandler[] menus)
	{
		if (menus == null || menus.Length == 0)
		{
			return;
		}
		if (!_applied)
		{
			_menuOriginals.Clear();
		}
		foreach (MenuAudioHandler menuAudioHandler in menus)
		{
			if ((bool)menuAudioHandler)
			{
				if (!_applied)
				{
					_menuOriginals.Add((menuAudioHandler, (menuAudioHandler.startupSounds != null) ? new List<AudioClip>(menuAudioHandler.startupSounds) : new List<AudioClip>(), (menuAudioHandler.openMenuSounds != null) ? new List<AudioClip>(menuAudioHandler.openMenuSounds) : new List<AudioClip>(), (menuAudioHandler.closeMenuSounds != null) ? new List<AudioClip>(menuAudioHandler.closeMenuSounds) : new List<AudioClip>(), (menuAudioHandler.buttonSounds != null) ? new List<AudioClip>(menuAudioHandler.buttonSounds) : new List<AudioClip>(), (menuAudioHandler.toggleSounds != null) ? new List<AudioClip>(menuAudioHandler.toggleSounds) : new List<AudioClip>(), (menuAudioHandler.sliderSounds != null) ? new List<AudioClip>(menuAudioHandler.sliderSounds) : new List<AudioClip>(), (menuAudioHandler.dropdownSounds != null) ? new List<AudioClip>(menuAudioHandler.dropdownSounds) : new List<AudioClip>()));
				}
				if (menuStartupClips != null && menuStartupClips.Count > 0)
				{
					menuAudioHandler.startupSounds = new List<AudioClip>(menuStartupClips);
				}
				if (menuOpenClips != null && menuOpenClips.Count > 0)
				{
					menuAudioHandler.openMenuSounds = new List<AudioClip>(menuOpenClips);
				}
				if (menuCloseClips != null && menuCloseClips.Count > 0)
				{
					menuAudioHandler.closeMenuSounds = new List<AudioClip>(menuCloseClips);
				}
				if (menuButtonClips != null && menuButtonClips.Count > 0)
				{
					menuAudioHandler.buttonSounds = new List<AudioClip>(menuButtonClips);
				}
				if (menuToggleClips != null && menuToggleClips.Count > 0)
				{
					menuAudioHandler.toggleSounds = new List<AudioClip>(menuToggleClips);
				}
				if (menuSliderClips != null && menuSliderClips.Count > 0)
				{
					menuAudioHandler.sliderSounds = new List<AudioClip>(menuSliderClips);
				}
				if (menuDropdownClips != null && menuDropdownClips.Count > 0)
				{
					menuAudioHandler.dropdownSounds = new List<AudioClip>(menuDropdownClips);
				}
			}
		}
	}

	private static int ResolveRegionIndex(List<PetVoiceReactionHandler.VoiceRegion> regions, PetRegionOverride ov)
	{
		if (!string.IsNullOrEmpty(ov.regionName))
		{
			for (int i = 0; i < regions.Count; i++)
			{
				if (string.Equals(regions[i].name, ov.regionName, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
		}
		return ov.fallbackRegionIndex;
	}

	private static List<AudioClip> BuildMappedList(List<AudioClip> source, int existingCount, MappingMode mode)
	{
		if (mode == MappingMode.ReplaceAll)
		{
			return new List<AudioClip>(source);
		}
		int num = Mathf.Max(existingCount, source.Count);
		List<AudioClip> list = new List<AudioClip>(num);
		for (int i = 0; i < num; i++)
		{
			list.Add(source[i % source.Count]);
		}
		return list;
	}

	private void EnsureStateWhitelistNotEmpty(PetVoiceReactionHandler p)
	{
		if (p == null)
		{
			return;
		}
		List<string> stateWhitelist = p.stateWhitelist;
		if (stateWhitelist != null && stateWhitelist.Count > 0)
		{
			return;
		}
		Animator animator = (p.avatarAnimator ? p.avatarAnimator : p.GetComponentInParent<Animator>());
		if (!animator)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>();
		AnimatorClipInfo[] currentAnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
		for (int i = 0; i < currentAnimatorClipInfo.Length; i++)
		{
			if ((bool)currentAnimatorClipInfo[i].clip)
			{
				hashSet.Add(currentAnimatorClipInfo[i].clip.name);
			}
		}
		hashSet.Add("Idle");
		hashSet.Add("Base Layer.Idle");
		hashSet.Add("Locomotion");
		hashSet.Add("Base Layer.Locomotion");
		p.stateWhitelist = new List<string>(hashSet);
	}
}
