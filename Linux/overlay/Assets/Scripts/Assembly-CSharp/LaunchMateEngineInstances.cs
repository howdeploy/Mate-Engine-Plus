using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LaunchMateEngineInstances : MonoBehaviour
{
	[Header("Executable")]
	public string executableName = "MateEngineX.exe";

	[Header("Texts")]
	public string notRunningText = "Open Avatar {0}";

	public string runningText = "Avatar {0} Running";

	[Header("Instances")]
	public List<InstanceEntry> instances = new List<InstanceEntry>(5);

	[Header("Status Polling")]
	public float statusPollInterval = 1.5f;

	[Header("Optional")]
	public GameObject hideIfSecondary;

	public List<GameObject> hideIfSecondaryItems = new List<GameObject>();

	private readonly Dictionary<int, Process> activeInstances = new Dictionary<int, Process>();

	private string persistentPath;

	private int currentInstanceIndex;

	private string pidPath;

	private void Awake()
	{
		persistentPath = Application.persistentDataPath;
		DetectCurrentInstance();
		if (currentInstanceIndex > 0)
		{
			ApplySecondaryHide();
		}
		for (int i = 0; i < instances.Count; i++)
		{
			int num = i + 1;
			if (instances[i] != null && instances[i].button != null)
			{
				int captured = num;
				instances[i].button.onClick.AddListener(delegate
				{
					LaunchInstance(captured);
				});
			}
			UpdateButtonText(num, IsInstanceAlive(num));
		}
		if (statusPollInterval > 0f)
		{
			InvokeRepeating("RefreshStatusPoll", statusPollInterval, statusPollInterval);
		}
		if (currentInstanceIndex > 0)
		{
			WritePidFile();
		}
	}

	private void OnApplicationQuit()
	{
		CleanupPidFile();
	}

	private void OnDestroy()
	{
		CleanupPidFile();
	}

	private void DetectCurrentInstance()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length - 1; i++)
		{
			if (commandLineArgs[i].Equals("--instance", StringComparison.OrdinalIgnoreCase))
			{
				int.TryParse(commandLineArgs[i + 1], out currentInstanceIndex);
				break;
			}
		}
		currentInstanceIndex = Mathf.Max(0, currentInstanceIndex);
	}

	private void ApplySecondaryHide()
	{
		foreach (GameObject hideTarget in GetHideTargets())
		{
			if (hideTarget != null)
			{
				hideTarget.SetActive(value: false);
			}
		}
	}

	private List<GameObject> GetHideTargets()
	{
		List<GameObject> list = new List<GameObject>();
		if (hideIfSecondary != null)
		{
			list.Add(hideIfSecondary);
		}
		if (hideIfSecondaryItems != null)
		{
			for (int i = 0; i < hideIfSecondaryItems.Count; i++)
			{
				GameObject gameObject = hideIfSecondaryItems[i];
				if (gameObject != null && !list.Contains(gameObject))
				{
					list.Add(gameObject);
				}
			}
		}
		return list;
	}

	private void WritePidFile()
	{
		try
		{
			pidPath = Path.Combine(persistentPath, $"instance_{currentInstanceIndex}.pid");
			int id = Process.GetCurrentProcess().Id;
			File.WriteAllText(pidPath, id.ToString());
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Launcher] Failed to write PID file: " + ex.Message);
		}
	}

	private void CleanupPidFile()
	{
		if (string.IsNullOrEmpty(pidPath))
		{
			return;
		}
		try
		{
			if (File.Exists(pidPath))
			{
				File.Delete(pidPath);
			}
		}
		catch
		{
		}
	}

	public void LaunchInstance(int index)
	{
		if (IsInstanceAlive(index))
		{
			Debug.Log($"[Launcher] Instance {index} already running.");
			UpdateButtonText(index, running: true);
			return;
		}
		string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../launch.sh"));
		if (!File.Exists(fullPath))
		{
			Debug.LogError("[Launcher] Executable not found: " + fullPath);
			return;
		}
		string arg = $"settings_instance{index}.json";
		string arg2 = $"Instance_{index}";
		string text = $"--instance {index} --savefile \"{arg}\" --datadir \"{arg2}\"";
		try
		{
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "/bin/bash",
				Arguments = "\"" + fullPath.Replace("\"", "\\\"") + "\" " + text,
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(fullPath)
			});
			if (process != null)
			{
				activeInstances[index] = process;
				process.EnableRaisingEvents = true;
				process.Exited += delegate
				{
					UnityMainThreadDispatcher.Enqueue(delegate
					{
						UpdateButtonText(index, running: false);
					});
					activeInstances.Remove(index);
				};
			}
			UpdateButtonText(index, running: true);
			Debug.Log($"[Launcher] Started Instance {index} with args: {text}");
		}
		catch (Exception ex)
		{
			Debug.LogError("[Launcher] Failed to start instance " + index + ": " + ex.Message);
		}
	}

	private void RefreshStatusPoll()
	{
		for (int i = 1; i <= instances.Count; i++)
		{
			UpdateButtonText(i, IsInstanceAlive(i));
		}
	}

	private bool IsInstanceAlive(int index)
	{
		if (activeInstances.TryGetValue(index, out var value) && value != null && !value.HasExited)
		{
			return true;
		}
		string path = Path.Combine(persistentPath, $"instance_{index}.pid");
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			if (int.TryParse(File.ReadAllText(path).Trim(), out var result))
			{
				try
				{
					if (!Process.GetProcessById(result).HasExited)
					{
						return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			File.Delete(path);
		}
		catch
		{
		}
		return false;
	}

	private void UpdateButtonText(int index, bool running)
	{
		int num = index - 1;
		if (num >= 0 && num < instances.Count)
		{
			InstanceEntry instanceEntry = instances[num];
			if (instanceEntry != null && !(instanceEntry.text == null))
			{
				instanceEntry.text.text = (running ? string.Format(runningText, index) : string.Format(notRunningText, index));
			}
		}
	}
}
