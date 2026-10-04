using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ThemeManager : MonoBehaviour
{
	public static ThemeManager Instance;

	[Range(0f, 1f)]
	public float hue;

	[Range(0f, 2f)]
	public float saturation = 1f;

	public List<Material> materials = new List<Material>();

	public List<ParticleSystem> particleSystems = new List<ParticleSystem>();

	public bool revertOnExitPlayMode = true;

	private readonly Dictionary<Material, Color> baseColor = new Dictionary<Material, Color>();

	private readonly Dictionary<Material, Color> baseMainColor = new Dictionary<Material, Color>();

	private readonly Dictionary<Material, Color> baseOverlayColor = new Dictionary<Material, Color>();

	private readonly Dictionary<ParticleSystem, Color> baseParticleColor = new Dictionary<ParticleSystem, Color>();

	private float lastHue = -1f;

	private float lastSat = -1f;

	private bool runtimeActive;

	private void OnEnable()
	{
		Instance = this;
		if (Application.isPlaying)
		{
			CaptureBaseProps();
			runtimeActive = true;
			Apply();
		}
	}

	private void OnDisable()
	{
		if (runtimeActive && revertOnExitPlayMode)
		{
			RestoreAll();
		}
		if (Instance == this)
		{
			Instance = null;
		}
		runtimeActive = false;
		lastHue = -1f;
		lastSat = -1f;
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			Apply();
		}
	}

	public void SetHue(float value)
	{
		hue = Mathf.Repeat(value, 1f);
		if (Application.isPlaying)
		{
			Apply();
		}
	}

	public void SetSaturation(float value)
	{
		saturation = Mathf.Clamp(value, 0f, 2f);
		if (Application.isPlaying)
		{
			Apply();
		}
	}

	public void RegisterMaterial(Material mat)
	{
		if (!(mat == null))
		{
			if (!materials.Contains(mat))
			{
				materials.Add(mat);
			}
			if (Application.isPlaying)
			{
				CacheMaterial(mat);
				ApplyTo(mat);
			}
		}
	}

	public void UnregisterMaterial(Material mat)
	{
		if (!(mat == null))
		{
			materials.Remove(mat);
			baseColor.Remove(mat);
			baseMainColor.Remove(mat);
			baseOverlayColor.Remove(mat);
		}
	}

	public void RegisterParticle(ParticleSystem ps)
	{
		if (!(ps == null))
		{
			if (!particleSystems.Contains(ps))
			{
				particleSystems.Add(ps);
			}
			if (Application.isPlaying)
			{
				CacheParticle(ps);
				ApplyTo(ps);
			}
		}
	}

	public void UnregisterParticle(ParticleSystem ps)
	{
		if (!(ps == null))
		{
			particleSystems.Remove(ps);
			baseParticleColor.Remove(ps);
		}
	}

	public void CaptureBaseProps()
	{
		baseColor.Clear();
		baseMainColor.Clear();
		baseOverlayColor.Clear();
		baseParticleColor.Clear();
		for (int i = 0; i < materials.Count; i++)
		{
			CacheMaterial(materials[i]);
		}
		for (int j = 0; j < particleSystems.Count; j++)
		{
			CacheParticle(particleSystems[j]);
		}
	}

	private void CacheMaterial(Material m)
	{
		if (!(m == null))
		{
			if (m.HasProperty("_Color") && !baseColor.ContainsKey(m))
			{
				baseColor[m] = m.GetColor("_Color");
			}
			if (m.HasProperty("_MainColor") && !baseMainColor.ContainsKey(m))
			{
				baseMainColor[m] = m.GetColor("_MainColor");
			}
			if (m.HasProperty("_OverlayColor") && !baseOverlayColor.ContainsKey(m))
			{
				baseOverlayColor[m] = m.GetColor("_OverlayColor");
			}
		}
	}

	private void CacheParticle(ParticleSystem ps)
	{
		if (!(ps == null) && !baseParticleColor.ContainsKey(ps))
		{
			baseParticleColor[ps] = ps.main.startColor.color;
		}
	}

	public void Apply()
	{
		if (Application.isPlaying && (!Mathf.Approximately(lastHue, hue) || !Mathf.Approximately(lastSat, saturation)))
		{
			for (int i = 0; i < materials.Count; i++)
			{
				ApplyTo(materials[i]);
			}
			for (int j = 0; j < particleSystems.Count; j++)
			{
				ApplyTo(particleSystems[j]);
			}
			lastHue = hue;
			lastSat = saturation;
		}
	}

	private void ApplyTo(Material m)
	{
		if (!(m == null))
		{
			if (m.HasProperty("_Color") && baseColor.TryGetValue(m, out var value))
			{
				m.SetColor("_Color", Adjust(value));
			}
			if (m.HasProperty("_MainColor") && baseMainColor.TryGetValue(m, out var value2))
			{
				m.SetColor("_MainColor", Adjust(value2));
			}
			if (m.HasProperty("_OverlayColor") && baseOverlayColor.TryGetValue(m, out var value3))
			{
				m.SetColor("_OverlayColor", Adjust(value3));
			}
		}
	}

	private void ApplyTo(ParticleSystem ps)
	{
		if (!(ps == null) && baseParticleColor.TryGetValue(ps, out var value))
		{
			ParticleSystem.MainModule main = ps.main;
			ParticleSystem.MinMaxGradient startColor = main.startColor;
			startColor.color = Adjust(value);
			main.startColor = startColor;
		}
	}

	private Color Adjust(Color src)
	{
		Color.RGBToHSV(src, out var H, out var S, out var V);
		H = (H + hue) % 1f;
		S = Mathf.Clamp01(S * saturation);
		Color result = Color.HSVToRGB(H, S, V);
		result.a = src.a;
		return result;
	}

	public void RestoreAll()
	{
		for (int i = 0; i < materials.Count; i++)
		{
			Material material = materials[i];
			if (!(material == null))
			{
				if (material.HasProperty("_Color") && baseColor.TryGetValue(material, out var value))
				{
					material.SetColor("_Color", value);
				}
				if (material.HasProperty("_MainColor") && baseMainColor.TryGetValue(material, out var value2))
				{
					material.SetColor("_MainColor", value2);
				}
				if (material.HasProperty("_OverlayColor") && baseOverlayColor.TryGetValue(material, out var value3))
				{
					material.SetColor("_OverlayColor", value3);
				}
			}
		}
		for (int j = 0; j < particleSystems.Count; j++)
		{
			ParticleSystem particleSystem = particleSystems[j];
			if (!(particleSystem == null) && baseParticleColor.TryGetValue(particleSystem, out var value4))
			{
				ParticleSystem.MainModule main = particleSystem.main;
				ParticleSystem.MinMaxGradient startColor = main.startColor;
				startColor.color = value4;
				main.startColor = startColor;
			}
		}
	}
}
