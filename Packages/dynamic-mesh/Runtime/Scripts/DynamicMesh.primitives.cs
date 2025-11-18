using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public partial class DynamicMesh
	{
		#region Regsitry
		readonly HashSet<Vertex> vertices = new();
		readonly HashSet<Face> faces = new();

		public Vertex[] GetVertices() => vertices.ToArray();
		public Face[] GetFaces() => faces.ToArray();
		#endregion

		#region Definitions
		/// <summary>
		/// A polygon face in a 3D model.
		/// </summary>
		/// <remarks>
		/// The out-facing orientation is defined to be the clockwise order (left-hand rule).
		/// </remarks>
		public class Face
		{
			/// <summary>
			/// Used for separating submeshes.
			/// </summary>
			public string material = string.Empty;

			public Face() { }

			public void CopySettings(Face f)
			{
				material = f.material;
			}

			/// <summary>
			/// One non-particular edge on this face.
			/// </summary>
			public Edge oneEdge;

			public IEnumerable<Edge> Edges
			{
				get
				{
					Edge it = oneEdge;
					do
					{
						yield return it;
						it = it.Next;
					}
					while(it != oneEdge);
				}
			}

			public IEnumerable<Corner> Corners => Edges.Select(e => e.destination);
			public IEnumerable<Vertex> Vertices => Corners.Select(c => c.vertex);
		}

		/// <summary>
		/// An "edge cluster".
		/// </summary>
		public class Ridge
		{
			public readonly List<Edge> edges = new();
		}

		/// <summary>
		/// A 2D edge on a polygon face having a source and a destination corner.
		/// </summary>
		public class Edge
		{
			public Corner destination;
			public Ridge ridge;
			public Face face;

			public Edge Next => destination.outGoingEdge;
			public Edge Previous
			{
				get
				{
					Edge it = this;
					do
						it = it.Next;
					while(it.Next != this);
					return it;
				}
			}
		}

		/// <summary>
		/// A "corner cluster".
		/// </summary>
		public class Vertex
		{
			public Vector3 position;

			public Vertex() { }
			public void CopySettings(Vertex v)
			{
				position = v.position;
			}

			public readonly List<Corner> corners = new();
		}

		/// <summary>
		/// A 2D corner of a polygon face.
		/// </summary>
		public class Corner
		{
			public Vector3 normal;
			/// <remarks>
			/// Might be null.
			/// </remarks>
			public Vector2[] uv;

			public Corner() { }

			public void CopySettings(Corner c)
			{
				normal = c.normal;
				if(c.uv != null)
				{
					uv = new Vector2[c.uv.Length];
					for(int i = 0; i < uv.Length; ++i)
						uv[i] = c.uv[i];
				}
			}

			public Vertex vertex;
			/// <summary>
			/// The only out-going edge from this corner.
			/// </summary>
			public Edge outGoingEdge;

			public Face Face => outGoingEdge.face;
			public Corner Next => outGoingEdge?.destination;
		}
		#endregion
	}
}
