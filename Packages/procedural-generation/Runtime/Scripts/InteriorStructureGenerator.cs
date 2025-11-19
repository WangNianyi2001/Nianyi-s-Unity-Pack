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
		[SerializeField, Expanded] public InteriorStructure config;

		public override void NewGeneration()
		{
			FetchComponentReferences();

			DynamicMesh mesh = new();

			foreach(var room in config.rooms)
				GenerateRoom(mesh, room);

			meshFilter.sharedMesh = mesh.ToMesh(true);
			meshRenderer.sharedMaterial = debugMaterial;
		}

		public override void DestroyGeneration()
		{
			FetchComponentReferences();

			HierarchyUtility.Destroy(meshFilter.sharedMesh);
			meshFilter.sharedMesh = null;

			meshRenderer.sharedMaterials = new Material[0];
		}

		class WallContext
		{
			public InteriorStructure.Wall wall;
			public bool flipped;

			public InteriorStructure.Wall.End GetEnd(bool to)
			{
				to ^= flipped;
				return to ? wall.to : wall.from;
			}

			public WallContext prev, next;

			public DynamicMesh.Vertex from, to;
			public DynamicMesh.Vertex fromTop, toTop;

			public Vector3 tangent, normal;
		}

		void GenerateRoom(DynamicMesh mesh, InteriorStructure.Room room)
		{
			var walls = room.walls;
			int count = walls.Count;

			// Preprocess wall contexts.
			WallContext[] contexts = new WallContext[count];
			for(int i = 0; i < count; ++i)
			{
				InteriorStructure.Wall
					wall = walls[i],
					next = walls[(i + 1) % count];
				bool flipped = !(wall.to.vertex == next.from.vertex || wall.to.vertex == next.to.vertex);

				var fromVert = mesh.CreateVertex((flipped ? wall.to : wall.from).vertex.position);

				var tangent = wall.to.vertex.position - wall.from.vertex.position;
				tangent.Normalize();
				if(flipped)
					tangent = -tangent;

				var normal = Vector3.Cross(Vector3.up, tangent);

				WallContext context = contexts[i] = new()
				{
					wall = wall,
					flipped = flipped,
					from = fromVert,
					tangent = tangent,
					normal = normal,
				};

			}
			for(int i = 0; i < count; ++i)
			{
				WallContext
					context = contexts[i],
					prev = contexts[(i + count - 1) % count],
					next = contexts[(i + 1) % count];

				context.prev = prev;
				context.next = next;

				Vector3 displacement = context.normal * context.wall.thickness + prev.normal * prev.wall.thickness;
				displacement *= .5f;
				context.from.position += displacement;

				context.to = next.from;
				context.fromTop = mesh.CreateVertex(context.from.position + Vector3.up * context.GetEnd(false).height);

				float margin = -Vector3.Dot(displacement, context.tangent);
			}
			foreach(var context in contexts)
			{
				bool sameHeight = Mathf.Abs(context.GetEnd(true).height - context.next.GetEnd(true).height) < 0.01f;
				if(sameHeight)
					context.toTop = context.next.fromTop;
				else
					context.toTop = mesh.CreateVertex(context.to.position + Vector3.up * context.GetEnd(true).height);
			}

			// Generate floor.
			if(room.generateFloor)
				mesh.CreateFace(contexts.Select(c => c.from));

			// Generate ceiling.
			if(room.generateCeiling)
			{
				// Deal with sudden height changes better.
				List<DynamicMesh.Vertex> ceilingVerts = new(count * 2);
				foreach(var context in contexts)
				{
					ceilingVerts.Add(context.fromTop);
					if(context.toTop != context.next.fromTop)
						ceilingVerts.Add(context.toTop);
				}
				ceilingVerts.Reverse();
				mesh.CreateFace(ceilingVerts);
			}

			// Generate walls.
			foreach(var context in contexts)
				GenerateWall(mesh, context);
		}

		void GenerateWall(DynamicMesh mesh, WallContext context)
		{
			mesh.CreateFace(context.from, context.fromTop, context.toTop, context.to);
		}
		#endregion
	}
}
