using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UniVRM10;
using UnityEngine;
using VRM;

public class AvatarGravityController : MonoBehaviour
{
	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	[Header("Impact Settings")]
	[Tooltip("How much motion from window drag affects SpringBones")]
	public float impactMultiplier = 0.05f;

	[Header("Debug")]
	public bool showDebugForce = true;

	public Color debugColor = Color.cyan;

	private Vector2Int previousWindowPos;

	private Vector3 currentForce;

	private List<VRMSpringBone> springBones = new List<VRMSpringBone>();

	private List<VRM10SpringBoneJoint> springBoneJoints = new List<VRM10SpringBoneJoint>();

	private Vrm10Instance vrm10Instance;

	private IntPtr unityHWND;

	private AvatarHideHandler hideHandler;

	private AvatarWindowHandler windowHandler;

	private bool auditMotion;

	private float nextAuditAt;

	private int auditMoves;

	private int auditImpulses;

	private float auditMaxForce;

	private void Start()
	{
		unityHWND = WindowManager.Instance.UnityWindow;
		hideHandler = GetComponent<AvatarHideHandler>();
		windowHandler = GetComponent<AvatarWindowHandler>();
		previousWindowPos = GetWindowPosition();
		springBones.AddRange(GetComponentsInChildren<VRMSpringBone>(includeInactive: true));
		springBoneJoints.AddRange(GetComponentsInChildren<VRM10SpringBoneJoint>(includeInactive: true));
		vrm10Instance = GetComponentInParent<Vrm10Instance>();
		auditMotion = Environment.GetEnvironmentVariable("MATEENGINE_SPRING_AUDIT") == "1";
	}

	private void Update()
	{
		Vector2Int windowPosition = GetWindowPosition();
		Vector2Int vector2Int = windowPosition - previousWindowPos;
		// Both ledge pinning and hiding compensate for animated bones by
		// moving the client. A one-pixel correction used to create the same
		// force as a large drag; the animator's drag flag also lingers on release.
		bool anchored = (hideHandler != null && hideHandler.IsAnchored) ||
			(windowHandler != null && windowHandler.IsAnchored);
		var wm = WindowManager.Instance;
		bool userDragging = wm != null && wm.IsDragging && Input.GetMouseButton(0);
		if (WindowManager.AllowDesktopActivity && (!anchored || userDragging) && vector2Int != Vector2Int.zero)
		{
			Vector3 vector = new Vector3(-vector2Int.x, vector2Int.y, 0f).normalized * impactMultiplier;
			currentForce = vector;
		}
		else
		{
			currentForce = Vector3.zero;
		}
		foreach (VRMSpringBone springBone in springBones)
		{
			if (springBone != null)
			{
				springBone.ExternalForce = currentForce;
			}
		}
		foreach (VRM10SpringBoneJoint springBoneJoint in springBoneJoints)
		{
			if (!(springBoneJoint == null))
			{
				springBoneJoint.m_gravityDir = currentForce.normalized;
				springBoneJoint.m_gravityPower = currentForce.magnitude;
				if (vrm10Instance != null && vrm10Instance.Runtime != null)
				{
					vrm10Instance.Runtime.SpringBone.SetJointLevel(springBoneJoint.transform, springBoneJoint.Blittable);
				}
			}
		}
		previousWindowPos = windowPosition;
		if (auditMotion)
		{
			if (vector2Int != Vector2Int.zero) auditMoves++;
			if (currentForce != Vector3.zero) auditImpulses++;
			auditMaxForce = Mathf.Max(auditMaxForce, currentForce.magnitude);
			if (Time.unscaledTime >= nextAuditAt)
			{
				UnityEngine.Debug.Log($"[SpringMotionAudit] edge={hideHandler != null && hideHandler.IsAnchored} " +
					$"seat={windowHandler != null && windowHandler.IsAnchored} userDrag={userDragging} " +
					$"visible={WindowManager.AllowDesktopActivity} windowMoves={auditMoves} " +
					$"impulseFrames={auditImpulses} maxForce={auditMaxForce:F4} vrm0={springBones.Count}");
				nextAuditAt = Time.unscaledTime + 2f;
				auditMoves = auditImpulses = 0;
				auditMaxForce = 0f;
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (showDebugForce)
		{
			Gizmos.color = debugColor;
			Gizmos.DrawLine(base.transform.position, base.transform.position + currentForce);
			Gizmos.DrawSphere(base.transform.position + currentForce, 0.02f);
		}
	}

	private Vector2Int GetWindowPosition()
	{
		if (!GetWindowRect(unityHWND, out var lpRect)) return previousWindowPos;
		return new Vector2Int(lpRect.left, lpRect.top);
	}

	private static bool GetWindowRect(IntPtr window, out RECT rect)
	{
		rect = default;
		var wm = WindowManager.Instance;
		if (wm == null || !wm.GetWindowRect(window, out var bounds)) return false;
		rect = new RECT { left = bounds.xMin, top = bounds.yMin, right = bounds.xMax, bottom = bounds.yMax };
		return true;
	}
}
