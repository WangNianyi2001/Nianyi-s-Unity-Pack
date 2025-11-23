using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public partial class DynamicMesh
	{
		#region Configurations
		public int usedUvChannelCount = 0;
		#endregion

		#region Creation
		public DynamicMesh() { }

		public DynamicMesh(DynamicMesh source)
		{
			Join(source);
		}

		public DynamicMesh Duplicate()
		{
			return new(this);
		}
		#endregion

		#region Export
		public void WriteToMesh(Mesh mesh, out string[] materialMap, bool recalculateNormals = false)
		{
			DynamicMesh triangulated = Duplicate();
			triangulated.Triangularize();

			string[] materials = triangulated.faces.Select(f => f.material).Distinct().ToArray();
			Dictionary<string, int> materialIndexMap = new(materials.Length);
			List<int>[] submeshIndices = new List<int>[materials.Length];
			for(int i = 0; i < materials.Length; ++i)
			{
				materialIndexMap.Add(materials[i], i);
				submeshIndices[i] = new();
			}

			Corner[] corners = triangulated.faces.SelectMany(f => f.Corners).ToArray();
			Dictionary<Corner, int> cornerIndexMap = new(corners.Length);
			for(int i = 0; i < corners.Length; ++i)
				cornerIndexMap.Add(corners[i], i);
			foreach(var f in triangulated.faces)
			{
				int submeshIndex = materialIndexMap[f.material];
				foreach(var c in f.Corners)
					submeshIndices[submeshIndex].Add(cornerIndexMap[c]);
			}

			mesh.SetVertices(corners.Select(c => c.vertex.position).ToList());
			mesh.SetNormals(corners.Select(c => c.normal).ToList());
			for(int i = 0; i < usedUvChannelCount; ++i)
			{
				mesh.SetUVs(i, corners.Select(c =>
				{
					if(c.uv != null && c.uv.Length > i)
						return c.uv[i];
					return default;
				}).ToList());
			}

			mesh.subMeshCount = materials.Length;
			for(int i = 0; i < materials.Length; ++i)
				mesh.SetIndices(submeshIndices[i], MeshTopology.Triangles, i);

			if(recalculateNormals)
				mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			materialMap = materials;
		}
		#endregion
	}
}