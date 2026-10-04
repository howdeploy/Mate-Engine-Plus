using System;
using System.Collections.Generic;

[Serializable]
public class PerTarget
{
	public int targetIndex;

	public int compIndex;

	public string compTypeName;

	public List<MemberKV> members = new List<MemberKV>();
}
