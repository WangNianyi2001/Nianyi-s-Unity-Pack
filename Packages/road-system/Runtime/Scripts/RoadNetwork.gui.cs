#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Nianyi.UnityPack.RoadSystem
{
	public partial class RoadNetwork
	{
		protected void OnDrawGizmos()
		{
			if(roadNetworkAsset == null)
				return;

			Gizmos.matrix = transform.localToWorldMatrix;

			// Anchor nodes

			Gizmos.color = new(1, 0, 0, .5f);
			foreach(var node in Data.anchorNodes)
			{
				Gizmos.DrawSphere(node.GetPosition(), 0.5f);
			}

			// Splines

			foreach(var spline in Data.roadSplines)
			{
				// Control points
				foreach(var cp in spline.controlPoints)
				{
					// Point
					Gizmos.color = new(0, 1, 1, .5f);
					Gizmos.DrawSphere(cp.position, 0.25f);

					// Slope
					Vector3 tangent = (cp.handleNext - cp.handlePrev) * .5f;
					Vector3 vSlope = Vector3.Cross(Vector3.up, tangent.normalized) + Vector3.up * cp.slope;
					Gizmos.color = new(1, 1, 0, .5f);
					Gizmos.DrawLine(cp.position + vSlope, cp.position - vSlope);
				}

				// Sections
				foreach(var section in spline.GetAllSections())
				{
					// Handles
					Gizmos.color = new(1, 1, 0, .5f);
					Gizmos.DrawLine(section.anchor0, section.cp0);
					Gizmos.DrawLine(section.anchor1, section.cp1);

					// Section line
					Gizmos.color = new(1, 1, 1, .5f);
					Vector3[] lineList = MathUtility.Interpolate(8)
						.Select(t => section.Sample(t))
						.ToArray();
					Gizmos.DrawLineStrip(lineList, false);
				}
			}
		}
	}
}
#endif