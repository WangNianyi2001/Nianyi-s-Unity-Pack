using System;
using UnityEngine;

namespace Nianyi.UnityPack
{
	[AttributeUsage(AttributeTargets.Field)]
	public class ReadOnlyInInspectorAttribute : PropertyAttribute
	{
		public bool allowInEditMode = false;
		public ReadOnlyInInspectorAttribute(bool allowInEditMode = false)
		{
			this.allowInEditMode = allowInEditMode;
		}
	}
}
