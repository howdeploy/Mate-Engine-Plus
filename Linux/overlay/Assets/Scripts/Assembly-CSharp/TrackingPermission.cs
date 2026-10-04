using System;

[Serializable]
public class TrackingPermission
{
	public string stateOrParameterName;

	public bool isParameter;

	public bool allowHead = true;

	public bool allowSpine = true;

	public bool allowEye = true;
}
