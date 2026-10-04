using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

public class MEValueChanger : MonoBehaviour
{
	[Header("Targets")]
	[SerializeField]
	private List<MEValueRuntimeEntry> targets = new List<MEValueRuntimeEntry>();

	[Header("Runtime UI")]
	[SerializeField]
	private bool showUIOnStart;

	[SerializeField]
	private KeyCode toggleKey = KeyCode.F8;

	[SerializeField]
	private Rect windowRect = new Rect(40f, 40f, 720f, 720f);

	[Header("Profiles")]
	[SerializeField]
	private string profileName = "Default";

	[SerializeField]
	private bool autoLoadOnStart;

	[Header("Deactivate While Open")]
	[SerializeField]
	private List<GameObject> deactivateWhileOpen = new List<GameObject>();

	private bool showUI;

	private Vector2 mainScroll;

	private string status;

	private float statusUntil;

	private static Texture2D whiteTex;

	private readonly Dictionary<GameObject, bool> prevActive = new Dictionary<GameObject, bool>();

	private void Start()
	{
		showUI = showUIOnStart;
		LoadSettings();
		if (autoLoadOnStart)
		{
			TryAutoLoad();
		}
		TryAttachCustomVRM();
		if (showUI)
		{
			ApplyDeactivateWhileOpen(visible: true);
		}
	}

	private void TryAttachCustomVRM()
	{
		VRMLoader vRMLoader = UnityEngine.Object.FindFirstObjectByType<VRMLoader>();
		if (vRMLoader != null)
		{
			GameObject clone = vRMLoader.GetCurrentModel();
			if (clone != null && !targets.Exists((MEValueRuntimeEntry t) => t.targetObject == clone))
			{
				MEValueRuntimeEntry mEValueRuntimeEntry = new MEValueRuntimeEntry
				{
					targetObject = clone
				};
				targets.Add(mEValueRuntimeEntry);
				RefreshTargetLists(mEValueRuntimeEntry);
			}
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(toggleKey))
		{
			showUI = !showUI;
			ApplyDeactivateWhileOpen(showUI);
		}
	}

	private void OnGUI()
	{
		if (Application.isPlaying && showUI)
		{
			windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "ME Value Changer (Runtime)");
		}
	}

	private void DrawWindow(int id)
	{
		GUILayout.Label("Playmode only. Changes are not saved unless you Save a profile.", GUI.skin.box);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Profile", GUILayout.Width(60f));
		profileName = GUILayout.TextField(profileName, GUILayout.MinWidth(160f));
		if (GUILayout.Button("Save", GUILayout.Width(80f)))
		{
			SaveCurrentProfile();
			SaveSettings();
		}
		if (GUILayout.Button("Load", GUILayout.Width(80f)))
		{
			LoadProfile(profileName, showResult: true);
			SaveSettings();
		}
		bool flag = GUILayout.Toggle(autoLoadOnStart, "Auto Load on Start", GUILayout.Width(180f));
		if (flag != autoLoadOnStart)
		{
			autoLoadOnStart = flag;
			SaveSettings();
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Refresh All", GUILayout.Width(110f)))
		{
			for (int i = 0; i < targets.Count; i++)
			{
				RefreshTargetLists(targets[i]);
			}
		}
		GUILayout.EndHorizontal();
		if (!string.IsNullOrEmpty(status) && Time.realtimeSinceStartup < statusUntil)
		{
			GUILayout.Label(status, GUI.skin.label);
		}
		mainScroll = GUILayout.BeginScrollView(mainScroll);
		for (int j = 0; j < targets.Count; j++)
		{
			MEValueRuntimeEntry mEValueRuntimeEntry = targets[j];
			GUILayout.BeginVertical(GUI.skin.window);
			mEValueRuntimeEntry.foldout = Foldout(mEValueRuntimeEntry.foldout, mEValueRuntimeEntry.targetObject ? mEValueRuntimeEntry.targetObject.name : "(None)");
			if (mEValueRuntimeEntry.foldout)
			{
				GUILayout.Space(4f);
				GUILayout.BeginHorizontal();
				GUILayout.Label("Target", GUILayout.Width(80f));
				GUILayout.Label(mEValueRuntimeEntry.targetObject ? mEValueRuntimeEntry.targetObject.name : "None", GUI.skin.textField);
				GUILayout.EndHorizontal();
				if (mEValueRuntimeEntry.targetObject == null)
				{
					GUILayout.Label("Assign a GameObject in the Inspector.");
				}
				else
				{
					if (mEValueRuntimeEntry.comps == null || mEValueRuntimeEntry.compNames == null)
					{
						BuildComponentList(mEValueRuntimeEntry);
					}
					if (mEValueRuntimeEntry.comps != null)
					{
						for (int k = 0; k < mEValueRuntimeEntry.comps.Length; k++)
						{
							Component component = mEValueRuntimeEntry.comps[k];
							if (!mEValueRuntimeEntry.compFoldouts.ContainsKey(component))
							{
								mEValueRuntimeEntry.compFoldouts[component] = false;
							}
							mEValueRuntimeEntry.compFoldouts[component] = Foldout(mEValueRuntimeEntry.compFoldouts[component], component ? component.GetType().Name : "(Missing)");
							if (!mEValueRuntimeEntry.compFoldouts[component])
							{
								continue;
							}
							if (!mEValueRuntimeEntry.membersPerComp.ContainsKey(component) || mEValueRuntimeEntry.membersPerComp[component] == null)
							{
								BuildMemberList(mEValueRuntimeEntry, component);
							}
							GUILayout.Space(6f);
							GUILayout.Label("Editable Fields / Properties", GUI.skin.box);
							mEValueRuntimeEntry.scrollMembers = GUILayout.BeginScrollView(mEValueRuntimeEntry.scrollMembers, GUILayout.MinHeight(240f));
							List<MemberEntry> list = (mEValueRuntimeEntry.membersPerComp.ContainsKey(component) ? mEValueRuntimeEntry.membersPerComp[component] : null);
							if (list != null && list.Count > 0)
							{
								for (int l = 0; l < list.Count; l++)
								{
									DrawMemberRow(component, list[l]);
								}
							}
							else
							{
								GUILayout.Label("No editable members found.");
							}
							GUILayout.EndScrollView();
							Animator animator = component as Animator;
							if (animator != null)
							{
								GUILayout.Space(6f);
								mEValueRuntimeEntry.showAnimatorParams = Foldout(mEValueRuntimeEntry.showAnimatorParams, "Animator Parameters");
								if (mEValueRuntimeEntry.showAnimatorParams)
								{
									DrawAnimatorParams(animator);
								}
							}
						}
					}
				}
			}
			GUILayout.EndVertical();
			GUILayout.Space(6f);
		}
		GUILayout.EndScrollView();
		GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
	}

	private void TryAutoLoad()
	{
		LoadProfile(profileName, showResult: false);
	}

	private void RefreshTargetLists(MEValueRuntimeEntry e)
	{
		e.comps = null;
		e.compNames = null;
		e.membersPerComp.Clear();
		e.compFoldouts.Clear();
		if (e.targetObject != null)
		{
			BuildComponentList(e);
		}
	}

	private void BuildComponentList(MEValueRuntimeEntry e)
	{
		if (!(e.targetObject == null))
		{
			e.comps = e.targetObject.GetComponents<Component>();
			List<string> list = new List<string>(e.comps.Length);
			for (int i = 0; i < e.comps.Length; i++)
			{
				list.Add(e.comps[i] ? e.comps[i].GetType().Name : "(Missing)");
			}
			e.compNames = list.ToArray();
		}
	}

	private void BuildMemberList(MEValueRuntimeEntry e, Component comp)
	{
		if (!comp)
		{
			return;
		}
		List<MemberEntry> list = new List<MemberEntry>();
		Type type = comp.GetType();
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in fields)
		{
			BuildMemberFromInfo(list, comp, fieldInfo, fieldInfo.FieldType);
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.CanWrite && propertyInfo.CanRead)
			{
				ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();
				if (indexParameters == null || indexParameters.Length == 0)
				{
					BuildMemberFromInfo(list, comp, propertyInfo, propertyInfo.PropertyType);
				}
			}
		}
		e.membersPerComp[comp] = list;
	}

	private void BuildMemberFromInfo(List<MemberEntry> list, Component comp, MemberInfo mi, Type mt)
	{
		if (!IsSupportedType(mt))
		{
			return;
		}
		MemberEntry memberEntry = new MemberEntry();
		memberEntry.member = mi;
		memberEntry.type = mt;
		memberEntry.displayName = ((mi is FieldInfo) ? "F: " : "P: ") + mi.Name + " (" + SimpleTypeName(mt) + ")";
		MemberEntry memberEntry2 = memberEntry;
		object[] array = ((mi is FieldInfo fieldInfo) ? fieldInfo.GetCustomAttributes(inherit: true) : (mi as PropertyInfo).GetCustomAttributes(inherit: true));
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is RangeAttribute rangeAttribute)
			{
				memberEntry2.hasRange = true;
				memberEntry2.rangeMin = rangeAttribute.min;
				memberEntry2.rangeMax = rangeAttribute.max;
				break;
			}
		}
		if (mt == typeof(Color) || mt == typeof(Color32))
		{
			memberEntry2.isColor = mt == typeof(Color);
			memberEntry2.isColor32 = mt == typeof(Color32);
			if (memberEntry2.isColor)
			{
				Color color = (Color)GetMemberValue(mi, comp);
				memberEntry2.cr = color.r.ToString("0.###", CultureInfo.InvariantCulture);
				memberEntry2.cg = color.g.ToString("0.###", CultureInfo.InvariantCulture);
				memberEntry2.cb = color.b.ToString("0.###", CultureInfo.InvariantCulture);
				memberEntry2.ca = color.a.ToString("0.###", CultureInfo.InvariantCulture);
			}
			else
			{
				Color32 color2 = (Color32)GetMemberValue(mi, comp);
				memberEntry2.cr = color2.r.ToString(CultureInfo.InvariantCulture);
				memberEntry2.cg = color2.g.ToString(CultureInfo.InvariantCulture);
				memberEntry2.cb = color2.b.ToString(CultureInfo.InvariantCulture);
				memberEntry2.ca = color2.a.ToString(CultureInfo.InvariantCulture);
			}
		}
		else if (mt == typeof(string))
		{
			memberEntry2.editCache = ((string)GetMemberValue(mi, comp)) ?? "";
		}
		else if (mt == typeof(Vector2))
		{
			memberEntry2.isV2 = true;
			Vector2 vector = (Vector2)GetMemberValue(mi, comp);
			memberEntry2.v2x = vector.x.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v2y = vector.y.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (mt == typeof(Vector3))
		{
			memberEntry2.isV3 = true;
			Vector3 vector2 = (Vector3)GetMemberValue(mi, comp);
			memberEntry2.v3x = vector2.x.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v3y = vector2.y.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v3z = vector2.z.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (mt == typeof(Vector4))
		{
			memberEntry2.isV4 = true;
			Vector4 vector3 = (Vector4)GetMemberValue(mi, comp);
			memberEntry2.v4x = vector3.x.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v4y = vector3.y.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v4z = vector3.z.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.v4w = vector3.w.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (mt == typeof(Quaternion))
		{
			memberEntry2.isQuat = true;
			Quaternion quaternion = (Quaternion)GetMemberValue(mi, comp);
			memberEntry2.qx = quaternion.x.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.qy = quaternion.y.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.qz = quaternion.z.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.qw = quaternion.w.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (mt == typeof(Rect))
		{
			memberEntry2.isRect = true;
			Rect rect = (Rect)GetMemberValue(mi, comp);
			memberEntry2.rx = rect.x.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.ry = rect.y.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.rw = rect.width.ToString("0.###", CultureInfo.InvariantCulture);
			memberEntry2.rh = rect.height.ToString("0.###", CultureInfo.InvariantCulture);
		}
		else if (mt.IsEnum)
		{
			memberEntry2.isEnum = true;
			memberEntry2.enumValues = Enum.GetValues(mt);
			memberEntry2.enumNames = Enum.GetNames(mt);
			object memberValue = GetMemberValue(mi, comp);
			memberEntry2.enumIndex = Array.IndexOf(memberEntry2.enumValues, memberValue);
			if (memberEntry2.enumIndex < 0)
			{
				memberEntry2.enumIndex = 0;
			}
		}
		else if (mt == typeof(AudioClip))
		{
			memberEntry2.isAudioClip = true;
			memberEntry2.audioPathCache = "";
			memberEntry2.audioStatus = "";
		}
		else
		{
			memberEntry2.editCache = GetValueString(comp, mi, mt);
		}
		list.Add(memberEntry2);
	}

	private static bool IsSupportedType(Type tp)
	{
		if (tp == typeof(float) || tp == typeof(int) || tp == typeof(bool) || tp == typeof(string))
		{
			return true;
		}
		if (tp == typeof(Color) || tp == typeof(Color32))
		{
			return true;
		}
		if (tp == typeof(Vector2) || tp == typeof(Vector3) || tp == typeof(Vector4))
		{
			return true;
		}
		if (tp == typeof(Quaternion) || tp == typeof(Rect))
		{
			return true;
		}
		if (tp == typeof(AudioClip))
		{
			return true;
		}
		if (tp.IsEnum)
		{
			return true;
		}
		return false;
	}

	private static string SimpleTypeName(Type t)
	{
		if (t == typeof(float))
		{
			return "float";
		}
		if (t == typeof(int))
		{
			return "int";
		}
		if (t == typeof(bool))
		{
			return "bool";
		}
		if (t == typeof(string))
		{
			return "string";
		}
		if (t == typeof(Color))
		{
			return "Color";
		}
		if (t == typeof(Color32))
		{
			return "Color32";
		}
		if (t == typeof(Vector2))
		{
			return "Vector2";
		}
		if (t == typeof(Vector3))
		{
			return "Vector3";
		}
		if (t == typeof(Vector4))
		{
			return "Vector4";
		}
		if (t == typeof(Quaternion))
		{
			return "Quaternion";
		}
		if (t == typeof(Rect))
		{
			return "Rect";
		}
		if (t == typeof(AudioClip))
		{
			return "AudioClip";
		}
		if (t.IsEnum)
		{
			return "Enum";
		}
		return t.Name;
	}

	private static object GetMemberValue(MemberInfo m, Component c)
	{
		if (m is FieldInfo fieldInfo)
		{
			return fieldInfo.GetValue(c);
		}
		if (m is PropertyInfo propertyInfo)
		{
			return propertyInfo.GetValue(c, null);
		}
		return null;
	}

	private static void SetMemberValue(MemberInfo m, Component c, object value)
	{
		if (m is FieldInfo fieldInfo)
		{
			fieldInfo.SetValue(c, value);
		}
		else if (m is PropertyInfo { CanWrite: not false } propertyInfo)
		{
			propertyInfo.SetValue(c, value, null);
		}
	}

	private static string GetValueString(Component c, MemberInfo m, Type tp)
	{
		object memberValue = GetMemberValue(m, c);
		if (tp == typeof(float))
		{
			return ((float)memberValue).ToString("0.###", CultureInfo.InvariantCulture);
		}
		if (tp == typeof(int))
		{
			return ((int)memberValue).ToString(CultureInfo.InvariantCulture);
		}
		if (tp == typeof(bool))
		{
			if (!(bool)memberValue)
			{
				return "false";
			}
			return "true";
		}
		if (tp == typeof(string))
		{
			return (memberValue as string) ?? "";
		}
		if (memberValue == null)
		{
			return "null";
		}
		return memberValue.ToString();
	}

	private void DrawMemberRow(Component comp, MemberEntry entry)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(entry.displayName, GUILayout.Width(300f));
		if (entry.isColor)
		{
			if (whiteTex == null)
			{
				whiteTex = MakeTex(Color.white);
			}
			float.TryParse(entry.cr, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
			float.TryParse(entry.cg, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2);
			float.TryParse(entry.cb, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3);
			float.TryParse(entry.ca, NumberStyles.Float, CultureInfo.InvariantCulture, out var result4);
			result = Mathf.Clamp01(result);
			result2 = Mathf.Clamp01(result2);
			result3 = Mathf.Clamp01(result3);
			result4 = Mathf.Clamp01(result4);
			Color color = new Color(result, result2, result3, result4);
			Rect rect = GUILayoutUtility.GetRect(32f, 18f, GUILayout.Width(32f));
			Color color2 = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, whiteTex);
			GUI.color = color2;
			entry.cr = GUILayout.TextField(result.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.cg = GUILayout.TextField(result2.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.cb = GUILayout.TextField(result3.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.ca = GUILayout.TextField(result4.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(60f));
			SetMemberValue(entry.member, comp, color);
		}
		else if (entry.isColor32)
		{
			if (whiteTex == null)
			{
				whiteTex = MakeTex(Color.white);
			}
			int.TryParse(entry.cr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result5);
			int.TryParse(entry.cg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result6);
			int.TryParse(entry.cb, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result7);
			int.TryParse(entry.ca, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result8);
			result5 = Mathf.Clamp(result5, 0, 255);
			result6 = Mathf.Clamp(result6, 0, 255);
			result7 = Mathf.Clamp(result7, 0, 255);
			result8 = Mathf.Clamp(result8, 0, 255);
			Color32 color3 = new Color32((byte)result5, (byte)result6, (byte)result7, (byte)result8);
			Rect rect2 = GUILayoutUtility.GetRect(32f, 18f, GUILayout.Width(32f));
			Color color4 = GUI.color;
			GUI.color = new Color((float)(int)color3.r / 255f, (float)(int)color3.g / 255f, (float)(int)color3.b / 255f, 1f);
			GUI.DrawTexture(rect2, whiteTex);
			GUI.color = color4;
			entry.cr = GUILayout.TextField(result5.ToString(CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.cg = GUILayout.TextField(result6.ToString(CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.cb = GUILayout.TextField(result7.ToString(CultureInfo.InvariantCulture), GUILayout.Width(60f));
			entry.ca = GUILayout.TextField(result8.ToString(CultureInfo.InvariantCulture), GUILayout.Width(60f));
			SetMemberValue(entry.member, comp, color3);
		}
		else if (entry.isV2)
		{
			entry.v2x = GUILayout.TextField(entry.v2x, GUILayout.Width(70f));
			entry.v2y = GUILayout.TextField(entry.v2y, GUILayout.Width(70f));
			if (float.TryParse(entry.v2x, NumberStyles.Float, CultureInfo.InvariantCulture, out var result9) && float.TryParse(entry.v2y, NumberStyles.Float, CultureInfo.InvariantCulture, out var result10))
			{
				SetMemberValue(entry.member, comp, new Vector2(result9, result10));
			}
		}
		else if (entry.isV3)
		{
			entry.v3x = GUILayout.TextField(entry.v3x, GUILayout.Width(70f));
			entry.v3y = GUILayout.TextField(entry.v3y, GUILayout.Width(70f));
			entry.v3z = GUILayout.TextField(entry.v3z, GUILayout.Width(70f));
			if (float.TryParse(entry.v3x, NumberStyles.Float, CultureInfo.InvariantCulture, out var result11) && float.TryParse(entry.v3y, NumberStyles.Float, CultureInfo.InvariantCulture, out var result12) && float.TryParse(entry.v3z, NumberStyles.Float, CultureInfo.InvariantCulture, out var result13))
			{
				SetMemberValue(entry.member, comp, new Vector3(result11, result12, result13));
			}
		}
		else if (entry.isV4)
		{
			entry.v4x = GUILayout.TextField(entry.v4x, GUILayout.Width(60f));
			entry.v4y = GUILayout.TextField(entry.v4y, GUILayout.Width(60f));
			entry.v4z = GUILayout.TextField(entry.v4z, GUILayout.Width(60f));
			entry.v4w = GUILayout.TextField(entry.v4w, GUILayout.Width(60f));
			if (float.TryParse(entry.v4x, NumberStyles.Float, CultureInfo.InvariantCulture, out var result14) && float.TryParse(entry.v4y, NumberStyles.Float, CultureInfo.InvariantCulture, out var result15) && float.TryParse(entry.v4z, NumberStyles.Float, CultureInfo.InvariantCulture, out var result16) && float.TryParse(entry.v4w, NumberStyles.Float, CultureInfo.InvariantCulture, out var result17))
			{
				SetMemberValue(entry.member, comp, new Vector4(result14, result15, result16, result17));
			}
		}
		else if (entry.isQuat)
		{
			entry.qx = GUILayout.TextField(entry.qx, GUILayout.Width(60f));
			entry.qy = GUILayout.TextField(entry.qy, GUILayout.Width(60f));
			entry.qz = GUILayout.TextField(entry.qz, GUILayout.Width(60f));
			entry.qw = GUILayout.TextField(entry.qw, GUILayout.Width(60f));
			if (float.TryParse(entry.qx, NumberStyles.Float, CultureInfo.InvariantCulture, out var result18) && float.TryParse(entry.qy, NumberStyles.Float, CultureInfo.InvariantCulture, out var result19) && float.TryParse(entry.qz, NumberStyles.Float, CultureInfo.InvariantCulture, out var result20) && float.TryParse(entry.qw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result21))
			{
				SetMemberValue(entry.member, comp, new Quaternion(result18, result19, result20, result21));
			}
		}
		else if (entry.isRect)
		{
			entry.rx = GUILayout.TextField(entry.rx, GUILayout.Width(60f));
			entry.ry = GUILayout.TextField(entry.ry, GUILayout.Width(60f));
			entry.rw = GUILayout.TextField(entry.rw, GUILayout.Width(60f));
			entry.rh = GUILayout.TextField(entry.rh, GUILayout.Width(60f));
			if (float.TryParse(entry.rx, NumberStyles.Float, CultureInfo.InvariantCulture, out var result22) && float.TryParse(entry.ry, NumberStyles.Float, CultureInfo.InvariantCulture, out var result23) && float.TryParse(entry.rw, NumberStyles.Float, CultureInfo.InvariantCulture, out var result24) && float.TryParse(entry.rh, NumberStyles.Float, CultureInfo.InvariantCulture, out var result25))
			{
				SetMemberValue(entry.member, comp, new Rect(result22, result23, result24, result25));
			}
		}
		else if (entry.isEnum)
		{
			if (GUILayout.Button("◂", GUILayout.Width(28f)))
			{
				entry.enumIndex = (entry.enumIndex - 1 + entry.enumNames.Length) % entry.enumNames.Length;
				SetMemberValue(entry.member, comp, entry.enumValues.GetValue(entry.enumIndex));
			}
			GUILayout.Label(entry.enumNames[entry.enumIndex], GUILayout.Width(160f));
			if (GUILayout.Button("▸", GUILayout.Width(28f)))
			{
				entry.enumIndex = (entry.enumIndex + 1) % entry.enumNames.Length;
				SetMemberValue(entry.member, comp, entry.enumValues.GetValue(entry.enumIndex));
			}
		}
		else if (entry.isAudioClip)
		{
			AudioClip audioClip = GetMemberValue(entry.member, comp) as AudioClip;
			GUILayout.Label(audioClip ? audioClip.name : "(none)", GUILayout.Width(150f));
			entry.audioPathCache = GUILayout.TextField(entry.audioPathCache ?? "", GUILayout.MinWidth(220f), GUILayout.ExpandWidth(expand: true));
			if (GUILayout.Button("Load", GUILayout.Width(70f)) && !entry.audioLoading && !string.IsNullOrWhiteSpace(entry.audioPathCache))
			{
				StartCoroutine(LoadAudioClipFromPath(entry.member, comp, entry.audioPathCache, delegate(string s)
				{
					entry.audioStatus = s;
				}, delegate(AudioClip clipOk)
				{
					if (clipOk != null)
					{
						SetMemberValue(entry.member, comp, clipOk);
					}
				}));
			}
			if (GUILayout.Button("Clear", GUILayout.Width(70f)))
			{
				SetMemberValue(entry.member, comp, null);
			}
			if (!string.IsNullOrEmpty(entry.audioStatus))
			{
				GUILayout.Label(entry.audioStatus, GUILayout.Width(160f));
			}
		}
		else if (entry.type == typeof(string))
		{
			string text = GUILayout.TextField(entry.editCache ?? "", GUILayout.MinWidth(220f), GUILayout.ExpandWidth(expand: true));
			if ((object)text != entry.editCache)
			{
				entry.editCache = text;
				SetMemberValue(entry.member, comp, text);
			}
		}
		else if (entry.type == typeof(bool))
		{
			bool flag = (bool)GetMemberValue(entry.member, comp);
			bool flag2 = GUILayout.Toggle(flag, flag ? "true" : "false", GUILayout.Width(80f));
			if (flag2 != flag)
			{
				SetMemberValue(entry.member, comp, flag2);
				entry.editCache = (flag2 ? "true" : "false");
			}
		}
		else if (entry.type == typeof(int))
		{
			if (entry.hasRange)
			{
				int num = (int)GetMemberValue(entry.member, comp);
				int num2 = Mathf.RoundToInt(GUILayout.HorizontalSlider(num, entry.rangeMin, entry.rangeMax, GUILayout.Width(160f)));
				if (int.TryParse(GUILayout.TextField(num2.ToString(CultureInfo.InvariantCulture), GUILayout.Width(80f)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result26))
				{
					num2 = Mathf.Clamp(result26, (int)entry.rangeMin, (int)entry.rangeMax);
				}
				if (num2 != num)
				{
					SetMemberValue(entry.member, comp, num2);
				}
				GUILayout.Label(num2.ToString(CultureInfo.InvariantCulture), GUILayout.Width(50f));
			}
			else
			{
				string text2 = GUILayout.TextField(entry.editCache ?? GetValueString(comp, entry.member, typeof(int)), GUILayout.MinWidth(120f));
				if ((object)text2 != entry.editCache)
				{
					entry.editCache = text2;
				}
				if (int.TryParse(entry.editCache, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result27))
				{
					SetMemberValue(entry.member, comp, result27);
				}
			}
		}
		else if (entry.type == typeof(float))
		{
			if (entry.hasRange)
			{
				float num3 = (float)GetMemberValue(entry.member, comp);
				float num4 = GUILayout.HorizontalSlider(num3, entry.rangeMin, entry.rangeMax, GUILayout.Width(160f));
				if (float.TryParse(GUILayout.TextField(num4.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(80f)), NumberStyles.Float, CultureInfo.InvariantCulture, out var result28))
				{
					num4 = Mathf.Clamp(result28, entry.rangeMin, entry.rangeMax);
				}
				if (Mathf.Abs(num4 - num3) > 1E-06f)
				{
					SetMemberValue(entry.member, comp, num4);
				}
				GUILayout.Label(num4.ToString("0.###", CultureInfo.InvariantCulture), GUILayout.Width(70f));
			}
			else
			{
				string text3 = GUILayout.TextField(entry.editCache ?? GetValueString(comp, entry.member, typeof(float)), GUILayout.MinWidth(120f));
				if ((object)text3 != entry.editCache)
				{
					entry.editCache = text3;
				}
				if (float.TryParse(entry.editCache, NumberStyles.Float, CultureInfo.InvariantCulture, out var result29))
				{
					SetMemberValue(entry.member, comp, result29);
				}
			}
		}
		GUILayout.EndHorizontal();
	}

	private bool Foldout(bool state, string title)
	{
		return GUILayout.Toggle(state, (state ? "▼ " : "► ") + title, "Button");
	}

	private Texture2D MakeTex(Color c)
	{
		Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
		texture2D.SetPixel(0, 0, c);
		texture2D.Apply();
		return texture2D;
	}

	private void DrawAnimatorParams(Animator anim)
	{
		try
		{
			if (anim.runtimeAnimatorController == null)
			{
				GUILayout.Label("(No RuntimeAnimatorController)");
				return;
			}
			AnimatorControllerParameter[] parameters = anim.parameters;
			foreach (AnimatorControllerParameter animatorControllerParameter in parameters)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(animatorControllerParameter.name + " (" + animatorControllerParameter.type.ToString() + ")", GUILayout.Width(300f));
				switch (animatorControllerParameter.type)
				{
				case AnimatorControllerParameterType.Bool:
				{
					bool flag = anim.GetBool(animatorControllerParameter.nameHash);
					bool flag2 = GUILayout.Toggle(flag, flag ? "true" : "false", GUILayout.Width(100f));
					if (flag2 != flag)
					{
						anim.SetBool(animatorControllerParameter.nameHash, flag2);
					}
					break;
				}
				case AnimatorControllerParameterType.Float:
				{
					if (float.TryParse(GUILayout.TextField(anim.GetFloat(animatorControllerParameter.nameHash).ToString("0.###", CultureInfo.InvariantCulture), GUILayout.MinWidth(120f)), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
					{
						anim.SetFloat(animatorControllerParameter.nameHash, result);
					}
					break;
				}
				case AnimatorControllerParameterType.Int:
				{
					if (int.TryParse(GUILayout.TextField(anim.GetInteger(animatorControllerParameter.nameHash).ToString(CultureInfo.InvariantCulture), GUILayout.MinWidth(120f)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
					{
						anim.SetInteger(animatorControllerParameter.nameHash, result2);
					}
					break;
				}
				case AnimatorControllerParameterType.Trigger:
					if (GUILayout.Button("Trigger", GUILayout.Width(120f)))
					{
						anim.SetTrigger(animatorControllerParameter.nameHash);
					}
					break;
				}
				GUILayout.EndHorizontal();
			}
		}
		catch
		{
			GUILayout.Label("(Animator parameters could not be read)");
		}
	}

	private IEnumerator LoadAudioClipFromPath(MemberInfo member, Component comp, string path, Action<string> setStatus, Action<AudioClip> onLoaded)
	{
		setStatus?.Invoke("Loading...");
		string uri;
		try
		{
			uri = new Uri(path).AbsoluteUri;
		}
		catch
		{
			uri = "file:///" + path.Replace("\\", "/");
		}
		AudioType audioType = GuessAudioType(path);
		using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
		yield return req.SendWebRequest();
		if (req.result != UnityWebRequest.Result.Success)
		{
			setStatus?.Invoke("Failed");
			yield break;
		}
		AudioClip content = DownloadHandlerAudioClip.GetContent(req);
		if (content != null)
		{
			onLoaded?.Invoke(content);
		}
		setStatus?.Invoke((content != null) ? "Loaded" : "Failed");
	}

	private AudioType GuessAudioType(string path)
	{
		return Path.GetExtension(path)?.ToLowerInvariant() switch
		{
			".wav" => AudioType.WAV, 
			".ogg" => AudioType.OGGVORBIS, 
			".mp3" => AudioType.MPEG, 
			_ => AudioType.UNKNOWN, 
		};
	}

	private void SaveCurrentProfile()
	{
		ProfileFile profileFile = new ProfileFile();
		for (int i = 0; i < targets.Count; i++)
		{
			MEValueRuntimeEntry mEValueRuntimeEntry = targets[i];
			if (mEValueRuntimeEntry == null || mEValueRuntimeEntry.targetObject == null)
			{
				continue;
			}
			if (mEValueRuntimeEntry.comps == null || mEValueRuntimeEntry.compNames == null)
			{
				BuildComponentList(mEValueRuntimeEntry);
			}
			int num = 0;
			while (true)
			{
				int num2 = num;
				Component[] comps = mEValueRuntimeEntry.comps;
				if (num2 >= ((comps != null) ? comps.Length : 0))
				{
					break;
				}
				Component component = mEValueRuntimeEntry.comps[num];
				if ((bool)component)
				{
					if (!mEValueRuntimeEntry.membersPerComp.ContainsKey(component) || mEValueRuntimeEntry.membersPerComp[component] == null)
					{
						BuildMemberList(mEValueRuntimeEntry, component);
					}
					PerTarget perTarget = new PerTarget
					{
						targetIndex = i,
						compIndex = num,
						compTypeName = component.GetType().AssemblyQualifiedName
					};
					List<MemberEntry> list = mEValueRuntimeEntry.membersPerComp[component];
					for (int j = 0; j < list.Count; j++)
					{
						MemberEntry memberEntry = list[j];
						string memberName = ((memberEntry.member is FieldInfo fieldInfo) ? fieldInfo.Name : ((PropertyInfo)memberEntry.member).Name);
						MemberKV memberKV = new MemberKV
						{
							memberName = memberName,
							isProperty = (memberEntry.member is PropertyInfo)
						};
						if (memberEntry.isColor)
						{
							Color color = (Color)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "color";
							memberKV.rf = color.r;
							memberKV.gf = color.g;
							memberKV.bf = color.b;
							memberKV.af = color.a;
						}
						else if (memberEntry.isColor32)
						{
							Color32 color2 = (Color32)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "color32";
							memberKV.r8 = color2.r;
							memberKV.g8 = color2.g;
							memberKV.b8 = color2.b;
							memberKV.a8 = color2.a;
						}
						else if (memberEntry.isV2)
						{
							Vector2 vector = (Vector2)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "v2";
							memberKV.v2x = vector.x;
							memberKV.v2y = vector.y;
						}
						else if (memberEntry.isV3)
						{
							Vector3 vector2 = (Vector3)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "v3";
							memberKV.v3x = vector2.x;
							memberKV.v3y = vector2.y;
							memberKV.v3z = vector2.z;
						}
						else if (memberEntry.isV4)
						{
							Vector4 vector3 = (Vector4)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "v4";
							memberKV.v4x = vector3.x;
							memberKV.v4y = vector3.y;
							memberKV.v4z = vector3.z;
							memberKV.v4w = vector3.w;
						}
						else if (memberEntry.isQuat)
						{
							Quaternion quaternion = (Quaternion)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "quat";
							memberKV.qx = quaternion.x;
							memberKV.qy = quaternion.y;
							memberKV.qz = quaternion.z;
							memberKV.qw = quaternion.w;
						}
						else if (memberEntry.isRect)
						{
							Rect rect = (Rect)GetMemberValue(memberEntry.member, component);
							memberKV.kind = "rect";
							memberKV.rx = rect.x;
							memberKV.ry = rect.y;
							memberKV.rw = rect.width;
							memberKV.rh = rect.height;
						}
						else if (memberEntry.isEnum)
						{
							memberKV.kind = "enum";
							memberKV.enumType = memberEntry.type.AssemblyQualifiedName;
							object memberValue = GetMemberValue(memberEntry.member, component);
							memberKV.enumName = ((memberValue != null) ? memberValue.ToString() : memberEntry.enumNames[Mathf.Clamp(memberEntry.enumIndex, 0, memberEntry.enumNames.Length - 1)]);
						}
						else if (memberEntry.isAudioClip)
						{
							memberKV.kind = "audioclip";
							memberKV.audioPath = memberEntry.audioPathCache ?? "";
						}
						else if (memberEntry.type == typeof(string))
						{
							memberKV.kind = "string";
							memberKV.s = ((string)GetMemberValue(memberEntry.member, component)) ?? "";
						}
						else if (memberEntry.type == typeof(float))
						{
							memberKV.kind = "float";
							memberKV.f = (float)GetMemberValue(memberEntry.member, component);
						}
						else if (memberEntry.type == typeof(int))
						{
							memberKV.kind = "int";
							memberKV.i = (int)GetMemberValue(memberEntry.member, component);
						}
						else if (memberEntry.type == typeof(bool))
						{
							memberKV.kind = "bool";
							memberKV.b = (bool)GetMemberValue(memberEntry.member, component);
						}
						perTarget.members.Add(memberKV);
					}
					profileFile.targets.Add(perTarget);
				}
				num++;
			}
		}
		string baseDir = GetBaseDir();
		if (!Directory.Exists(baseDir))
		{
			Directory.CreateDirectory(baseDir);
		}
		string text = Path.Combine(baseDir, SanitizeFileName(profileName) + ".json");
		string contents = JsonUtility.ToJson(profileFile, prettyPrint: true);
		File.WriteAllText(text, contents);
		ShowStatus("Saved: " + text);
	}

	private void LoadProfile(string name, bool showResult)
	{
		string text = Path.Combine(GetBaseDir(), SanitizeFileName(name) + ".json");
		if (!File.Exists(text))
		{
			if (showResult)
			{
				ShowStatus("Profile not found: " + text);
			}
			return;
		}
		ProfileFile profileFile = JsonUtility.FromJson<ProfileFile>(File.ReadAllText(text));
		if (profileFile == null || profileFile.targets == null)
		{
			if (showResult)
			{
				ShowStatus("Invalid profile: " + text);
			}
			return;
		}
		for (int i = 0; i < profileFile.targets.Count; i++)
		{
			PerTarget perTarget = profileFile.targets[i];
			if (perTarget.targetIndex < 0 || perTarget.targetIndex >= targets.Count)
			{
				continue;
			}
			MEValueRuntimeEntry mEValueRuntimeEntry = targets[perTarget.targetIndex];
			if (mEValueRuntimeEntry == null || mEValueRuntimeEntry.targetObject == null)
			{
				continue;
			}
			if (mEValueRuntimeEntry.comps == null || mEValueRuntimeEntry.compNames == null)
			{
				BuildComponentList(mEValueRuntimeEntry);
			}
			int num = perTarget.compIndex;
			if (num >= 0)
			{
				int num2 = num;
				Component[] comps = mEValueRuntimeEntry.comps;
				if (num2 < ((comps != null) ? comps.Length : 0))
				{
					goto IL_0170;
				}
			}
			num = -1;
			int num3 = 0;
			while (true)
			{
				int num4 = num3;
				Component[] comps2 = mEValueRuntimeEntry.comps;
				if (num4 >= ((comps2 != null) ? comps2.Length : 0))
				{
					break;
				}
				if ((mEValueRuntimeEntry.comps[num3] ? mEValueRuntimeEntry.comps[num3].GetType().AssemblyQualifiedName : "") == perTarget.compTypeName)
				{
					num = num3;
					break;
				}
				num3++;
			}
			goto IL_0170;
			IL_0170:
			if (num < 0)
			{
				continue;
			}
			int num5 = num;
			Component[] comps3 = mEValueRuntimeEntry.comps;
			if (num5 >= ((comps3 != null) ? comps3.Length : 0))
			{
				continue;
			}
			Component comp = mEValueRuntimeEntry.comps[num];
			if (!mEValueRuntimeEntry.membersPerComp.ContainsKey(comp) || mEValueRuntimeEntry.membersPerComp[comp] == null)
			{
				BuildMemberList(mEValueRuntimeEntry, comp);
			}
			List<MemberEntry> list = mEValueRuntimeEntry.membersPerComp[comp];
			for (int j = 0; j < perTarget.members.Count; j++)
			{
				MemberKV memberKV = perTarget.members[j];
				MemberEntry match = null;
				if (list != null)
				{
					for (int k = 0; k < list.Count; k++)
					{
						MemberEntry memberEntry = list[k];
						if (((memberEntry.member is FieldInfo fieldInfo) ? fieldInfo.Name : ((PropertyInfo)memberEntry.member).Name) == memberKV.memberName)
						{
							match = memberEntry;
							break;
						}
					}
				}
				if (match == null)
				{
					continue;
				}
				if (memberKV.kind == "color" && match.isColor)
				{
					SetMemberValue(value: new Color(memberKV.rf, memberKV.gf, memberKV.bf, memberKV.af), m: match.member, c: comp);
					match.cr = memberKV.rf.ToString("0.###", CultureInfo.InvariantCulture);
					match.cg = memberKV.gf.ToString("0.###", CultureInfo.InvariantCulture);
					match.cb = memberKV.bf.ToString("0.###", CultureInfo.InvariantCulture);
					match.ca = memberKV.af.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "color32" && match.isColor32)
				{
					SetMemberValue(value: new Color32((byte)memberKV.r8, (byte)memberKV.g8, (byte)memberKV.b8, (byte)memberKV.a8), m: match.member, c: comp);
					match.cr = memberKV.r8.ToString(CultureInfo.InvariantCulture);
					match.cg = memberKV.g8.ToString(CultureInfo.InvariantCulture);
					match.cb = memberKV.b8.ToString(CultureInfo.InvariantCulture);
					match.ca = memberKV.a8.ToString(CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "v2" && match.isV2)
				{
					SetMemberValue(value: new Vector2(memberKV.v2x, memberKV.v2y), m: match.member, c: comp);
					match.v2x = memberKV.v2x.ToString("0.###", CultureInfo.InvariantCulture);
					match.v2y = memberKV.v2y.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "v3" && match.isV3)
				{
					SetMemberValue(value: new Vector3(memberKV.v3x, memberKV.v3y, memberKV.v3z), m: match.member, c: comp);
					match.v3x = memberKV.v3x.ToString("0.###", CultureInfo.InvariantCulture);
					match.v3y = memberKV.v3y.ToString("0.###", CultureInfo.InvariantCulture);
					match.v3z = memberKV.v3z.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "v4" && match.isV4)
				{
					SetMemberValue(value: new Vector4(memberKV.v4x, memberKV.v4y, memberKV.v4z, memberKV.v4w), m: match.member, c: comp);
					match.v4x = memberKV.v4x.ToString("0.###", CultureInfo.InvariantCulture);
					match.v4y = memberKV.v4y.ToString("0.###", CultureInfo.InvariantCulture);
					match.v4z = memberKV.v4z.ToString("0.###", CultureInfo.InvariantCulture);
					match.v4w = memberKV.v4w.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "quat" && match.isQuat)
				{
					SetMemberValue(value: new Quaternion(memberKV.qx, memberKV.qy, memberKV.qz, memberKV.qw), m: match.member, c: comp);
					match.qx = memberKV.qx.ToString("0.###", CultureInfo.InvariantCulture);
					match.qy = memberKV.qy.ToString("0.###", CultureInfo.InvariantCulture);
					match.qz = memberKV.qz.ToString("0.###", CultureInfo.InvariantCulture);
					match.qw = memberKV.qw.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "rect" && match.isRect)
				{
					SetMemberValue(value: new Rect(memberKV.rx, memberKV.ry, memberKV.rw, memberKV.rh), m: match.member, c: comp);
					match.rx = memberKV.rx.ToString("0.###", CultureInfo.InvariantCulture);
					match.ry = memberKV.ry.ToString("0.###", CultureInfo.InvariantCulture);
					match.rw = memberKV.rw.ToString("0.###", CultureInfo.InvariantCulture);
					match.rh = memberKV.rh.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "enum" && match.isEnum)
				{
					try
					{
						Type type = Type.GetType(memberKV.enumType, throwOnError: false);
						if (type != null)
						{
							object value = Enum.Parse(type, memberKV.enumName);
							SetMemberValue(match.member, comp, value);
							match.enumIndex = Array.IndexOf(match.enumValues, value);
							if (match.enumIndex < 0)
							{
								match.enumIndex = 0;
							}
						}
					}
					catch
					{
					}
				}
				else if (memberKV.kind == "audioclip" && match.isAudioClip)
				{
					match.audioPathCache = memberKV.audioPath ?? "";
					if (string.IsNullOrWhiteSpace(match.audioPathCache))
					{
						continue;
					}
					StartCoroutine(LoadAudioClipFromPath(match.member, comp, match.audioPathCache, delegate(string s)
					{
						match.audioStatus = s;
					}, delegate(AudioClip clipOk)
					{
						if (clipOk != null)
						{
							SetMemberValue(match.member, comp, clipOk);
						}
					}));
				}
				else if (memberKV.kind == "string" && match.type == typeof(string))
				{
					SetMemberValue(match.member, comp, memberKV.s ?? "");
					match.editCache = memberKV.s ?? "";
				}
				else if (memberKV.kind == "float" && match.type == typeof(float))
				{
					SetMemberValue(match.member, comp, memberKV.f);
					match.editCache = memberKV.f.ToString("0.###", CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "int" && match.type == typeof(int))
				{
					SetMemberValue(match.member, comp, memberKV.i);
					match.editCache = memberKV.i.ToString(CultureInfo.InvariantCulture);
				}
				else if (memberKV.kind == "bool" && match.type == typeof(bool))
				{
					SetMemberValue(match.member, comp, memberKV.b);
					match.editCache = (memberKV.b ? "true" : "false");
				}
			}
		}
		if (showResult)
		{
			ShowStatus("Loaded: " + text);
		}
	}

	private string SanitizeFileName(string s)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			s = s.Replace(oldChar, '_');
		}
		if (string.IsNullOrWhiteSpace(s))
		{
			s = "Profile";
		}
		return s;
	}

	private string GetBaseDir()
	{
		return Path.Combine(Application.persistentDataPath, "MEValueChanger");
	}

	private string GetSettingsPath()
	{
		return Path.Combine(GetBaseDir(), "settings.json");
	}

	private void LoadSettings()
	{
		try
		{
			string settingsPath = GetSettingsPath();
			if (!File.Exists(settingsPath))
			{
				return;
			}
			SettingsData settingsData = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));
			if (settingsData != null)
			{
				if (!string.IsNullOrEmpty(settingsData.profileName))
				{
					profileName = settingsData.profileName;
				}
				autoLoadOnStart = settingsData.autoLoadOnStart;
			}
		}
		catch
		{
		}
	}

	private void SaveSettings()
	{
		try
		{
			string baseDir = GetBaseDir();
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			string settingsPath = GetSettingsPath();
			string contents = JsonUtility.ToJson(new SettingsData
			{
				profileName = profileName,
				autoLoadOnStart = autoLoadOnStart
			}, prettyPrint: true);
			File.WriteAllText(settingsPath, contents);
			ShowStatus("Settings saved");
		}
		catch
		{
		}
	}

	private void ShowStatus(string msg)
	{
		status = msg;
		statusUntil = Time.realtimeSinceStartup + 3f;
	}

	private void ApplyDeactivateWhileOpen(bool visible)
	{
		if (visible)
		{
			prevActive.Clear();
			for (int i = 0; i < deactivateWhileOpen.Count; i++)
			{
				GameObject gameObject = deactivateWhileOpen[i];
				if (!(gameObject == null))
				{
					prevActive[gameObject] = gameObject.activeSelf;
					gameObject.SetActive(value: false);
				}
			}
			return;
		}
		foreach (KeyValuePair<GameObject, bool> item in prevActive)
		{
			if (item.Key != null)
			{
				item.Key.SetActive(item.Value);
			}
		}
		prevActive.Clear();
	}
}
