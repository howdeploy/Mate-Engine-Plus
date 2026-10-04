using System;
using System.Collections.Generic;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        static Vector3 SeatSkinContact(AvatarWindowHandler handler, Vector3 original)
        {
            var animator = SeatAnimator.GetValue(handler) as Animator;
            var camera = handler.targetCamera;
            if (animator == null || !animator.isHuman || camera == null ||
                (_recalibrateAt >= 0f && Time.unscaledTime < _recalibrateAt)) return original;
            // The seated pelvis or reclining torso rests on its lower skin
            // surface, not a point inferred from standing head/foot height.
            // Bake only at pose calibration, never every frame. A horizontal
            // leg is not necessarily resting on the ledge: including its
            // lowest vertex lifts the pelvis/torso off the support.
            var contact = new HashSet<Transform>();
            foreach (var bone in new[] { HumanBodyBones.Hips, HumanBodyBones.Spine,
                HumanBodyBones.Chest, HumanBodyBones.UpperChest })
            {
                var transform = animator.GetBoneTransform(bone);
                if (transform != null) contact.Add(transform);
            }
            float lowest = 0f;
            bool found = false;
            var baked = new Mesh();
            try
            {
                foreach (var renderer in handler.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    var source = renderer.sharedMesh;
                    if (!renderer.enabled || source == null) continue;
                    BoneWeight[] weights;
                    Vector3[] vertices;
                    try
                    {
                        weights = source.boneWeights;
                        renderer.BakeMesh(baked, false);
                        vertices = baked.vertices;
                    }
                    catch (Exception error) { Report(error); continue; }
                    if (weights.Length != source.vertexCount) continue;
                    var bones = renderer.bones;
                    if (vertices.Length != weights.Length) continue;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var weight = weights[i];
                        float contactWeight = ContactWeight(bones, contact, weight.boneIndex0, weight.weight0) +
                            ContactWeight(bones, contact, weight.boneIndex1, weight.weight1) +
                            ContactWeight(bones, contact, weight.boneIndex2, weight.weight2) +
                            ContactWeight(bones, contact, weight.boneIndex3, weight.weight3);
                        if (contactWeight < 0.5f) continue;
                        Vector3 point = camera.WorldToScreenPoint(renderer.transform.TransformPoint(vertices[i]));
                        if (point.z > 0f && Finite(point.y) && (!found || point.y < lowest))
                        {
                            lowest = point.y;
                            found = true;
                        }
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(baked); }
            if (!found) return original;
            Vector3 reference = camera.WorldToScreenPoint(original);
            return reference.z <= 0f || !Finite(reference.x) || !Finite(reference.z) ? original :
                camera.ScreenToWorldPoint(new Vector3(reference.x, lowest, reference.z));
        }

        static float ContactWeight(Transform[] bones, HashSet<Transform> contact, int index, float weight)
        {
            return weight > 0f && index >= 0 && index < bones.Length && contact.Contains(bones[index]) ? weight : 0f;
        }

    }
}
