using UnityEngine;
using UnityEditor;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(ShowWhenAttribute))]
	public class ShowWhenAttributeDrawer : PropertyDrawer
	{
		bool ShouldShow(SerializedProperty property)
		{
			bool showing = false;
			MemberAccessor member = new(property);
			foreach(var attribute in fieldInfo.GetCustomAttributes(false))
			{
				if(attribute is not ShowWhenAttribute)
					continue;
				var showWhen = attribute as ShowWhenAttribute;
				var targetProperty = member.Navigate(1, '.' + showWhen.propertyName);
				showing = showing || Equals(targetProperty.Get<object>(), showWhen.value);
			}
			return showing;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if(!ShouldShow(property))
				return 0f;
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(!ShouldShow(property))
				return;
			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}
