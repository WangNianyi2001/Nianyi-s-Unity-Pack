using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public partial class DynamicMesh
	{
		#region Join/separation
		public void Join(DynamicMesh mesh)
		{
			Dictionary<Vertex, Vertex> vMap = new(mesh.vertices.Count);
			Dictionary<Corner, Corner> cMap = new();
			Dictionary<Edge, Edge> eMap = new();
			Dictionary<Ridge, Ridge> rMap = new();
			Dictionary<Face, Face> fMap = new(mesh.faces.Count);

			// Copy vertices and corners.
			foreach(var v in mesh.vertices)
			{
				Vertex vCopy = new(v);
				vMap.Add(v, vCopy);
				foreach(var c in v.corners)
				{
					Corner cCopy = new();
					cCopy.CopySettings(c);
					cMap.Add(c, cCopy);

					vCopy.corners.Add(cCopy);
					cCopy.vertex = vCopy;
				}
			}

			// Copy edges.
			foreach(var v in mesh.vertices)
			{
				foreach(var c in v.corners)
				{
					Edge e = c.outGoingEdge, eCopy = new();
					eMap.Add(e, eCopy);

					cMap[c].outGoingEdge = eCopy;
					eCopy.destination = cMap[e.destination];
				}
			}

			// Copy ridges.
			foreach(var (e, eCopy) in eMap)
			{
				if(!rMap.TryGetValue(e.ridge, out Ridge rCopy))
				{
					rCopy = new();
					rMap.Add(e.ridge, rCopy);
				}
				eCopy.ridge = rCopy;
				rCopy.edges.Add(eCopy);
			}

			// Copy faces.
			foreach(var f in mesh.faces)
			{
				Face fCopy = new();
				fCopy.CopySettings(f);
				fMap.Add(f, fCopy);

				fCopy.oneEdge = eMap[f.oneEdge];
				foreach(var e in f.Edges)
					eMap[e].face = fCopy;
			}

			// Integrate into self.
			foreach(var f in fMap.Values) faces.Add(f);
			foreach(var v in vMap.Values) vertices.Add(v);
		}
		#endregion

		#region Triangularization
		public void Triangularize()
		{
			var nonTriangularFaces = faces
				.Where(f => f.Edges.Count() != 3).ToArray();
			foreach(var f in nonTriangularFaces)
			{
				var vertices = f.Vertices.ToArray();
				DeleteFace(f);

				if(vertices.Length < 3)
					continue;

				// TODO: A proper triangularization algorithm.
				// Current implementation only works for convex cases.

				var v0 = vertices[0];
				for(int i = 1; i + 1 < vertices.Length; ++i)
				{
					var fCopy = CreateFace(new Vertex[] { v0, vertices[i], vertices[(i + 1) % vertices.Length] });
					fCopy.CopySettings(f);
				}
			}
		}
		#endregion
	}
}
