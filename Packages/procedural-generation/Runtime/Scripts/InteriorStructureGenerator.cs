using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
	public class InteriorStructureGenerator : ProceduralGenerator
	{
		#region Component references
		MeshFilter meshFilter;
		MeshRenderer meshRenderer;

		void FetchComponentReferences()
		{
			HierarchyUtility.EnsureComponent(gameObject, out meshFilter);
			HierarchyUtility.EnsureComponent(gameObject, out meshRenderer);
		}
		#endregion

		#region Generation
		[SerializeField] Material debugMaterial;
		[SerializeField, Expanded] InteriorStructure config;

		public override void Regenerate()
		{
			FetchComponentReferences();
			DestroyPreviousGeneration();
			Generate();
		}

		void DestroyPreviousGeneration()
		{
			HierarchyUtility.Destroy(meshFilter.sharedMesh);
			meshFilter.sharedMesh = null;

			meshRenderer.sharedMaterials = new Material[0];
		}

		void Generate()
		{
			DynamicMesh mesh = new();

			foreach(var room in config.rooms)
				GenerateRoomCeilingAndFloor(mesh, room);

			meshFilter.sharedMesh = mesh.ToMesh(true);
			meshRenderer.sharedMaterial = debugMaterial;
		}

		void GenerateRoomCeilingAndFloor(DynamicMesh mesh, InteriorStructure.Room room)
		{
			var floor = mesh.CreateFace(room.walls.Select(w => mesh.CreateVertex(w.from.vertex.position)));

			var ceiling = mesh.CreateFace(room.walls
				.Select(w => new DynamicMesh.Vertex[2] {
					mesh.CreateVertex(w.from.vertex.position + Vector3.up * w.from.height),
					mesh.CreateVertex(w.to.vertex.position + Vector3.up * w.to.height),
				})
				.SelectMany(x => x)
			);
			mesh.InvertNormals(ceiling);
		}
		#endregion
	}
}
