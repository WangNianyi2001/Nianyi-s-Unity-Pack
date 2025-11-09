using System;
using UnityEngine;

namespace Nianyi.UnityPack
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public abstract class ConditionalShowingAttribute : PropertyAttribute
	{
	}
}
