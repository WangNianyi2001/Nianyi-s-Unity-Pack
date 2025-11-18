using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack
{
	[CustomEditor(typeof(ProceduralGenerator), true)]
	public class ProceduralGeneratorEditor : Editor
	{
		ProceduralGenerator Generator => serializedObject.targetObject as ProceduralGenerator;

		public override void OnInspectorGUI()
		{
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Regenerate"))
				Generator.Regenerate();
			if(GUILayout.Button("Ungarrison"))
			{
				if(EditorUtility.DisplayDialog("Confirm", "Sure to ungarrison?", "Yes", "Cancel"))
				{
					Generator.Ungarrison();
					Undo.RegisterFullObjectHierarchyUndo(target, $"Ungarrison {target.name}");
				}
			}
			EditorGUILayout.EndHorizontal();

			base.OnInspectorGUI();
		}
	}
}
