using System;
using System.Collections.Generic;
using UnityEngine;

namespace MateEngine.Shoji
{
    public static partial class ShojiBridge
    {
        static Transform[] _contactSkinBones;
        static Vector3[] _contactSkinLocal;
        static float[] _contactSkinWeights;
        static readonly bool SeatAuditEnabled = Environment.GetEnvironmentVariable("MATEENGINE_SEAT_AUDIT") == "1";
        static SkinnedMeshRenderer _seatAuditRenderer;
        static int _seatAuditVertex;
        static BoneWeight[] _seatAuditWeights;
        static HashSet<Transform> _seatAuditBones;
        static float _nextSeatAudit;
        static bool _seatReclining;

        static Vector3 SeatSkinContact(AvatarWindowHandler handler, Vector3 original)
        {
            _hasSeatFront = false;
            _contactSkinBones = null;
            _seatAuditRenderer = null;
            _seatReclining = false;
            var animator = SeatAnimator.GetValue(handler) as Animator;
            var camera = handler.targetCamera;
            if (animator == null || !animator.isHuman || camera == null ||
                (_recalibrateAt >= 0f && Time.unscaledTime < _recalibrateAt)) return original;
            // This clip hangs a leg below its supporting hip. Keep the bone
            // calibration used at acquisition; a delayed mesh minimum selects
            // that leg and raises the whole avatar off the ledge.
            foreach (var info in animator.GetCurrentAnimatorClipInfo(0))
                if (info.clip != null && info.weight > 0.5f && info.clip.name == "PET_SIT_06")
                    return original;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
            if (hips != null && chest != null)
            {
                Vector3 torso = chest.position - hips.position;
                _seatReclining = torso.sqrMagnitude > 1e-8f &&
                    Mathf.Abs(Vector3.Dot(torso.normalized, handler.transform.up)) < 0.5f;
            }
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
            float highest = float.NegativeInfinity;
            bool found = false;
            SkinnedMeshRenderer supportRenderer = null;
            int supportVertex = -1;
            BoneWeight[] supportWeights = null;
            BoneWeight supportWeight = default(BoneWeight);
            Vector3 supportWorld = default(Vector3);
            var points = new List<Vector3>();
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
                        // ponytail: retain the tuned upright path; migrate its
                        // legacy height offset separately before changing it.
                        renderer.BakeMesh(baked, _seatReclining);
                        vertices = baked.vertices;
                    }
                    catch (Exception error) { Report(error); continue; }
                    if (weights.Length != source.vertexCount) continue;
                    var bones = renderer.bones;
                    if (vertices.Length != weights.Length) continue;
                    float rendererLowest = float.PositiveInfinity;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var weight = weights[i];
                        if (!IsSeatContactVertex(bones, contact, weight)) continue;
                        Vector3 point = camera.WorldToScreenPoint(renderer.transform.TransformPoint(vertices[i]));
                        if (point.z > 0f && Finite(point.x) && Finite(point.y) && Finite(point.z))
                        {
                            points.Add(point);
                            rendererLowest = Mathf.Min(rendererLowest, point.y);
                            highest = Mathf.Max(highest, point.y);
                            if (!found || point.y < lowest)
                            {
                                lowest = point.y;
                                supportRenderer = renderer;
                                supportVertex = i;
                                supportWeights = weights;
                                supportWeight = weight;
                                supportWorld = renderer.transform.TransformPoint(vertices[i]);
                            }
                            found = true;
                        }
                    }
                    if (SeatAuditEnabled && Finite(rendererLowest))
                        Debug.Log($"[SeatAudit] candidate pose={_poseIndex} renderer={renderer.name} mesh={source.name} lowestY={rendererLowest:F3}");
                }
            }
            finally { UnityEngine.Object.Destroy(baked); }
            if (!found) return original;
            _seatAuditRenderer = supportRenderer;
            _seatAuditVertex = supportVertex;
            _seatAuditWeights = supportWeights;
            _seatAuditBones = contact;
            _nextSeatAudit = 0f;
            CacheSeatSkinAnchor(supportRenderer, supportWeight, supportWorld, camera);
            // The ledge meets the front of the supporting skin, not the
            // centre of the pelvis. Ignore hanging legs and the upper torso.
            float bandTop = lowest + (highest - lowest) * 0.025f;
            Vector3 front = new Vector3(0f, 0f, float.PositiveInfinity);
            foreach (Vector3 point in points)
                if (point.y <= bandTop && point.z < front.z) front = point;
            if (Finite(front.z))
            {
                _seatFrontLocal = handler.transform.InverseTransformPoint(camera.ScreenToWorldPoint(front));
                _hasSeatFront = true;
            }
            Vector3 reference = camera.WorldToScreenPoint(original);
            return reference.z <= 0f || !Finite(reference.x) || !Finite(reference.z) ? original :
                camera.ScreenToWorldPoint(new Vector3(reference.x, lowest, reference.z));
        }

        static float ContactWeight(Transform[] bones, HashSet<Transform> contact, int index, float weight)
        {
            return weight > 0f && index >= 0 && index < bones.Length && contact.Contains(bones[index]) ? weight : 0f;
        }

        static bool IsSeatContactVertex(Transform[] bones, HashSet<Transform> contact, BoneWeight weight)
        {
            float total = ContactWeight(bones, contact, weight.boneIndex0, weight.weight0) +
                ContactWeight(bones, contact, weight.boneIndex1, weight.weight1) +
                ContactWeight(bones, contact, weight.boneIndex2, weight.weight2) +
                ContactWeight(bones, contact, weight.boneIndex3, weight.weight3);
            return total >= 0.5f;
        }

        static void CacheSeatSkinAnchor(SkinnedMeshRenderer renderer, BoneWeight weight, Vector3 world, Camera camera)
        {
            var indices = new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 };
            var weights = new[] { weight.weight0, weight.weight1, weight.weight2, weight.weight3 };
            var bones = renderer.bones;
            var bindposes = renderer.sharedMesh.bindposes;
            var skin = Matrix4x4.zero;
            for (int i = 0; i < 4; i++)
            {
                if (weights[i] <= 0f) continue;
                int index = indices[i];
                if (index < 0 || index >= bones.Length || index >= bindposes.Length || bones[index] == null) return;
                Matrix4x4 matrix = bones[index].localToWorldMatrix * bindposes[index];
                for (int element = 0; element < 16; element++) skin[element] += matrix[element] * weights[i];
            }
            if (!Finite(skin.determinant) || Mathf.Abs(skin.determinant) < 1e-12f) return;
            // Invert the current weighted skin matrix once. This retains the
            // baked blend-shape position while following the actual bones,
            // including spine/chest in a reclining pose, rather than hips only.
            Vector3 vertex = skin.inverse.MultiplyPoint3x4(world);
            if (!Finite(vertex.x) || !Finite(vertex.y) || !Finite(vertex.z)) return;
            var selectedBones = new Transform[4];
            var local = new Vector3[4];
            for (int i = 0; i < 4; i++)
                if (weights[i] > 0f)
                {
                    selectedBones[i] = bones[indices[i]];
                    local[i] = bindposes[indices[i]].MultiplyPoint3x4(vertex);
                }
            _contactSkinBones = selectedBones;
            _contactSkinLocal = local;
            _contactSkinWeights = weights;
            // Runnable live invariant, opt-in after an authorized build.
            if (SeatAuditEnabled)
            {
                Vector3 reconstructed;
                if (!TrySeatSkinAnchor(out reconstructed) || Vector3.Distance(reconstructed, world) >=
                    Mathf.Max(0.0001f, renderer.transform.lossyScale.magnitude * 0.0001f))
                    throw new InvalidOperationException("[SeatAudit] weighted skin anchor changed the calibrated contact");
                AuditSeatMeshSpace(renderer, skin, world, camera);
            }
        }

        static void AuditSeatMeshSpace(SkinnedMeshRenderer renderer, Matrix4x4 skin, Vector3 bakedWorld, Camera camera)
        {
            var mesh = renderer.sharedMesh;
            Vector3 vertex = mesh.vertices[_seatAuditVertex];
            Vector3[] delta = null, normals = null, tangents = null;
            for (int shape = 0; shape < mesh.blendShapeCount; shape++)
            {
                float weight = renderer.GetBlendShapeWeight(shape);
                if (weight == 0f) continue;
                if (mesh.GetBlendShapeFrameCount(shape) != 1 || mesh.GetBlendShapeFrameWeight(shape, 0) <= 0f)
                {
                    Debug.Log($"[SeatAudit] mesh-space comparison skipped: multi-frame blend shape {mesh.GetBlendShapeName(shape)}");
                    return;
                }
                if (delta == null)
                {
                    delta = new Vector3[mesh.vertexCount];
                    normals = new Vector3[mesh.vertexCount];
                    tangents = new Vector3[mesh.vertexCount];
                }
                mesh.GetBlendShapeFrameVertices(shape, 0, delta, normals, tangents);
                vertex += delta[_seatAuditVertex] * (weight / mesh.GetBlendShapeFrameWeight(shape, 0));
            }
            Vector3 canonical = skin.MultiplyPoint3x4(vertex);
            Vector3 baked = camera.WorldToScreenPoint(bakedWorld);
            Vector3 expected = camera.WorldToScreenPoint(canonical);
            var scaledMesh = new Mesh();
            Vector3 scaled;
            try
            {
                renderer.BakeMesh(scaledMesh, true);
                scaled = camera.WorldToScreenPoint(renderer.transform.TransformPoint(scaledMesh.vertices[_seatAuditVertex]));
            }
            finally { UnityEngine.Object.Destroy(scaledMesh); }
            Debug.Log($"[SeatAudit] mesh-space pose={_poseIndex} renderer={renderer.name} vertex={_seatAuditVertex} " +
                $"bakedY={baked.y:F3} canonicalY={expected.y:F3} differenceY={baked.y - expected.y:F3} " +
                $"useScaleTrueY={scaled.y:F3} useScaleTrueError={scaled.y - expected.y:F3} " +
                $"rendererScale={renderer.transform.lossyScale} rootBoneScale={(renderer.rootBone != null ? renderer.rootBone.lossyScale : Vector3.one)} " +
                $"sourceVertex={vertex} bakedWorld={bakedWorld} canonicalWorld={canonical}");
            if (_seatReclining && Mathf.Abs(baked.y - expected.y) > 0.5f)
                throw new InvalidOperationException("[SeatAudit] reclining contact differs from source-mesh skinning by more than half a pixel");
        }

        static bool TrySeatSkinAnchor(out Vector3 world)
        {
            world = Vector3.zero;
            if (_contactSkinBones == null) return false;
            for (int i = 0; i < 4; i++)
            {
                if (_contactSkinWeights[i] <= 0f) continue;
                if (_contactSkinBones[i] == null) return false;
                world += _contactSkinBones[i].TransformPoint(_contactSkinLocal[i]) * _contactSkinWeights[i];
            }
            return Finite(world.x) && Finite(world.y) && Finite(world.z);
        }

        static void AuditSeatContact(AvatarWindowHandler handler, Vector3 skin, Vector3 seat, ShojiSnapshot snapshot)
        {
            if (!SeatAuditEnabled || _seatAuditRenderer == null || Time.unscaledTime < _nextSeatAudit) return;
            _nextSeatAudit = Time.unscaledTime + 1f;
            var mesh = new Mesh();
            try
            {
                var renderer = _seatAuditRenderer;
                renderer.BakeMesh(mesh, _seatReclining);
                Vector3[] vertices = mesh.vertices;
                if (_seatAuditVertex < 0 || _seatAuditVertex >= vertices.Length) return;
                Vector3 bakedWorld = renderer.transform.TransformPoint(vertices[_seatAuditVertex]);
                var camera = handler.targetCamera;
                Vector3 predicted = camera.WorldToScreenPoint(skin);
                Vector3 actual = camera.WorldToScreenPoint(bakedWorld);
                float lowestY = actual.y;
                var bones = renderer.bones;
                for (int i = 0; i < vertices.Length && i < _seatAuditWeights.Length; i++)
                {
                    var weight = _seatAuditWeights[i];
                    if (!IsSeatContactVertex(bones, _seatAuditBones, weight)) continue;
                    Vector3 point = camera.WorldToScreenPoint(renderer.transform.TransformPoint(vertices[i]));
                    if (point.z > 0f && Finite(point.y)) lowestY = Mathf.Min(lowestY, point.y);
                }
                Vector3 pinned = camera.WorldToScreenPoint(seat);
                var support = Surface((IntPtr)SeatHandle.GetValue(handler));
                if (support == null) return;
                float pixelsPerUnityPixel = snapshot.Window.height / (float)camera.pixelHeight;
                float desktopY = snapshot.Window.y + (camera.pixelHeight - pinned.y) * pixelsPerUnityPixel;
                float pinError = desktopY - support.Rect.y - handler.seatOffsetPx;
                Vector3 hipsContact = (Vector3)SeatLocal.GetValue(handler);
                Vector3 size = (Vector3)SeatBoundsSize.GetValue(handler);
                hipsContact.y = ((Vector3)SeatBoundsMinimum.GetValue(handler)).y +
                    Mathf.Clamp((float)SeatNormalizedY.GetValue(handler) + handler.windowSitYOffset, -0.5f, 1.5f) * size.y +
                    handler.transform.InverseTransformPoint(_contactHips.position).y - _contactHipsLocal.y;
                float hipsY = camera.WorldToScreenPoint(handler.transform.TransformPoint(hipsContact)).y;
                Debug.Log($"[SeatAudit] contact pose={_poseIndex} target={support.Id} renderer={renderer.name} " +
                    $"reclining={_seatReclining} " +
                    $"tracking={(_seatReclining ? "skin" : "hips")} hipsSeatY={hipsY:F3} " +
                    $"mesh={renderer.sharedMesh.name} vertex={_seatAuditVertex} " +
                    $"predictedY={predicted.y:F3} bakedY={actual.y:F3} " +
                    $"skinErrorPx={(predicted.y - actual.y) * pixelsPerUnityPixel:F3} " +
                    $"contactChangePx={(actual.y - lowestY) * pixelsPerUnityPixel:F3} " +
                    $"manualOffsetPx={(pinned.y - predicted.y) * pixelsPerUnityPixel:F3} " +
                    $"pinErrorPx={pinError:F3} planeDepth={camera.nearClipPlane + handler.targetQuadZOffset:F5} " +
                    $"skinDepth={actual.z:F5} window={snapshot.Window} support={support.Rect} " +
                    $"pinnedMenus={_menuPlacements.Count}");
            }
            catch (Exception error) { Report(error); }
            finally { UnityEngine.Object.Destroy(mesh); }
        }

    }
}
