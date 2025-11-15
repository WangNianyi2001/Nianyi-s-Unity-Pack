using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(AssetOnlyAttribute))]
	public class AssetOnlyAttributeDrawer : PropertyDrawer
	{
		public static bool ShouldDraw(SerializedProperty property)
		{
			if(property.propertyType == SerializedPropertyType.ObjectReference)
				return true;
			Debug.LogWarning($"The field \"{property.name}\" of type {property.serializedObject.targetObject.GetType().Name} is not applicable for AssetOnlyAttribute, as it is not a Unity Object reference.");
			return false;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(!ShouldDraw(property))
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			// TODO: Handle arrays.
			EditorGUI.BeginChangeCheck();
			property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, fieldInfo.FieldType, false);
			if(EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(property.serializedObject.targetObject);
		}
	}
}
