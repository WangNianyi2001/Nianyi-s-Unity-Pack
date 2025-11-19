using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack.Editor
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
