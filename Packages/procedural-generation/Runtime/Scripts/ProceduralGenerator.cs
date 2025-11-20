using UnityEngine;

namespace Nianyi.UnityPack
{
	[ExecuteAlways]
	public abstract class ProceduralGenerator : MonoBehaviour
	{
		#region Unity life cycle
		protected void Awake()
		{
#if UNITY_EDITOR
			if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Generate();
				return;
			}
#endif
			Ungarrison();
		}

		protected void OnDestroy()
		{
#if UNITY_EDITOR
			if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
			{
				DestroyGeneration();
				return;
			}
#endif
		}

#if UNITY_EDITOR
		protected void OnValidate()
		{
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
					return;
				Generate();
			};
		}
#endif
		#endregion

		#region Generation
		public void Ungarrison()
		{
			HierarchyUtility.Destroy(this);
		}

		public abstract void DestroyGeneration();
		public abstract void Generate();
		#endregion
	}
}
