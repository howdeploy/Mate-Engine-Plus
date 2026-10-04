using UniGLTF;
using UnityEngine;

public sealed class GltfInstanceDisposer : MonoBehaviour
{
	private RuntimeGltfInstance inst;

	public void Bind(RuntimeGltfInstance i)
	{
		inst = i;
	}

	private void OnDestroy()
	{
		try
		{
			inst?.Dispose();
		}
		catch
		{
		}
	}
}
