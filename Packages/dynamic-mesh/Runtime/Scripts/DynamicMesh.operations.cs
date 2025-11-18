using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public partial class DynamicMesh
	{
		#region Vertex
		public Vertex CreateVertex(Vector3 position)
		{
			Vertex v = new();
			vertices.Add(v);
			v.position = position;
			return v;
		}

		public void DeleteVertex(Vertex v)
		{
			if(!vertices.Contains(v))
				return;

			foreach(var c in v.corners)
				DeleteFace(c.outGoingEdge.face);

			vertices.Remove(v);
		}

		public void PruneEmptyVertices()
		{
			foreach(var v in vertices.Where(v => v.corners.Count == 0).ToArray())
				DeleteVertex(v);
		}

		public void TranslateVertices(Vector3 movement, params Vertex[] vertices)
		{
			foreach(var v in vertices)
				v.position += movement;
		}

		public void TransformVertices(Matrix4x4 transform, Vector3 pivot, params Vertex[] vertices)
		{
			foreach(var v in vertices)
				v.position = MathUtility.TransformAroundPivot(v.position, transform, pivot);
		}
		#endregion

		#region Face
		public Face CreateFace(IEnumerable<Vertex> vertices)
		{
			vertices = vertices.ToArray();
			if(vertices.Count() < 3)
				return null;
			if(vertices.Any(v => !vertices.Contains(v)))
				return null;

			var corners = vertices.Select(v =>
			{
				Corner c = new();
				v.corners.Add(c);
				c.vertex = v;
				return c;
			}).ToArray();

			for(int i = 0; i < corners.Length; ++i)
			{
				Corner source = corners[i], destination = corners[(i + 1) % corners.Length];
				Ridge r = GetRidge(source.vertex, destination.vertex) ?? new();
				Edge e = new();
				r.edges.Add(e);
				e.ridge = r;
				source.outGoingEdge = e;
				e.destination = destination;
			}

			Face f = new();
			faces.Add(f);
			f.oneEdge = corners[0].outGoingEdge;
			foreach(var e in f.Edges)
				e.face = f;

			return f;
		}
		public Face CreateFace(params Vertex[] vertices) => CreateFace(vertices as IEnumerable<Vertex>);

		public void DeleteFace(Face f)
		{
			if(!faces.Contains(f))
				return;

			foreach(var e in f.Edges)
			{
				e.destination.vertex.corners.Remove(e.destination);
				e.ridge.edges.Remove(e);
			}

			faces.Remove(f);
		}

		Ridge GetRidge(Vertex a, Vertex b)
		{
			if(a == null || b == null)
				return null;
			if(!vertices.Contains(a) || !vertices.Contains(b))
				return null;

			foreach(var c in a.corners)
			{
				if(c.Next?.vertex == b)
					return c.outGoingEdge.ridge;
			}
			foreach(var c in b.corners)
			{
				if(c.Next?.vertex == a)
					return c.outGoingEdge.ridge;
			}
			return null;
		}

		public Face[] DuplicateFaces(params Face[] faces)
		{
			var duplicated = faces.Select(f =>
			{
				Face copy = CreateFace(f.Vertices);
				copy.CopySettings(f);
				return copy;
			}).ToArray();

			SeparateFaces(duplicated);

			return duplicated;
		}

		public void SeparateFaces(params Face[] faces)
		{
			Vertex[] vertices = faces.GetVertices();
			Dictionary<Vertex, Vertex> vMap = new(vertices.Length);
			foreach(var v in vertices)
			{
				Vertex copy = new();
				this.vertices.Add(copy);
				copy.CopySettings(v);
				vMap[v] = copy;
			}

			foreach(var f in faces)
			{
				var edges = f.Edges.ToArray();
				var corners = edges.Select(e => e.destination).ToArray();

				foreach(var c in corners)
				{
					var v = c.vertex;
					v.corners.Remove(c);
					c.vertex = vMap[v];
					c.vertex.corners.Add(c);
				}

				foreach(var e in edges)
				{
					e.ridge.edges.Remove(e);
					e.ridge = new();
					e.ridge.edges.Add(e);
				}
			}
		}

		public void InvertNormals(params Face[] faces)
		{
			foreach(var f in faces)
				InvertNormal(f);
		}

		void InvertNormal(Face face)
		{
			var edges = face.Edges.ToArray();
			var corners = face.Corners.ToArray();

			for(int i = 0; i < edges.Length; ++i)
			{
				edges[i].destination = corners[(i + edges.Length - 1) % edges.Length];
				corners[i].outGoingEdge = edges[i];
			}
		}
		#endregion

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
				Vertex vCopy = new();
				vCopy.CopySettings(v);
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
