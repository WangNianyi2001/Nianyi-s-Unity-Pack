using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public class InteriorStructure : ISerializationCallbackReceiver
	{
		public List<Vertex> vertices;
		public List<Wall> walls;
		public List<Room> rooms;

		[System.Serializable]
		public class Room
		{
			[SerializeField] public List<int> wallIndices;
			[System.NonSerialized] public List<Wall> walls;
		}

		[System.Serializable]
		public class Wall
		{
			public End from, to;
			public bool fill = true;

			[System.Serializable]
			public class End
			{
				[SerializeField] public int vertexIndex;
				[System.NonSerialized] public Vertex vertex;
				public float height = 3f;
				public float thickness = 0.1f;
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
