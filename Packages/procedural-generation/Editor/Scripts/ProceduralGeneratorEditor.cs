using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Nianyi.UnityPack
{
	[CustomEditor(typeof(ProceduralGenerator), true)]
	public class ProceduralGeneratorEditor : Editor
	{
		static string[] skip = new string[] { "m_Script", "config" };

		ProceduralGenerator Generator => serializedObject.targetObject as ProceduralGenerator;

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Manual Regenerate"))
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

			EditorGUI.BeginChangeCheck();

			SerializedProperty config = serializedObject.FindProperty("config");
			if(config != null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(config, true);
				if(EditorGUI.EndChangeCheck())
					EditorApplication.delayCall += Generator.Regenerate;
			}

			SerializedProperty it = serializedObject.GetIterator();
			if(it.NextVisible(true))
			{
				do
				{
					if(skip.Contains(it.name))
						continue;
					EditorGUILayout.PropertyField(it, true);
				}
				while(it.NextVisible(false));
			}

			if(EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
		}
	}
}
