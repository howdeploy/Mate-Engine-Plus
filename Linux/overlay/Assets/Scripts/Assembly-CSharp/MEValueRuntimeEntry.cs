using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MEValueRuntimeEntry
{
	public GameObject targetObject;

	[NonSerialized]
	public bool foldout;

	[NonSerialized]
	public Component[] comps;

	[NonSerialized]
	public string[] compNames;

	[NonSerialized]
	public Dictionary<Component, List<MemberEntry>> membersPerComp = new Dictionary<Component, List<MemberEntry>>();

	[NonSerialized]
	public Dictionary<Component, bool> compFoldouts = new Dictionary<Component, bool>();

	[NonSerialized]
	public Vector2 scrollMembers;

	[NonSerialized]
	public bool showAnimatorParams;
}
