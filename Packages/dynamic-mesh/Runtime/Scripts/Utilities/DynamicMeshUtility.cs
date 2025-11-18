using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	using Vertex = DynamicMesh.Vertex;
	using Face = DynamicMesh.Face;

	public static class DynamicMeshUtility
	{
		public static Vertex[] GetVertices(this IEnumerable<Face> faces)
		{
			return faces.SelectMany(f => f.Vertices).ToArray();
		}
	}
}
