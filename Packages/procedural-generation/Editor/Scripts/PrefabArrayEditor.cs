using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Nianyi.UnityPack
{
	[CustomEditor(typeof(PrefabArray))]
	public class PrefabArrayEditor : ProceduralGeneratorEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			using(new EditorGUI.DisabledGroupScope(true))
			{
				var instances = serializedObject.FindProperty("instances");
				if(instances != null)
					EditorGUILayout.PropertyField(instances, true);
			}
		}
	}
}
