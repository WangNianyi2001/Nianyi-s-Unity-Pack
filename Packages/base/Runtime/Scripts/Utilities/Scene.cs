using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public static class Scene
	{
		#region Editor
		public static SceneMode CurrentMode
		{
			get
			{
#if !UNITY_EDITOR
			return SceneMode.Play;
#else
				if(Application.isPlaying)
					return SceneMode.Play;
				if(PrefabStageUtility.GetCurrentPrefabStage() != null)
					return SceneMode.Prefab;
				return SceneMode.Edit;
#endif
			}
		}

		public static bool IsPlaying => CurrentMode == SceneMode.Play;

		public static bool IsEditing => CurrentMode switch
		{
			SceneMode.Edit => true,
			SceneMode.Prefab => true,
			_ => false,
		};

		public static Camera EditorSceneCamera
		{
			get
			{
#if UNITY_EDITOR
				if(SceneView.currentDrawingSceneView != null)
					return SceneView.currentDrawingSceneView.camera;
#endif
				return null;
			}
		}
		#endregion
	}

	public enum SceneMode { Play, Edit, Prefab }
}
