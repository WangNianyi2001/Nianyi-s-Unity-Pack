using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack
{
	[CustomEditor(typeof(ProceduralGenerator), true)]
	public class ProceduralGeneratorEditor : UnityEditor.Editor
	{
		ProceduralGenerator Generator => serializedObject.targetObject as ProceduralGenerator;

		protected void DrawHeaderButtons()
		{
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Update generation"))
				Generator.UpdateGeneration();
			EditorGUILayout.EndHorizontal();
		}

		public override void OnInspectorGUI()
		{
			DrawHeaderButtons();

			base.OnInspectorGUI();
		}
	}
}
