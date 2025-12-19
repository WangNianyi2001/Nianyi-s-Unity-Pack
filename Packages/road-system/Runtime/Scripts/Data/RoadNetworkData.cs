using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack.RoadSystem
{
	[System.Serializable]
	public class RoadNetworkData : ISerializationCallbackReceiver
	{
		#region Nodes
		public List<AnchorNode> anchorNodes;
		#endregion

		#region Splines
		[System.NonSerialized] public List<RoadSpline> roadSplines = new();
		[SerializeField] List<RoadSplineDef> roadSplineDefs;

		[System.Serializable]
		public struct RoadSplineDef
		{
			public string startAddress, endAddress;
			public Vector3 startCP, endCP;

			public List<RoadSpline.ControlPoint> controlPoints;
		}

		public INode GetNodeByAddress(string address)
		{
			// Anchor node
			if(address.StartsWith("a#"))
			{
				string indexPart = address.Substring(2);
				if(!int.TryParse(indexPart, out var index))
					return null;
				return anchorNodes[index];
			}

			return null;
		}

		public string GetNodeAddress(INode node)
		{
			if(node is AnchorNode)
			{
				var anchor = node as AnchorNode;
				int index = anchorNodes.IndexOf(anchor);
				if(index < 0)
					return null;
				return $"a#{index}";
			}

			return null;
		}

		public void ApplySplineDef(in RoadSplineDef def, RoadSpline spline)
		{
			spline.start = GetNodeByAddress(def.startAddress);
			spline.end = GetNodeByAddress(def.endAddress);

			// Control points

			spline.controlPoints = def.controlPoints.ToList();
			while(spline.controlPoints.Count < 2)
				spline.controlPoints.Add(default);

			var startCP = spline.controlPoints[0];
			startCP.position = spline.start.GetPosition();
			spline.controlPoints[0] = startCP;

			var endCP = spline.controlPoints[^1];
			endCP.position = spline.end.GetPosition();
			spline.controlPoints[^1] = endCP;
		}

		public RoadSplineDef CreateSplineDef(in RoadSpline spline)
		{
			RoadSplineDef def = new()
			{
				startAddress = GetNodeAddress(spline.start),
				endAddress = GetNodeAddress(spline.end),

				controlPoints = spline.controlPoints.ToList(),
			};
			return def;
		}
		#endregion

		#region Serialization
		public void OnAfterDeserialize()
		{
			// Splines

			roadSplines ??= new();

			if(roadSplines.Count > roadSplineDefs.Count)
				roadSplines.RemoveRange(roadSplineDefs.Count, roadSplines.Count - roadSplineDefs.Count);
			else if(roadSplines.Count < roadSplineDefs.Count)
			{
				int addCount = roadSplineDefs.Count - roadSplines.Count;
				for(int i = 0; i < addCount; ++i)
					roadSplines.Add(new());
			}

			for(int i = 0; i < roadSplines.Count; ++i)
			{
				if(roadSplines[i] == null)
					roadSplines[i] = new();
				ApplySplineDef(roadSplineDefs[i], roadSplines[i]);
			}
		}

		public void OnBeforeSerialize()
		{
			// Spline

			roadSplines ??= new();

			roadSplineDefs = roadSplines.Select(s =>
			{
				if(s == null)
					return default;

				return CreateSplineDef(s);
			}).ToList();
		}
		#endregion
	}
}
