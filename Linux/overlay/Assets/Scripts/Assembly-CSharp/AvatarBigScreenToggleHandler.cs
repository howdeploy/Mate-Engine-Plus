using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AvatarBigScreenToggleHandler : MonoBehaviour
{
	public List<BigScreenToggleSetting> settings = new List<BigScreenToggleSetting>();

	private AvatarBigScreenHandler bigScreenHandler;

	private Dictionary<Behaviour, bool> wasEnabledBefore = new Dictionary<Behaviour, bool>();

	private void Awake()
	{
		bigScreenHandler = GetComponent<AvatarBigScreenHandler>();
	}

	private void Update()
	{
		if (!bigScreenHandler)
		{
			return;
		}
		bool flag = false;
		FieldInfo field = typeof(AvatarBigScreenHandler).GetField("isBigScreenActive", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field != null)
		{
			flag = (bool)field.GetValue(bigScreenHandler);
		}
		Behaviour[] components = GetComponents<Behaviour>();
		foreach (Behaviour b in components)
		{
			if (b == this || b == bigScreenHandler)
			{
				continue;
			}
			bool flag2 = settings.Exists((BigScreenToggleSetting s) => s.componentTypeName == b.GetType().FullName && s.disableInBigScreen);
			if (flag && flag2)
			{
				if (!wasEnabledBefore.ContainsKey(b))
				{
					wasEnabledBefore[b] = b.enabled;
					b.enabled = false;
				}
			}
			else if (!flag && wasEnabledBefore.ContainsKey(b))
			{
				b.enabled = wasEnabledBefore[b];
			}
		}
		if (!flag)
		{
			wasEnabledBefore.Clear();
		}
	}
}
