using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public static class MathUtility
	{
		#region Float
		/// <summary>
		/// Returns the mathematically correct result of <c>a % b</c>.
		/// </summary>
		/// <remarks>
		/// The result always lies in <c>[0, b)</c>, given that <c>b</c> is positive.
		/// For example, <c>Mod(360f, -361f)</c> yields <c>359</c> instead of <c>-1</c>.
		/// </remarks>
		public static float Mod(in float a, in float b)
		{
			return a - b * Mathf.Floor(a / b);
		}
		#endregion

		#region Rotation
		public static Quaternion Scale(in this Quaternion quat, float t)
		{
			return Quaternion.SlerpUnclamped(Quaternion.identity, quat, t);
		}

		/// <param name="range">In [0, 90]</param>
		public static Vector3 RotateEulerWithZenithClamped(this Vector3 eulers, in Vector3 delta, float range = 90f)
		{
			range = Mathf.Clamp(range, 0f, 90f);

			var zenith = Mod(eulers.x, 360);
			if(zenith > 180f)
				zenith -= 360f;
			zenith += delta.x;
			zenith = Mathf.Clamp(zenith, -range, range);
			if(zenith < 0f)
				zenith += 360f;

			eulers += delta;
			eulers.x = zenith;

			return eulers;
		}
		#endregion
	}
}
