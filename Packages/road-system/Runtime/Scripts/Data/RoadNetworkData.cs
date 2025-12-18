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
		[System.NonSerialized] public List<Spline> splines = new();
		[SerializeField] List<SplineDef> splineDefs;

		[System.Serializable]
		public struct SplineDef
		{
			public string startAddress, endAddress;
			public Vector3 startCP, endCP;
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

		public void ApplySplineDef(in SplineDef def, Spline spline)
		{
			spline.start = GetNodeByAddress(def.startAddress);
			spline.end = GetNodeByAddress(def.endAddress);
			spline.startCP = def.startCP;
			spline.endCP = def.endCP;
		}

		public SplineDef CreateSplineDef(in Spline spline)
		{
			SplineDef def = new()
			{
				startAddress = GetNodeAddress(spline.start),
				endAddress = GetNodeAddress(spline.end),
				startCP = spline.startCP,
				endCP = spline.endCP
			};
			return def;
		}
		#endregion

		#region Serialization
		public void OnAfterDeserialize()
		{
			// Splines

			splines ??= new();

			if(splines.Count > splineDefs.Count)
				splines.RemoveRange(splineDefs.Count, splines.Count - splineDefs.Count);
			else if(splines.Count < splineDefs.Count)
			{
				int addCount = splineDefs.Count - splines.Count;
				for(int i = 0; i < addCount; ++i)
					splines.Add(new());
			}

			for(int i = 0; i < splines.Count; ++i)
			{
				if(splines[i] == null)
					splines[i] = new();
				ApplySplineDef(splineDefs[i], splines[i]);
			}
		}

		public void OnBeforeSerialize()
		{
			// Spline

			splines ??= new();

			splineDefs = splines.Select(s => {
				if(s == null)
					return default;

				return CreateSplineDef(s);
			}).ToList();
		}
		#endregion
	}
}
