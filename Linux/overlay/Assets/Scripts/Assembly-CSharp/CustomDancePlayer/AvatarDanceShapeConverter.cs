using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace CustomDancePlayer
{
	[DisallowMultipleComponent]
	public class AvatarDanceShapeConverter : MonoBehaviour
	{
		public Mesh dummyBlendshapeMesh;

		public string[] candidatePaths = new string[3] { "Body", "Model/Body", "Face" };

		public bool hideProxyRenderer = true;

		private Animator targetAnimator;

		private AvatarDanceHandler dancePlayer;

		private VRMLoader vrmLoader;

		private GameObject lastModel;

		private UniversalBlendshapes ub;

		private PlayableGraph graph;

		private AnimationClipPlayable clipPlayable;

		private Animator proxyAnimator;

		private SkinnedMeshRenderer[] proxySmrs = Array.Empty<SkinnedMeshRenderer>();

		private AnimationClip boundClip;

		private bool built;

		private bool lastPlaying;

		private bool bypassForThisAvatar;

		private void Awake()
		{
			dancePlayer = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			vrmLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		}

		private void OnEnable()
		{
			TearDownGraph();
			TearDownProxy();
			FindAndBindAnimator(force: true);
			lastPlaying = false;
		}

		private void OnDisable()
		{
			TearDownGraph();
			TearDownProxy();
			targetAnimator = null;
			ub = null;
			lastModel = null;
			lastPlaying = false;
			bypassForThisAvatar = false;
		}

		private void Update()
		{
			if (dancePlayer == null)
			{
				dancePlayer = UnityEngine.Object.FindFirstObjectByType<AvatarDanceHandler>();
			}
			if (vrmLoader == null)
			{
				vrmLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
			}
			FindAndBindAnimator(force: false);
		}

		private void LateUpdate()
		{
			if (targetAnimator == null || dancePlayer == null || bypassForThisAvatar)
			{
				return;
			}
			EnsureProxyIfNeeded();
			if (!built || ub == null)
			{
				return;
			}
			if (!dancePlayer.IsPlaying)
			{
				if (lastPlaying)
				{
					ZeroOut();
					TearDownGraph();
				}
				lastPlaying = false;
				return;
			}
			lastPlaying = true;
			AnimationClip currentClip = dancePlayer.GetCurrentClip();
			if (currentClip != boundClip)
			{
				EnsureGraph(currentClip);
			}
			if (clipPlayable.IsValid())
			{
				double value = dancePlayer.GetPlaybackTime();
				clipPlayable.SetTime(value);
				graph.Evaluate(0f);
				float max = GetMax("まばたき");
				float blink_L = Mathf.Max(GetMax("ウィンク"), GetMax("ウィンク２"));
				float blink_R = Mathf.Max(GetMax("ウィンク右"), GetMax("ｳｨﾝｸ２右"));
				ub.Blink = max;
				ub.Blink_L = blink_L;
				ub.Blink_R = blink_R;
				ub.A = GetMax("あ");
				ub.I = GetMax("い");
				ub.U = GetMax("う");
				ub.E = GetMax("え");
				ub.O = GetMax("お");
				ub.Joy = GetMax("にこり");
				ub.Angry = GetMax("怒り");
				ub.Sorrow = GetMax("困る");
				ub.Neutral = GetMax("真面目");
				ub.Fun = GetMax("笑い");
			}
		}

		public void ForceReset()
		{
			if (!bypassForThisAvatar)
			{
				ZeroOut();
			}
			TearDownGraph();
			lastPlaying = false;
		}

		private void FindAndBindAnimator(bool force)
		{
			GameObject gameObject = null;
			if (vrmLoader != null)
			{
				gameObject = vrmLoader.GetCurrentModel();
				if (gameObject == null || !gameObject.activeInHierarchy)
				{
					gameObject = vrmLoader.mainModel;
				}
			}
			if (gameObject == null)
			{
				Animator animator = UnityEngine.Object.FindFirstObjectByType<Animator>();
				if (animator != null)
				{
					gameObject = animator.transform.root.gameObject;
				}
			}
			if (gameObject == null || (!force && gameObject == lastModel && targetAnimator != null && targetAnimator.isActiveAndEnabled))
			{
				return;
			}
			Animator componentInChildren = gameObject.GetComponentInChildren<Animator>(includeInactive: true);
			if (componentInChildren == null || !componentInChildren.isActiveAndEnabled)
			{
				return;
			}
			targetAnimator = componentInChildren;
			lastModel = gameObject;
			bypassForThisAvatar = HasMmdBlendshapes(targetAnimator);
			if (bypassForThisAvatar)
			{
				TearDownGraph();
				TearDownProxy();
				ub = null;
				return;
			}
			ub = targetAnimator.GetComponent<UniversalBlendshapes>();
			if (ub == null)
			{
				ub = targetAnimator.gameObject.AddComponent<UniversalBlendshapes>();
			}
			ZeroOut();
		}

		private bool HasMmdBlendshapes(Animator a)
		{
			if (a == null)
			{
				return false;
			}
			SkinnedMeshRenderer[] componentsInChildren = a.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			string[] array = new string[15]
			{
				"まばたき", "ウィンク", "ウィンク２", "ウィンク右", "ｳｨﾝｸ２右", "あ", "い", "う", "え", "お",
				"にこり", "怒り", "困る", "真面目", "笑い"
			};
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Mesh sharedMesh = componentsInChildren[i].sharedMesh;
				if (!sharedMesh || sharedMesh.blendShapeCount == 0)
				{
					continue;
				}
				for (int j = 0; j < sharedMesh.blendShapeCount; j++)
				{
					string blendShapeName = sharedMesh.GetBlendShapeName(j);
					for (int k = 0; k < array.Length; k++)
					{
						if (blendShapeName.Contains(array[k]))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private void EnsureProxyIfNeeded()
		{
			if (built || dummyBlendshapeMesh == null)
			{
				return;
			}
			GameObject gameObject = new GameObject("ADS_ProxyRoot");
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			proxyAnimator = gameObject.AddComponent<Animator>();
			List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
			for (int i = 0; i < candidatePaths.Length; i++)
			{
				Transform transform = gameObject.transform;
				string[] array = candidatePaths[i].Split('/');
				for (int j = 0; j < array.Length; j++)
				{
					Transform transform2 = transform.Find(array[j]);
					if (!transform2)
					{
						GameObject obj = new GameObject(array[j]);
						obj.transform.SetParent(transform, worldPositionStays: false);
						transform2 = obj.transform;
					}
					transform = transform2;
				}
				SkinnedMeshRenderer skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();
				if (!skinnedMeshRenderer)
				{
					skinnedMeshRenderer = transform.gameObject.AddComponent<SkinnedMeshRenderer>();
				}
				skinnedMeshRenderer.sharedMesh = dummyBlendshapeMesh;
				skinnedMeshRenderer.updateWhenOffscreen = true;
				if (hideProxyRenderer)
				{
					skinnedMeshRenderer.enabled = false;
					skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
					skinnedMeshRenderer.receiveShadows = false;
				}
				list.Add(skinnedMeshRenderer);
			}
			proxySmrs = list.ToArray();
			built = true;
		}

		private void EnsureGraph(AnimationClip clip)
		{
			TearDownGraph();
			if ((bool)proxyAnimator)
			{
				if (clip == null)
				{
					boundClip = null;
					return;
				}
				graph = PlayableGraph.Create("ADS_Graph");
				graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
				clipPlayable = AnimationClipPlayable.Create(graph, clip);
				clipPlayable.SetApplyFootIK(value: false);
				clipPlayable.SetSpeed(0.0);
				AnimationPlayableOutput.Create(graph, "ADS_Output", proxyAnimator).SetSourcePlayable(clipPlayable);
				boundClip = clip;
				graph.Play();
			}
		}

		private void TearDownGraph()
		{
			if (graph.IsValid())
			{
				graph.Stop();
				graph.Destroy();
			}
			clipPlayable = default(AnimationClipPlayable);
			boundClip = null;
		}

		private void TearDownProxy()
		{
			if ((bool)proxyAnimator)
			{
				UnityEngine.Object.Destroy(proxyAnimator.gameObject);
			}
			proxyAnimator = null;
			proxySmrs = Array.Empty<SkinnedMeshRenderer>();
			built = false;
		}

		private float GetMax(string token)
		{
			float num = 0f;
			for (int i = 0; i < proxySmrs.Length; i++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = proxySmrs[i];
				Mesh mesh = (skinnedMeshRenderer ? skinnedMeshRenderer.sharedMesh : null);
				if (!mesh)
				{
					continue;
				}
				int blendShapeCount = mesh.blendShapeCount;
				for (int j = 0; j < blendShapeCount; j++)
				{
					if (mesh.GetBlendShapeName(j).Contains(token))
					{
						float num2 = Mathf.Clamp01(skinnedMeshRenderer.GetBlendShapeWeight(j) / 100f);
						if (num2 > num)
						{
							num = num2;
						}
					}
				}
			}
			return num;
		}

		private void ZeroOut()
		{
			if ((bool)ub)
			{
				ub.Blink = 0f;
				ub.Blink_L = 0f;
				ub.Blink_R = 0f;
				ub.A = 0f;
				ub.I = 0f;
				ub.U = 0f;
				ub.E = 0f;
				ub.O = 0f;
				ub.Joy = 0f;
				ub.Angry = 0f;
				ub.Sorrow = 0f;
				ub.Neutral = 0f;
				ub.Fun = 0f;
			}
		}
	}
}
