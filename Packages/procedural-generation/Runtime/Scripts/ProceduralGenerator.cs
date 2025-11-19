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
				UpdateGeneration();
				return;
			}
#endif
			UpdateGeneration();
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
			UnityEditor.EditorApplication.delayCall += UpdateGeneration;
		}
#endif
		#endregion

		#region Generation
		public void Ungarrison()
		{
			HierarchyUtility.Destroy(this);
		}

		public abstract void NewGeneration();
		public abstract void DestroyGeneration();
		/// <summary>
		/// Should replace with incremental implementation in child classes.
		/// </summary>
		public virtual void UpdateGeneration()
		{
			DestroyGeneration();
			NewGeneration();
		}
		#endregion
	}
}
