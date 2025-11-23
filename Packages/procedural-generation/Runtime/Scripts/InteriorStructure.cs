using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public partial class InteriorStructure : ISerializationCallbackReceiver
	{
		public List<Vertex> vertices = new();
		public List<Wall> walls = new();
		public List<Room> rooms = new();

		public interface IGeometry
		{
			public Vertex[] GetVertices();
		}

		[System.Serializable]
		public class Room : IGeometry
		{
			public bool generateFloor = true;
			public float floorThickness = 0.25f;  // The depth between the floor's upward surface and the downward outside surface.
			public bool generateCeiling = true;
			public float ceilingThickness = 0.25f;  // Save as above, reversed.

			public bool overrideFloorMaterial = false;
			[ShowWhen(nameof(overrideFloorMaterial))] public Material floorMaterial;
			public bool overrideCeilingMaterial = false;
			[ShowWhen(nameof(overrideCeilingMaterial))] public Material ceilingMaterial;

			[SerializeField] public List<int> wallIndices;
			[System.NonSerialized] public List<Wall> walls;

			public Vertex[] GetVertices()
			{
				Vertex[] vertices = new Vertex[walls.Count];
				for(int i = 0; i < walls.Count; ++i)
				{
					var w = walls[i];
					vertices[i] = (IsWallFlipped(i) ? w.to : w.from).vertex;
				}
				return vertices;
			}

			public bool IsWallFlipped(int i)
			{
				if(i < 0 || i >= walls.Count)
					return false;
				Wall w = walls[i], next = walls[(i + 1) % walls.Count];
				return !(w.to.vertex == next.from.vertex || w.to.vertex == next.to.vertex);
			}
			public bool IsWallFlipped(Wall w) => IsWallFlipped(walls.IndexOf(w));
		}

		[System.Serializable]
		public class Wall : IGeometry
		{
			[HideInInspector] public End from, to;
			public bool fill = true;
			[Min(0)] public float thickness = 0.25f;
			public List<Hole> holes;

			public bool overrideMaterials = false;
			[ShowWhen(nameof(overrideMaterials))] public Material leftMaterial, rightMaterial, insetMaterial, outsideMaterial;

			public bool overrideGenerateOutside = false;
			[ShowWhen(nameof(overrideGenerateOutside))] public bool generateOutside = true;

			public Vertex[] GetVertices()
			{
				return new Vertex[2] { from.vertex, to.vertex };
			}

			public float Length => Vector3.Distance(from.Position, to.Position);
			public Vector3 Span => to.Position - from.Position;
			public Vector3 Normal => Vector3.Cross(Span, Vector3.up).normalized;

			public Rect GetHoleArea(Hole h)
			{
				float x = Length * h.x;
				return new()
				{
					xMin = x - h.width * .5f,
					xMax = x + h.width * .5f,
					yMin = h.yMin,
					yMax = h.yMax,
				};
			}

			[System.Serializable]
			public class End
			{
				[SerializeField] public int vertexIndex;
				[System.NonSerialized] public Vertex vertex;
				public float height = 3f;

				public Vector3 Position => vertex.position;
			}

			[System.Serializable]
			public class Hole
			{
				[Range(0, 1)] public float x;
				[Min(0)] public float width;
				[Min(0)] public float yMin, yMax;
				public bool flipped;
				public GameObject filler;
			}
		}

		[System.Serializable]
		public class Vertex : IGeometry
		{
			public Vector3 position;

			public Vertex[] GetVertices()
			{
				return new Vertex[1] { this };
			}
		}

		#region Serialization
		public void OnAfterDeserialize()
		{
			foreach(var room in rooms)
				room.walls = room.wallIndices.Select(i => walls[i]).ToList();

			foreach(var wall in walls)
			{
				wall.from.vertex = vertices[wall.from.vertexIndex];
				wall.to.vertex = vertices[wall.to.vertexIndex];
			}
		}

		public void OnBeforeSerialize()
		{
			// TODO: Performance boost, if turned out to be an issue.

			vertices ??= new();
			walls ??= new();
			rooms ??= new();

			foreach(var room in rooms)
				room.wallIndices = room.walls.Select(wall => walls.IndexOf(wall)).ToList();

			foreach(var wall in walls)
			{
				wall.from.vertexIndex = vertices.IndexOf(wall.from.vertex);
				wall.to.vertexIndex = vertices.IndexOf(wall.to.vertex);
			}
		}
		#endregion
	}
}
