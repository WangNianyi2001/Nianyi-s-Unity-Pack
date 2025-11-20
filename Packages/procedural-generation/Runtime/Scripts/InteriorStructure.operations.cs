using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack
{
	public partial class InteriorStructure
	{
		#region Querying

		public IEnumerable<Wall> FindWallsUsingVertex(Vertex v)
		{
			if(!vertices.Contains(v))
				yield break;
			foreach(var w in walls)
			{
				if(w.from.vertex == v || w.to.vertex == v)
				{
					yield return w;
					continue;
				}
			}
		}

		public IEnumerable<Room> FindRoomsUsingVertex(Vertex v)
		{
			if(!vertices.Contains(v))
				yield break;
			foreach(var r in rooms)
			{
				foreach(var w in r.walls)
				{
					if(w.from.vertex == v || w.to.vertex == v)
					{
						yield return r;
						break;
					}
				}
			}
		}

		public IEnumerable<Room> FindRoomsUsingWall(Wall w)
		{
			if(!walls.Contains(w))
				yield break;
			foreach(var r in rooms)
			{
				foreach(var _w in r.walls)
				{
					if(_w == w)
					{
						yield return r;
						break;
					}
				}
			}
		}

		#endregion


		#region General

		// GPT generated.
		public void CreateDefaultRoom()
		{
			Vertex v0 = new() { position = new Vector3(-5f, 0f, -5f) };
			Vertex v1 = new() { position = new Vector3(-5f, 0f, 5f) };
			Vertex v2 = new() { position = new Vector3(5f, 0f, 5f) };
			Vertex v3 = new() { position = new Vector3(5f, 0f, -5f) };

			vertices.Add(v0);
			vertices.Add(v1);
			vertices.Add(v2);
			vertices.Add(v3);

			float wallHeight = 3f;

			Wall MakeWall(Vertex a, Vertex b, List<Wall.Hole> hs = null)
			{
				return new Wall
				{
					from = new()
					{
						vertex = a,
						height = wallHeight,
					},
					to = new()
					{
						vertex = b,
						height = wallHeight,
					},
					fill = true,
					thickness = 0.25f,
					holes = hs ?? new(),
				};
			}

			Wall w0 = MakeWall(v0, v1);
			Wall w1 = MakeWall(v1, v2);
			Wall w2 = MakeWall(v2, v3);
			Wall w3 = MakeWall(v3, v0);

			Wall.Hole door = new()
			{
				x = 0.5f,
				width = 1f,
				yMin = 0f,
				yMax = 2f,
				flipped = false,
				filler = null,
			};
			w1.holes.Add(door);

			walls.Add(w0);
			walls.Add(w1);
			walls.Add(w2);
			walls.Add(w3);

			Room room = new()
			{
				generateFloor = true,
				generateCeiling = true,
				walls = new List<Wall> { w0, w1, w2, w3 },
			};

			rooms.Add(room);
		}

		public void PruneGeometries()
		{
			foreach(var v in vertices.ToArray())
				PruneVertex(v);
			foreach(var w in walls.ToArray())
				PruneWall(w);
			foreach(var r in rooms.ToArray())
				PruneRoom(r);
		}

		#endregion


		#region Vertex

		public bool DissolveVertex(Vertex v)
		{
			if(!vertices.Contains(v))
			{
				Debug.LogWarning("The vertex to be dissolved is not in this structure.");
				return false;
			}

			var ab = FindWallsUsingVertex(v).ToArray();
			if(ab.Length != 2)
			{
				if(ab.Length == 0)
				{
					DeleteVertex(v);
					return true;
				}
				Debug.LogWarning($"The vertex to be dissolved must have exactly 2 walls using it. Actual = {ab.Length}");
				return false;
			}
			Wall a = ab[0], b = ab[1];

			Room[] ra = FindRoomsUsingWall(a).ToArray(), rb = FindRoomsUsingWall(b).ToArray();
			Room[] rooms = ra.Union(rb).ToArray();
			if(rooms.Length != ra.Length || rooms.Length != rb.Length)
			{
				Debug.LogWarning("The walls using the vertex have different number of rooms using them.");
				return false;
			}

			if(a.to.vertex != v)
				InvertWall(a);
			if(b.from.vertex != v)
				InvertWall(b);

			float al = a.Length, bl = b.Length, m = al / (al + bl);

			Wall w = new()
			{
				from = a.from,
				to = b.to,
				holes = new(),
			};
			w.holes.AddRange(a.holes.Select(h =>
			{
				h.x *= m;
				return h;
			}));
			w.holes.AddRange(b.holes.Select(h =>
			{
				h.x = m + h.x * (1 - m);
				return h;
			}));
			walls.Add(w);

			foreach(var r in rooms)
			{
				var walls = r.walls;
				walls.Insert(walls.IndexOf(a), w);
				walls.Remove(a);
				walls.Remove(b);
				PruneRoom(r);
			}
			walls.Remove(a);
			walls.Remove(b);

			vertices.Remove(v);

			return true;
		}

		public void DeleteVertex(Vertex v)
		{
			Room[] rooms = FindRoomsUsingVertex(v).ToArray();

			vertices.Remove(v);

			foreach(var r in rooms)
				DeleteRoom(r);
		}

		void PruneVertex(Vertex v)
		{
			if(FindWallsUsingVertex(v).Count() > 0)
				return;
			vertices.Remove(v);
		}

		#endregion


		#region Wall

		void PruneWall(Wall w)
		{
			if(FindRoomsUsingWall(w).Count() != 0)
				return;

			walls.Remove(w);
			PruneVertex(w.from.vertex);
			PruneVertex(w.to.vertex);
		}

		public Wall[] SubdivideWall(Wall w, IEnumerable<float> xs)
		{
			var _xs = xs.Where(x => x > 0 && x < 1).Distinct().ToList();
			_xs.Sort();
			List<Wall> walls = new();
			float prev = 0;
			foreach(var x in _xs)
			{
				var (a, b) = SubdivideWall(w, (x - prev) / (1 - prev));
				prev = x;
				walls.Add(a);
				w = b;
			}
			return walls.ToArray();
		}

		public void SubdivideWallEvenly(Wall w, int n)
		{
			SubdivideWall(w, Enumerable.Range(1, n).Select(i => (float)i / (n + 1)));
		}

		// GPT generated.
		(Wall, Wall) SubdivideWall(Wall w, float x)
		{
			// 基础合法性
			if(x <= 0f || x >= 1f)
				return (w, null);
			if(!walls.Contains(w))
				return (w, null);

			// 1. 先记录所有用到这堵墙的房间，以及在房间里的索引和是否 flipped
			var roomInfos = new List<(Room room, int index, bool flipped)>();
			foreach(var r in FindRoomsUsingWall(w).ToArray())
			{
				if(r.walls == null)
					continue;
				int idx = r.walls.IndexOf(w);
				if(idx < 0)
					continue;

				bool flipped = r.IsWallFlipped(idx);
				roomInfos.Add((r, idx, flipped));
			}

			// 2. 计算中点顶点与高度
			Vertex vFrom = w.from.vertex;
			Vertex vTo = w.to.vertex;

			Vector3 p0 = vFrom.position;
			Vector3 p1 = vTo.position;
			Vector3 midPos = Vector3.Lerp(p0, p1, x);
			float midHeight = Mathf.Lerp(w.from.height, w.to.height, x);

			Vertex midVertex = new Vertex
			{
				position = midPos,
			};
			vertices.Add(midVertex);

			// 缓存旧的 to 端
			Wall.End oldTo = w.to;

			// 3. 左段：直接修改原墙 w，使其变成 from → mid
			w.to = new Wall.End
			{
				vertex = midVertex,
				height = midHeight,
			};

			// 右段：mid → oldTo
			Wall right = new Wall
			{
				from = new Wall.End
				{
					vertex = midVertex,
					height = midHeight,
				},
				to = new Wall.End
				{
					vertex = oldTo.vertex,
					height = oldTo.height,
				},
				fill = w.fill,
				thickness = w.thickness,
				holes = new List<Wall.Hole>(),
			};

			// 4. 按比例拆分洞（Hole.x 为 0–1）
			var leftHoles = new List<Wall.Hole>();
			var rightHoles = new List<Wall.Hole>();

			if(w.holes != null)
			{
				foreach(var h in w.holes)
				{
					Wall.Hole CloneWithX(float newX)
					{
						return new Wall.Hole
						{
							x = newX,
							width = h.width,
							yMin = h.yMin,
							yMax = h.yMax,
							flipped = h.flipped,
							filler = h.filler,
						};
					}

					if(h.x <= x)
					{
						float newX = h.x / x;
						leftHoles.Add(CloneWithX(newX));
					}
					else
					{
						float newX = (h.x - x) / (1f - x);
						rightHoles.Add(CloneWithX(newX));
					}
				}
			}

			w.holes = leftHoles;
			right.holes = rightHoles;

			// 5. 把右半段加入全局墙列表（紧跟在 w 后面）
			int wi = walls.IndexOf(w);
			if(wi >= 0)
				walls.Insert(wi + 1, right);
			else
				walls.Add(right);

			// 6. 更新各房间里的墙顺序：
			//    - 若房间里 w 未 flipped： [..., w, next...] -> [..., w, right, next...]
			//    - 若房间里 w flipped：    [..., prev, w, ...]（逻辑方向 to→from），
			//      希望变成 [..., prev, right, w, ...]（房间看到的是 right 反着走、再 w 反着走）。
			foreach(var info in roomInfos)
			{
				var room = info.room;
				var list = room.walls;
				int idx = list.IndexOf(w);
				if(idx < 0)
					continue;

				if(info.flipped)
					list.Insert(idx, right);    // flipped：right 在前
				else
					list.Insert(idx + 1, right); // 未 flipped：right 在后
			}

			return (w, right);
		}

		public void InvertWall(Wall w)
		{
			if(!walls.Contains(w))
				return;

			foreach(var h in w.holes)
			{
				h.x = 1 - h.x;
				h.flipped = !h.flipped;
			}

			(w.from, w.to) = (w.to, w.from);
		}

		public void DeleteWall(Wall w)
		{
			if(!walls.Contains(w))
				return;

			foreach(var r in FindRoomsUsingWall(w).ToArray())
				DeleteRoom(r);

			PruneWall(w);
		}

		#endregion


		#region Room

		public void DeleteRoom(Room r)
		{
			rooms.Remove(r);
			foreach(var w in r.walls)
				PruneWall(w);
		}

		public void PruneRoom(Room r)
		{
			if(!rooms.Contains(r))
				return;

			if(r.walls.Count < 3)
				DeleteRoom(r);
		}

		// GPT generated.
		public Wall[] ExtrudeWalls(Wall[] selectedWalls, Vector3 displacement)
		{
			// 基本有效性检查
			if(selectedWalls == null || selectedWalls.Length == 0)
			{
				Debug.LogWarning("Extrude: No walls selected.");
				return System.Array.Empty<Wall>();
			}

			if(walls == null || rooms == null)
			{
				Debug.LogWarning("Extrude: InteriorStructure is not initialized.");
				return System.Array.Empty<Wall>();
			}

			// 去重 + 全部在当前结构中
			Wall[] sel = selectedWalls.Distinct().ToArray();
			if(sel.Length != selectedWalls.Length)
				Debug.LogWarning("Extrude: Duplicate walls in selection, using distinct set.");

			foreach(var w in sel)
			{
				if(!walls.Contains(w))
				{
					Debug.LogWarning("Extrude: Selected wall not in this InteriorStructure.");
					return System.Array.Empty<Wall>();
				}
			}

			// 找一个包含所有选中墙段的房间，作为基准房间
			Room baseRoom = null;
			foreach(var r in rooms)
			{
				if(r.walls == null)
					continue;
				bool allIn = true;
				foreach(var w in sel)
				{
					if(!r.walls.Contains(w))
					{
						allIn = false;
						break;
					}
				}
				if(allIn)
				{
					baseRoom = r;
					break;
				}
			}

			if(baseRoom == null)
			{
				Debug.LogWarning("Extrude: Selected walls do not belong to a single room.");
				return System.Array.Empty<Wall>();
			}

			// 检查这些墙在基准房间里是否构成连续的一段（不允许绕回）
			List<Wall> ordered = new();
			List<Wall> roomWalls = baseRoom.walls;

			int start = -1;
			for(int i = 0; i < roomWalls.Count; ++i)
			{
				if(sel.Contains(roomWalls[i]))
				{
					start = i;
					break;
				}
			}

			if(start < 0)
			{
				Debug.LogWarning("Extrude: Selected walls not found in base room.");
				return System.Array.Empty<Wall>();
			}

			int idx = start;
			while(idx < roomWalls.Count && sel.Contains(roomWalls[idx]))
			{
				ordered.Add(roomWalls[idx]);
				++idx;
			}

			if(ordered.Count != sel.Length)
			{
				Debug.LogWarning("Extrude: Selected walls must be contiguous in the base room (no wrap-around).");
				return System.Array.Empty<Wall>();
			}

			int n = ordered.Count;
			if(n == 0)
				return System.Array.Empty<Wall>();

			// 抽取这一段上的顶点序列 v0, v1, ..., vn（按照房间方向）
			List<Vertex> chainVertices = new();

			for(int i = 0; i < n; ++i)
			{
				Wall w = ordered[i];
				int wi = roomWalls.IndexOf(w);
				if(wi < 0)
				{
					Debug.LogWarning("Extrude: Inconsistent room wall list.");
					return System.Array.Empty<Wall>();
				}

				bool flipped = baseRoom.IsWallFlipped(wi);
				Vertex vFrom = flipped ? w.to.vertex : w.from.vertex;
				Vertex vTo = flipped ? w.from.vertex : w.to.vertex;

				if(i == 0)
					chainVertices.Add(vFrom);
				chainVertices.Add(vTo);
			}

			// 不允许闭合（否则是整个房间一圈，逻辑复杂得多）
			if(chainVertices[0] == chainVertices[chainVertices.Count - 1])
			{
				Debug.LogWarning("Extrude: Selected walls form a closed loop; this case is not supported by this Extrude implementation.");
				return System.Array.Empty<Wall>();
			}

			// 顶点高度字典：以原墙的 End.height 为准，冲突时简单取平均
			Dictionary<Vertex, float> vertexHeights = new();
			void AccHeight(Vertex v, float h)
			{
				if(vertexHeights.TryGetValue(v, out float old))
					vertexHeights[v] = 0.5f * (old + h);
				else
					vertexHeights[v] = h;
			}

			foreach(var w in ordered)
			{
				if(w.from != null && w.from.vertex != null)
					AccHeight(w.from.vertex, w.from.height);
				if(w.to != null && w.to.vertex != null)
					AccHeight(w.to.vertex, w.to.height);
			}

			// 为链上的每个顶点生成偏移后的新顶点
			if(vertices == null)
				vertices = new List<Vertex>();

			Dictionary<Vertex, Vertex> vMap = new();
			foreach(var v in chainVertices.Distinct())
			{
				Vertex nv = new Vertex
				{
					position = v.position + displacement,
				};
				vertices.Add(nv);
				vMap[v] = nv;
			}

			float GetHeight(Vertex v)
			{
				if(vertexHeights.TryGetValue(v, out float h))
					return h;
				return 3f;
			}

			if(walls == null)
				walls = new List<Wall>();

			List<Wall> offsetWalls = new();

			// 1. 对应链上的每条边创建一条偏移边（外侧墙）
			for(int i = 0; i < n; ++i)
			{
				Vertex v0 = chainVertices[i];
				Vertex v1 = chainVertices[i + 1];
				Vertex nv0 = vMap[v0];
				Vertex nv1 = vMap[v1];

				float h0 = GetHeight(v0);
				float h1 = GetHeight(v1);

				Wall src = ordered[i];

				Wall offset = new Wall
				{
					from = new Wall.End
					{
						vertex = nv0,
						height = h0,
					},
					to = new Wall.End
					{
						vertex = nv1,
						height = h1,
					},
					fill = src.fill,
					thickness = src.thickness,
					holes = new List<Wall.Hole>(),
				};

				walls.Add(offset);
				offsetWalls.Add(offset);
			}

			// 2. 两个“封口”墙：链两端与偏移后的端点之间
			Vertex vStart = chainVertices[0];
			Vertex vEnd = chainVertices[chainVertices.Count - 1];
			Vertex nvStart = vMap[vStart];
			Vertex nvEnd = vMap[vEnd];

			float hStart = GetHeight(vStart);
			float hEnd = GetHeight(vEnd);

			Wall capEnd = new Wall
			{
				from = new Wall.End
				{
					vertex = vEnd,
					height = hEnd,
				},
				to = new Wall.End
				{
					vertex = nvEnd,
					height = hEnd,
				},
				fill = true,
				thickness = ordered[0].thickness,
				holes = new List<Wall.Hole>(),
			};

			Wall capStart = new Wall
			{
				from = new Wall.End
				{
					vertex = nvStart,
					height = hStart,
				},
				to = new Wall.End
				{
					vertex = vStart,
					height = hStart,
				},
				fill = true,
				thickness = ordered[0].thickness,
				holes = new List<Wall.Hole>(),
			};

			walls.Add(capEnd);
			walls.Add(capStart);

			// 3. 创建新房间：边界按「原链 → 尾封口 → 反向偏移链 → 头封口」的顺序
			Room newRoom = new Room
			{
				generateFloor = baseRoom.generateFloor,
				generateCeiling = baseRoom.generateCeiling,
				walls = new List<Wall>(),
			};

			// 原始选中墙作为新房间的一边
			foreach(var w in ordered)
				newRoom.walls.Add(w);

			// 尾封口
			newRoom.walls.Add(capEnd);

			// 偏移链反向
			for(int i = offsetWalls.Count - 1; i >= 0; --i)
				newRoom.walls.Add(offsetWalls[i]);

			// 头封口
			newRoom.walls.Add(capStart);

			// 手动修 bug：房间顺序反了。
			newRoom.walls.Reverse();

			rooms.Add(newRoom);

			return offsetWalls.ToArray();
		}
		#endregion
	}
}
