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

		public Wall ConnectVertices(Vertex a, Vertex b)
		{
			// 基本合法性检查。
			if(a == null || b == null)
				return null;
			if(a == b)
			{
				Debug.LogWarning("ConnectVertices: vertices must be distinct.");
				return null;
			}
			if(vertices == null || walls == null)
				return null;
			if(!vertices.Contains(a) || !vertices.Contains(b))
			{
				Debug.LogWarning("ConnectVertices: both vertices must belong to this InteriorStructure.");
				return null;
			}

			// 检查这两个顶点之间是否已经有墙（任意朝向）。
			foreach(var w in walls)
			{
				if(w.from.vertex == null || w.to.vertex == null)
					continue;

				bool forward = w.from.vertex == a && w.to.vertex == b;
				bool backward = w.from.vertex == b && w.to.vertex == a;
				if(forward || backward)
				{
					Debug.LogWarning("ConnectVertices: there is already a wall between the two vertices.");
					return null;
				}
			}

			// 尝试从相邻墙推断高度和厚度。
			const float defaultHeight = 3f;
			const float defaultThickness = 0.25f;

			float ha = defaultHeight;
			float hb = defaultHeight;
			float thickness = defaultThickness;

			var wallsA = FindWallsUsingVertex(a).ToArray();
			var wallsB = FindWallsUsingVertex(b).ToArray();

			if(wallsA.Length > 0)
			{
				var w0 = wallsA[0];
				ha = (w0.from.vertex == a ? w0.from.height : w0.to.height);
				thickness = w0.thickness;
			}

			if(wallsB.Length > 0)
			{
				var w0 = wallsB[0];
				hb = (w0.from.vertex == b ? w0.from.height : w0.to.height);
				if(thickness <= 0f)
					thickness = w0.thickness;
			}

			// 创建新墙，方向就按传入顺序 a -> b。
			Wall newWall = new Wall
			{
				from = new Wall.End
				{
					vertex = a,
					height = ha,
				},
				to = new Wall.End
				{
					vertex = b,
					height = hb,
				},
				fill = true,
				thickness = thickness,
				holes = new List<Wall.Hole>(),
			};

			walls.Add(newWall);
			return newWall;
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

		// GPT generated.
		public bool DissolveWall(Wall w)
		{
			if(w == null || !walls.Contains(w))
				return false;

			// 查这条墙被几个房间用到。
			var roomsUsing = FindRoomsUsingWall(w).ToArray();

			// 情况一：两边都没有房间，直接删墙。
			if(roomsUsing.Length == 0)
			{
				walls.Remove(w);
				PruneVertex(w.from.vertex);
				PruneVertex(w.to.vertex);
				return true;
			}

			// 情况二：恰好两个房间，共用这条墙，尝试合并。
			if(roomsUsing.Length != 2)
				return false;

			Room rA = roomsUsing[0];
			Room rB = roomsUsing[1];

			if(rA.walls == null || rB.walls == null)
				return false;

			// 找出这两个房间之间所有共用的墙（可能不止一条）。
			var shared = rA.walls.Intersect(rB.walls).Distinct().ToList();
			if(shared.Count == 0)
				return false; // 理论上不应该发生

			// 这条被消去的墙一定算在 shared 里，如果不在就补上。
			if(!shared.Contains(w))
				shared.Add(w);

			// 被选中的这条墙从全局 walls 里直接删除；
			// 其他共用墙只从两个房间的 walls 里去掉，留下来当“孤墙”。
			walls.Remove(w);

			foreach(var s in shared)
			{
				rA.walls.Remove(s);
				rB.walls.Remove(s);
			}

			// 合并后的新房间边界 = 两个房间的墙并集，减去所有共用边。
			var boundaryWalls = rA.walls
				.Concat(rB.walls)
				.Distinct()
				.ToList();

			if(boundaryWalls.Count < 3)
			{
				// 退化情况：合并后已经不能构成房间了，
				// 那就当这俩房间都被删掉。
				rooms.Remove(rA);
				rooms.Remove(rB);

				PruneVertex(w.from.vertex);
				PruneVertex(w.to.vertex);

				return true;
			}

			// 用“边-顶点”邻接关系，把 boundaryWalls 重新串成一个闭合环。
			// 假定拓扑是正常的室内平面：边界图是一条简单闭合曲线。

			Dictionary<Vertex, List<Wall>> adj = new();

			void AddAdj(Vertex v, Wall e)
			{
				if(!adj.TryGetValue(v, out var list))
				{
					list = new List<Wall>();
					adj[v] = list;
				}
				list.Add(e);
			}

			foreach(var e in boundaryWalls)
			{
				AddAdj(e.from.vertex, e);
				AddAdj(e.to.vertex, e);
			}

			// 简单 sanity check：理想情况每个边界顶点度数为 2。
			foreach(var kv in adj)
			{
				if(kv.Value.Count != 2)
				{
					Debug.LogWarning("DissolveWall: boundary graph is not a simple cycle; result may be invalid.");
					break;
				}
			}

			HashSet<Wall> used = new();
			List<Wall> loop = new();

			Wall cur = boundaryWalls[0];
			Vertex startV = cur.from.vertex;
			Vertex curV = startV;

			for(int steps = 0; steps < boundaryWalls.Count * 2; ++steps)
			{
				loop.Add(cur);
				used.Add(cur);

				// 当前边在当前点的另一端。
				Vertex nextV = (cur.from.vertex == curV) ? cur.to.vertex : cur.from.vertex;

				// 在 nextV 上找未用过的下一条边。
				if(!adj.TryGetValue(nextV, out var listNext) || listNext.Count == 0)
					break;

				Wall next = null;
				foreach(var e in listNext)
				{
					if(!used.Contains(e))
					{
						next = e;
						break;
					}
				}

				if(next == null)
				{
					// 没有未用边了，如果回到起点，就算建成一个闭环。
					if(nextV == startV)
						break;
					else
						break;
				}

				curV = nextV;
				cur = next;
			}

			if(used.Count != boundaryWalls.Count)
				Debug.LogWarning("DissolveWall: could not cover all boundary walls with a single closed loop.");

			// 手动修复：墙是反的。
			loop.Reverse();

			// 创建新房间，属性做个并集（你也可以按自己语义选 rA 或 rB）。
			Room newRoom = new Room
			{
				generateFloor = rA.generateFloor || rB.generateFloor,
				generateCeiling = rA.generateCeiling || rB.generateCeiling,
				walls = loop,
			};

			// 把旧房间摘掉，挂上新房间。
			rooms.Remove(rA);
			rooms.Remove(rB);
			rooms.Add(newRoom);

			// 删掉这条墙之后，如果两个端点不再被任何墙使用，会在 PruneVertex 里被清掉。
			PruneVertex(w.from.vertex);
			PruneVertex(w.to.vertex);

			return true;
		}

		// GPT generated.
		public Room CreateRoomFromWalls(IEnumerable<Wall> selected)
		{
			if(selected == null)
				return null;

			if(walls == null)
				walls = new List<Wall>();
			if(rooms == null)
				rooms = new List<Room>();

			// 去重 + 列表化。
			List<Wall> input = selected.Distinct().ToList();
			if(input.Count < 3)
			{
				Debug.LogWarning("CreateRoomFromWalls: need at least 3 walls.");
				return null;
			}

			// 必须全部属于当前结构。
			foreach(var w in input)
			{
				if(w == null || !walls.Contains(w))
				{
					Debug.LogWarning("CreateRoomFromWalls: a wall is null or not in this InteriorStructure.");
					return null;
				}
			}

			// 建顶点邻接表，只考虑选中的墙。
			Dictionary<Vertex, List<Wall>> adj = new Dictionary<Vertex, List<Wall>>();

			void AddAdj(Vertex v, Wall e)
			{
				if(v == null)
					return;
				if(!adj.TryGetValue(v, out var list))
				{
					list = new List<Wall>();
					adj[v] = list;
				}
				list.Add(e);
			}

			foreach(var e in input)
			{
				AddAdj(e.from.vertex, e);
				AddAdj(e.to.vertex, e);
			}

			// 简单环拓扑：每个顶点度数应为 2。
			foreach(var kv in adj)
			{
				if(kv.Value.Count != 2)
				{
					Debug.LogWarning("CreateRoomFromWalls: selected walls do not form a simple closed loop.");
					return null;
				}
			}

			// 按邻接关系排出一个闭合环（walls 顺序 + 顶点顺序）。
			List<Wall> loopWalls = new List<Wall>();
			List<Vertex> loopVertices = new List<Vertex>();
			HashSet<Wall> used = new HashSet<Wall>();

			Wall startWall = input[0];
			Vertex startV = startWall.from.vertex;
			Vertex curV = startV;
			Wall cur = startWall;

			int maxSteps = input.Count * 2;
			for(int step = 0; step < maxSteps; ++step)
			{
				loopWalls.Add(cur);
				used.Add(cur);

				// 记录当前顶点。
				loopVertices.Add(curV);

				// 找到当前边的另一端。
				Vertex nextV = (cur.from.vertex == curV) ? cur.to.vertex : cur.from.vertex;

				// 如果所有边都用完且回到了起点，则闭环成功。
				if(used.Count == input.Count && nextV == startV)
					break;

				// 否则在 nextV 处找下一条未用边。
				if(!adj.TryGetValue(nextV, out var listNext))
				{
					Debug.LogWarning("CreateRoomFromWalls: adjacency broken.");
					return null;
				}

				Wall next = null;
				foreach(var e in listNext)
				{
					if(!used.Contains(e))
					{
						next = e;
						break;
					}
				}

				if(next == null)
				{
					Debug.LogWarning("CreateRoomFromWalls: cannot traverse a full loop.");
					return null;
				}

				curV = nextV;
				cur = next;
			}

			if(used.Count != input.Count)
			{
				Debug.LogWarning("CreateRoomFromWalls: not all selected walls are used in a single cycle.");
				return null;
			}

			// loopWalls.Count == input.Count，loopVertices.Count == input.Count。

			// 计算一个现有房间的绕序符号，作为全局基准。
			float baseSign = 0f;
			foreach(var r in rooms)
			{
				var vs = r.GetVertices();
				if(vs == null || vs.Length < 3)
					continue;

				float s = 0f;
				for(int i = 0; i < vs.Length; ++i)
				{
					int j = (i + 1) % vs.Length;
					Vector3 p = vs[i].position;
					Vector3 q = vs[j].position;
					s += p.x * q.z - q.x * p.z;
				}
				if(Mathf.Abs(s) > Mathf.Epsilon)
				{
					baseSign = Mathf.Sign(s);
					break;
				}
			}

			// 计算新环的绕序符号（按 XZ）。
			float loopSign = 0f;
			if(loopVertices.Count >= 3)
			{
				for(int i = 0; i < loopVertices.Count; ++i)
				{
					int j = (i + 1) % loopVertices.Count;
					Vector3 p = loopVertices[i].position;
					Vector3 q = loopVertices[j].position;
					loopSign += p.x * q.z - q.x * p.z;
				}
			}

			// 若已有基准，且新环方向相反，则翻转一次，使其与全局一致。
			if(baseSign != 0f && Mathf.Abs(loopSign) > Mathf.Epsilon && Mathf.Sign(loopSign) != Mathf.Sign(baseSign))
			{
				loopWalls.Reverse();
				loopVertices.Reverse();
			}

			// 临时构造一个房间对象，用 Room.IsWallFlipped 来判断新房间中每条墙的走向。
			Room tempRoom = new Room
			{
				generateFloor = true,
				generateCeiling = true,
				walls = new List<Wall>(loopWalls),
			};

			// 检查与现有房间是否重叠：
			// 若存在某条墙，在新房间与某个已有房间中的走向相同，则视为重叠。
			for(int i = 0; i < loopWalls.Count; ++i)
			{
				Wall w = loopWalls[i];
				bool flippedNew = tempRoom.IsWallFlipped(i);
				int dirNew = flippedNew ? -1 : 1;

				// 找出所有使用这条墙的已有房间。
				foreach(var r in FindRoomsUsingWall(w))
				{
					int idx = r.walls.IndexOf(w);
					if(idx < 0)
						continue;

					bool flippedOld = r.IsWallFlipped(idx);
					int dirOld = flippedOld ? -1 : 1;

					if(dirNew == dirOld)
					{
						Debug.LogWarning("CreateRoomFromWalls: new room would overlap an existing room (same wall orientation).");
						return null;
					}
				}
			}

			// 通过所有检查，正式创建房间。
			Room newRoom = new Room
			{
				generateFloor = true,
				generateCeiling = true,
				walls = loopWalls,
			};

			rooms.Add(newRoom);
			return newRoom;
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
