using System;
using System.Reflection;

public class MemberEntry
{
	public MemberInfo member;

	public string displayName;

	public Type type;

	public string editCache;

	public bool isColor;

	public bool isColor32;

	public bool isEnum;

	public bool isAudioClip;

	public bool isV2;

	public bool isV3;

	public bool isV4;

	public bool isQuat;

	public bool isRect;

	public string cr;

	public string cg;

	public string cb;

	public string ca;

	public string v2x;

	public string v2y;

	public string v3x;

	public string v3y;

	public string v3z;

	public string v4x;

	public string v4y;

	public string v4z;

	public string v4w;

	public string qx;

	public string qy;

	public string qz;

	public string qw;

	public string rx;

	public string ry;

	public string rw;

	public string rh;

	public string[] enumNames;

	public Array enumValues;

	public int enumIndex;

	public bool hasRange;

	public float rangeMin;

	public float rangeMax;

	public string audioPathCache;

	public bool audioLoading;

	public string audioStatus;
}
