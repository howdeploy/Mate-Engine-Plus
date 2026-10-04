using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kirurobo
{
	public class SampleManager : MonoBehaviour
	{
		private static SampleManager _instance;

		public Canvas canvas;

		public static SampleManager Instance
		{
			get
			{
				object obj = _instance;
				if (obj == null)
				{
					obj = Object.FindAnyObjectByType<SampleManager>() ?? new SampleManager();
					_instance = (SampleManager)obj;
				}
				return (SampleManager)obj;
			}
		}

		private void Awake()
		{
			if (this != Instance)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			Object.DontDestroyOnLoad(Instance);
			Object.DontDestroyOnLoad(UniWindowController.current);
			SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		}

		private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			UniWindowController.current.SetCamera(Camera.main);
		}

		public void LoadScene(string name)
		{
			if (name == "SimpleSample")
			{
				UniWindowController.current.isTransparent = true;
			}
			else if (name == "FullScreenSample")
			{
				UniWindowController.current.shouldFitMonitor = true;
			}
			SceneManager.LoadScene(name);
		}

		public void Quit()
		{
			Application.Quit();
		}
	}
}
