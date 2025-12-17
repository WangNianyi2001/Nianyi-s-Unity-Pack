using UnityEngine;
using System.Collections;

namespace Nianyi.UnityPack
{
	[ExecuteAlways]
	public abstract class ProceduralGenerator : MonoBehaviour
	{
		#region Unity life cycle
		protected void Awake()
		{
			if(!hasInitialized)
				OnCreated();
		}

		protected void OnDestroy()
		{
			OnDestroyed();
		}

#if UNITY_EDITOR
		protected void OnValidate()
		{
			OnModified(true);  // Diff is guaranteed.
		}

		protected void Reset()
		{
			isUngarrisoning = false;
			OnDestroyed();
			hasInitialized = false;
			OnCreated();
		}
#endif

		protected void Update()
		{
			// Do not actively check for diffs in runtime.
			if(Application.isPlaying)
				return;

			OnModified();
		}
		#endregion

		#region Life cycle
		[SerializeField, HideInInspector] bool hasInitialized = false;
		bool isUngarrisoning = false;

		void OnCreated()
		{
			if(hasInitialized)
				return;
			NewGeneration();
			hasInitialized = true;
		}

		void OnDestroyed()
		{
			if(!isUngarrisoning)
				DestroyGeneration();
		}

		void OnModified(bool forceUpdate = false)
		{
			if(forceUpdate || CheckDiff())
				RequestUpdateGeneration();
		}

		public void RequestUpdateGeneration()
		{
#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				UnityEditor.EditorApplication.delayCall += UpdateGeneration_Safe;
				return;
			}
#endif
			StartCoroutine(DelayedUpdateGenerationCoroutine());
		}

		IEnumerator DelayedUpdateGenerationCoroutine()
		{
			yield return new WaitForEndOfFrame();
			UpdateGeneration_Safe();
		}

		void UpdateGeneration_Safe()
		{
			if(this == null)
				return;
			UpdateGeneration();
		}

		public void Ungarrison()
		{
			isUngarrisoning = true;
			HierarchyUtility.Destroy(this);
		}
		#endregion

		#region Interfaces
		public abstract void NewGeneration();

		public abstract void UpdateGeneration();

		public abstract void DestroyGeneration();

		public virtual bool CheckDiff()
		{
			return true;
		}
		#endregion
	}
}
