using UnityEditor;
using UnityEngine;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(ExpandedAttribute))]
	public class ExpandedAttributeDrawer : PropertyDrawer
	{
		static bool ShouldDraw(SerializedProperty property)
		{
			return property.propertyType switch
			{
				SerializedPropertyType.Generic => true,
				SerializedPropertyType.ManagedReference => true,
				SerializedPropertyType.ObjectReference => property.objectReferenceValue is ScriptableObject,
				_ => false,
			};
		}

		static bool IsReferenceType(SerializedProperty property)
		{
			return property.propertyType switch
			{
				SerializedPropertyType.ManagedReference => true,
				SerializedPropertyType.ObjectReference => true,
				_ => false,
			};
		}

		static bool IsNotNull(SerializedProperty property)
		{
			return property.propertyType switch
			{
				SerializedPropertyType.Generic => true,
				SerializedPropertyType.ManagedReference => property.managedReferenceValue != null,
				SerializedPropertyType.ObjectReference => property.objectReferenceValue != null,
				_ => false,
			};
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if(!ShouldDraw(property))
				return EditorGUI.GetPropertyHeight(property, label);

			float sum = EditorGUIUtility.singleLineHeight;

			SerializedProperty it = property.Copy(), end = it.GetEndProperty();
			if(it.NextVisible(true))
			{
				do
				{
					sum += EditorGUIUtility.standardVerticalSpacing;
					sum += EditorGUI.GetPropertyHeight(it, label, true);
				}
				while(it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
			}

			return sum;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(!ShouldDraw(property))
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			// Header
			position.height = EditorGUIUtility.singleLineHeight;
			GUIStyle labelStyle = new(GUI.skin.label) { fontStyle = FontStyle.Bold };
			if(IsReferenceType(property))
			{
				// TODO: Make bold.
				EditorGUI.ObjectField(position, property, label);
			}
			else
				EditorGUI.LabelField(position, label, labelStyle);
			position.y += position.height;

			// Content
			// TODO: Support deflating SO.
			if(IsNotNull(property))
			{
				++EditorGUI.indentLevel;

				SerializedProperty it = property.Copy(), end = it.GetEndProperty();
				if(it.NextVisible(true))
				{
					do
					{
						position.y += EditorGUIUtility.standardVerticalSpacing;
						position.height = (float)EditorGUI.GetPropertyHeight(it, label);
						EditorGUI.PropertyField(position, it, true);
						position.y += position.height;
					}
					while(it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
				}

				--EditorGUI.indentLevel;
			}
		}
	}
}