using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Nianyi.UnityPack.InteriorStructure;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public class InteriorStructure : ISerializationCallbackReceiver
	{
		[Header("Materials")]
		public Material defaultFloorMaterial;
		public Material defaultCeilingMaterial;
		public Material defaultWallMaterial;
		public Material defaultInsetMaterial;

		[Header("Geometry")]
		public List<Vertex> vertices;
		public List<Wall> walls;
		public List<Room> rooms;

		[System.Serializable]
		public class Room
		{
			public bool generateFloor = true;
			public bool generateCeiling = true;

			[SerializeField] public List<int> wallIndices;
			[System.NonSerialized] public List<Wall> walls;

			public Vertex[] Vertices
			{
				get
				{
					Vertex[] vertices = new Vertex[walls.Count];
					for(int i = 0; i < walls.Count; ++i)
					{
						var w = walls[i];
						vertices[i] = (IsWallFlipped(i) ? w.to : w.from).vertex;
					}
					return vertices;
				}
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
		public class Wall
		{
			public End from, to;
			public bool fill = true;
			[Min(0)] public float thickness = 0.25f;
			public List<Hole> holes;

			[System.Serializable]
			public class End
			{
				[SerializeField] public int vertexIndex;
				[System.NonSerialized] public Vertex vertex;
				public float height = 3f;

				public Vector3 Position => vertex.position;
			}

			[System.Serializable]
			public struct Hole
			{
				public Rect area;
			}
		}

		[System.Serializable]
		public class Vertex
		{
			public Vector3 position;
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
