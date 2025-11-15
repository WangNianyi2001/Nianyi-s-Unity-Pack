using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack.Editor
{
	[CustomPropertyDrawer(typeof(Matrix4x4))]
	public class Matrix4x4Drawer : PropertyDrawer
	{
		const string headers = "xyzw";
		const int d = 4;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * (d + 1) + EditorGUIUtility.standardVerticalSpacing * d;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			MemberAccessor member = new(property);
			Matrix4x4 matrix = member.Get<Matrix4x4>();
			EditorGUI.BeginChangeCheck();

			float labelWidth = EditorGUIUtility.labelWidth;
			Rect row = new(position) { height = EditorGUIUtility.singleLineHeight };
			for(int y = 0; y <= d; ++y)
			{
				int realY = y - 1;
				Rect cell = new(row) { width = labelWidth };
				for(int x = 0; x <= d; ++x)
				{
					int realX = x - 1;
					if(x == 0)  // Left-most column.
					{
						if(y == 0)  // Label.
						{
							EditorGUI.LabelField(new Rect(position)
							{
								width = labelWidth,
								height = EditorGUIUtility.singleLineHeight,
							}, label);
						}
						else  // Row headers.
							EditorGUI.LabelField(cell, headers[realY].ToString());
						cell.width = (position.width - labelWidth) / 4 - EditorGUIUtility.standardVerticalSpacing;
						cell.x += labelWidth;
					}
					else  // Rest columns.
					{
						if(y == 0)  // Row headers.
							EditorGUI.LabelField(cell, headers[realX].ToString());
						else  // Components.
							matrix[realY, realX] = EditorGUI.FloatField(cell, matrix[realY, realX]);
						cell.x += cell.width;
					}
					cell.x += EditorGUIUtility.standardVerticalSpacing;
				}
				row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			if(EditorGUI.EndChangeCheck())
			{
				member.Set(matrix);
				EditorUtility.SetDirty(property.serializedObject.targetObject);
			}
		}
	}
}
