using UnityEditor;
using UnityEngine;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
	public class ReadOnlyInInspectorDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool disabled = true;
			if((attribute as ReadOnlyInInspectorAttribute).allowInEditMode)
			{
				if(!Application.isPlaying)
					disabled = false;
			}
			EditorGUI.BeginDisabledGroup(disabled);
			EditorGUI.PropertyField(position, property, label);
			EditorGUI.EndDisabledGroup();
		}
	}
}
