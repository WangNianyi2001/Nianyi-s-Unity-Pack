using UnityEngine;

namespace Nianyi.UnityPack
{
	public class ShowWhenAttribute : ConditionalShowingAttribute
	{
		public readonly string propertyName;
		public readonly object value;

		public ShowWhenAttribute(string propertyName, object value)
		{
			this.propertyName = propertyName;
			this.value = value;
		}
	}
}