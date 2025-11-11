using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nianyi.UnityPack
{
	public static class HierarchyUtility
	{
		#region Existence
		/// <remarks>Will create a prefab instance if <c>template</c> is a prefab asset and in edit mode.</remarks>
		public static GameObject Instantiate(GameObject template, Transform under = null)
		{
#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				if(PrefabUtility.IsPartOfPrefabAsset(template))
					return PrefabUtility.InstantiatePrefab(template, under) as GameObject;
			}
#endif
			return Object.Instantiate(template, under);
		}

		/// <remarks>Cannot delete assets.</remarks>
		public static void Destroy(GameObject target)
		{
#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				Object.DestroyImmediate(target, false);
				return;
			}
#endif
			Object.Destroy(target);
		}
		#endregion

		#region Genealogy
		public static bool IsAncestorOf(this Transform target, Transform reference)
		{
			if(target == null)
				return true;
			if(reference == null)
				return false;
			return reference.IsChildOf(target);
		}

		public static bool IsDescendantOf(this Transform target, Transform reference)
		{
			if(target == null)
				return false;
			if(reference == null)
				return true;
			return target.IsChildOf(reference);
		}

		public static Transform[] GetAncestorChain(this Transform target, bool bottomToTop = true)
		{
			List<Transform> chain = new();
			for(; target != null; target = target.parent)
				chain.Add(target);
			if(!bottomToTop)
				chain.Reverse();
			return chain.ToArray();
		}

		public static Transform[] GetDirectChildren(this Transform parent)
		{
			Transform[] children = new Transform[parent.childCount];
			for(int i = 0; i < parent.childCount; ++i)
				children[i] = parent.GetChild(i);
			return children;
		}
		#endregion

		#region Components
		public static bool EnsureComponent<T>(this GameObject target, out T component) where T : Component
		{
			if(target.TryGetComponent(out component))
				return false;
			component = target.AddComponent<T>();
			return true;
		}

		public static bool RemoveComponent<T>(this GameObject target) where T : Component
		{
			if(!target.TryGetComponent<T>(out var component))
				return false;
			Object.Destroy(component);
			return true;
		}

		public static bool RemoveComponent<T>(this GameObject target, ref T component, bool force = true) where T : Component
		{
			if(!component || component.gameObject != target)
			{
				if(force)
					component = target.GetComponent<T>();
				if(!component)
					return false;
			}
			Object.Destroy(component);
			component = null;
			return true;
		}
		#endregion
	}
}
