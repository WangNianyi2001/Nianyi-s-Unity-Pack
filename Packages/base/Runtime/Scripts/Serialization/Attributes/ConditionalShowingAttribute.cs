using UnityEngine;
using System;

namespace Nianyi.UnityPack
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public abstract class ConditionalShowingAttribute : PropertyAttribute
	{
	}
}
