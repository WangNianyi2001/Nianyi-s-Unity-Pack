using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nianyi.UnityPack
{
	public static class AssetUtility
	{
		/// <remarks>Only works in editor.</remarks>
		public static bool IsAsset(this Object target)
		{
			if(target == null)
				return false;
#if !UNITY_EDITOR
			return false;
#else
			return AssetDatabase.Contains(target);
#endif
		}

		public static void Destroy(this Object target)
		{
			if(target.IsAsset())
			{
				Debug.LogWarning($"Attempting to destroy asset {target}, aborting.", target);
				return;
			}
			if(SceneUtility.IsPlaying)
				Object.Destroy(target);
			else
				Object.DestroyImmediate(target);
		}
	}
}
