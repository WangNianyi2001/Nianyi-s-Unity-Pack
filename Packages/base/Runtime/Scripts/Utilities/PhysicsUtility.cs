using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public static class PhysicsUtility
	{

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
		public struct WallClipConstraint
		{
			public Vector3 normal;
			public float depth;
		}

		public static Vector3 WallClip(Vector3 movement, List<WallClipConstraint> constraints, float tolerance = 0.01f)
		{
			constraints = constraints
				.Select(c => {
					c.normal = c.normal.normalized;
					return c;
				})
				.Where(c => Vector3.Dot(c.normal, movement) < 0f && WallClip_Penalty(movement, c) > 0)
				.ToList();
			if(constraints.Count() < 3)
				return WallClip_Internal(movement, constraints);
			constraints.Sort((a, b) => {
				float pa = WallClip_Penalty(movement, a), pb = WallClip_Penalty(movement, b);
				return pa == pb ? 0 : pa > pb ? -1 : 1;
			});

			List<WallClipConstraint> activeSet = constraints.Take(1).ToList();
			for(int i = 1; i < constraints.Count; ++i)
			{
				activeSet.Add(constraints[i]);
				Vector3 clipped = WallClip_Internal(movement, activeSet);
				if(constraints.TakeLast(constraints.Count - i - 1).Any(c => WallClip_Penalty(movement, c) > tolerance))
					continue;
				return clipped;
			}

			return Vector3.zero;
		}

		static float WallClip_Penalty(Vector3 movement, WallClipConstraint constraint)
		{
			return Mathf.Max(0, -Vector3.Dot(movement, constraint.normal) + constraint.depth);
		}

		static Vector3 WallClip_Internal(Vector3 movement, IList<WallClipConstraint> constraints)
		{
			return constraints.Count() switch
			{
				0 => movement,
				1 => Vector3.ProjectOnPlane(movement, constraints[0].normal)
										- constraints[0].normal * constraints[0].depth,
				2 => Vector3.Project(movement, Vector3.Cross(constraints[0].normal, constraints[1].normal))
										- constraints[0].normal * constraints[0].depth
										- constraints[1].normal * constraints[1].depth,
				_ => Vector3.zero,
			};
		}

		public static Vector3 RigidbodyVelocityAtPoint(Vector3 point, Vector3 com, Vector3 velocity, Vector3 angularVelocity)
		{
			Vector3 arm = point - com;
			return Vector3.Cross(arm, angularVelocity) + velocity;
		}
		#endregion
	}
}
