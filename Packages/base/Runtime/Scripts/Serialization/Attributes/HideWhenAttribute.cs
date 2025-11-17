using UnityEngine;
using System;

namespace Nianyi.UnityPack
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class HideWhenAttribute : PropertyAttribute
	{
		public readonly string propertyName;
		public readonly object value;

		public HideWhenAttribute(string propertyName) : this(propertyName, true) { }

		public HideWhenAttribute(string propertyName, object value)
		{
			this.propertyName = propertyName;
			this.value = value;
		}
	}
}