using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(ReadOnlyInInspectorAttribute))]
	public class ReadOnlyInInspectorDrawer : PropertyDrawer
	{
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
