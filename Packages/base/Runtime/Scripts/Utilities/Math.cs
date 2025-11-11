using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public static class Math
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

		#region Capsule
		public static void GetCapsuleCenters(in CapsuleCollider capsule, out Vector3 center1, out Vector3 center2)
		{
			float half = capsule.height * 0.5f - capsule.radius;
			var dir = capsule.direction switch
			{
				0 => Vector3.right,
				1 => Vector3.up,
				_ => Vector3.forward,
			};

			center1 = capsule.transform.TransformPoint(capsule.center + dir * half);
			center2 = capsule.transform.TransformPoint(capsule.center - dir * half);
		}
		#endregion

		#region Vector
		public static Vector3 WallSlide(Vector3 movement, IEnumerable<Vector3> normals, float threshold = 0.01f)
		{
			if(normals.Count() == 0)
				return movement;

			const int maxStep = 4;
			threshold *= threshold;
			for(int i = 0; i < maxStep; ++i)
			{
				foreach(var normal in normals)
				{
					if(Vector3.Dot(movement, normal) >= 0f)
						continue;
					movement = Vector3.ProjectOnPlane(movement, normal);
				}
				if(movement.sqrMagnitude < threshold)
					break;
			}
			return movement;
		}
		#endregion
	}
}
