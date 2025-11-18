using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public class InteriorStructure
	{
		public List<Vertex> vertices;
		public List<Wall> walls;
		public List<Room> rooms;

		[System.Serializable]
		public class Room
		{
			public List<int> wallIndices;
		}

		[System.Serializable]
		public class Wall
		{
			public End from, to;
			public bool fill = true;

			[System.Serializable]
			public class End
			{
				public int vertexIndex;
				public float height = 3f;
				public float thickness = 0.1f;
			}
		}

		[System.Serializable]
		public class Vertex
		{
			public Vector3 position;
		}
	}

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
			var vertices = room.wallIndices
				.Select(i => config.walls[i].from.vertexIndex)
				.Select(i => config.vertices[i].position)
				.Select(p => mesh.CreateVertex(p))
				.ToArray();

			var floor = mesh.CreateFace(vertices);

			var ceiling = mesh.DuplicateFaces(floor);
			mesh.InvertNormals(ceiling);
			mesh.TranslateVertices(Vector3.up * 3f, ceiling.GetVertices());
		}
		#endregion
	}
}
