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

			// Anchor nodes

			Gizmos.color = new(1, 0, 0, .5f);
			foreach(var node in Data.anchorNodes)
			{
				Gizmos.DrawSphere(transform.TransformPoint(node.GetPosition()), 0.5f);
			}

			// Splines

			foreach(var spline in Data.splines)
			{
				Vector3 start = transform.TransformPoint(spline.start.GetPosition());
				Vector3 end = transform.TransformPoint(spline.end.GetPosition());
				Vector3 startCP = start + transform.TransformVector(spline.startCP);
				Vector3 endCP = end + transform.TransformVector(spline.endCP);

				// Control points
				Gizmos.color = new(0, 1, 1, .5f);
				Gizmos.DrawLine(start, startCP);
				Gizmos.DrawSphere(startCP, 0.1f);
				Gizmos.DrawLine(end, endCP);
				Gizmos.DrawSphere(endCP, 0.1f);

				// Line
				Gizmos.color = new(1, 1, 0, .5f);
				Vector3[] lineList = MathUtility.Interpolate(8)
					.Select(t => spline.SamplePositionByPortion(t))
					.ToArray();
				Gizmos.DrawLineStrip(lineList, false);
			}
		}
	}
}
#endif