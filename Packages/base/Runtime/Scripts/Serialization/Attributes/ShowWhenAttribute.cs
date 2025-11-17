using UnityEngine;
using System;

namespace Nianyi.UnityPack
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class ShowWhenAttribute : PropertyAttribute
	{
		public readonly string propertyName;
		public readonly object value;

		public ShowWhenAttribute(string propertyName) : this(propertyName, true) { }

		public ShowWhenAttribute(string propertyName, object value)
		{
			this.propertyName = propertyName;
			this.value = value;
		}
	}
}