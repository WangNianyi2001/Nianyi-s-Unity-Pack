using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack.Editor
{
	using static InteriorStructure;

	[CustomEditor(typeof(InteriorStructureGenerator))]
	public class InteriorStructureGeneratorEditor : ProceduralGeneratorEditor
	{
		#region Configs
		static readonly Color normalColor = Color.red;
		static readonly Color focusedColor = Color.white;
		static readonly Color selectedColor = Color.yellow;

		const float wallThickness = 3f;
		const float vertexSize = .1f;
		#endregion

		#region Status
		InteriorStructureGenerator generator;
		bool isEditing = false;

		#region Selection mode
		readonly HashSet<Vertex> selectedVertices = new();
		readonly HashSet<Wall> selectedWalls = new();
		readonly HashSet<Room> selectedRooms = new();

		enum SelectionMode { Vertices, Walls, Rooms }
		SelectionMode mode = SelectionMode.Vertices;
		SelectionMode Mode
		{
			get => mode;
			set
			{
				if(mode == value)
					return;

				switch(mode)
				{
					case SelectionMode.Vertices:
						switch(value)
						{
							case SelectionMode.Walls:
								selectedWalls.Clear();
								foreach(var w in generator.config.walls)
								{
									if(selectedVertices.Contains(w.from.vertex) && selectedVertices.Contains(w.to.vertex))
										selectedWalls.Add(w);
								}
								break;
							case SelectionMode.Rooms:
								selectedRooms.Clear();
								foreach(var r in generator.config.rooms)
								{
									if(r.Vertices.All(v => selectedVertices.Contains(v)))
										selectedRooms.Add(r);
								}
								break;
						}
						selectedVertices.Clear();
						break;

					case SelectionMode.Walls:
						switch(value)
						{
							case SelectionMode.Vertices:
								selectedVertices.Clear();
								foreach(var w in selectedWalls)
								{
									selectedVertices.Add(w.from.vertex);
									selectedVertices.Add(w.to.vertex);
								}
								break;
							case SelectionMode.Rooms:
								selectedRooms.Clear();
								foreach(var r in generator.config.rooms)
								{
									if(r.walls.All(w => selectedWalls.Contains(w)))
										selectedRooms.Add(r);
								}
								break;
						}
						selectedWalls.Clear();
						break;

					case SelectionMode.Rooms:
						switch(value)
						{
							case SelectionMode.Vertices:
								selectedVertices.Clear();
								foreach(var r in selectedRooms)
								{
									foreach(var w in r.walls)
									{
										selectedVertices.Add(w.from.vertex);
										selectedVertices.Add(w.to.vertex);
									}
								}
								break;
							case SelectionMode.Walls:
								selectedWalls.Clear();
								foreach(var r in selectedRooms)
								{
									foreach(var w in r.walls)
										selectedWalls.Add(w);
								}
								break;
						}
						selectedRooms.Clear();
						break;
				}

				mode = value;
			}
		}
		#endregion

		#endregion

		#region Unity life cycle
		protected void OnEnable()
		{
			if(target as InteriorStructureGenerator != generator)
			{
				isEditing = false;

				generator = target as InteriorStructureGenerator;

				selectedVertices.Clear();
				selectedWalls.Clear();
				selectedRooms.Clear();
			}
		}

		public override void OnInspectorGUI()
		{
			if(!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				if(GUILayout.Button("Toggle Edit Mode", new GUIStyle(GUI.skin.button)
				{
					fontStyle = isEditing ? FontStyle.Bold : FontStyle.Normal,
				}))
				{
					isEditing = !isEditing;
					SceneView.RepaintAll();
				}
			}
			base.OnInspectorGUI();
		}

		protected void OnSceneGUI()
		{
			if(EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			if(target == null || !isEditing)
				return;

			switch(Mode)
			{
				case SelectionMode.Vertices:
					foreach(var v in generator.config.vertices)
						DrawVertex(v);
					break;
				case SelectionMode.Walls:
					foreach(var w in generator.config.walls)
						DrawWall(w);
					break;
				case SelectionMode.Rooms:
					foreach(var r in generator.config.rooms)
						DrawRoom(r);
					break;
			}

			DrawEditPanel(GetEditPanelArea());

			Event e = Event.current;
			switch(e.type)
			{
				case EventType.MouseMove:
					SceneView.RepaintAll();
					break;
			}
		}
		#endregion

		#region GUI drawing
		Rect GetEditPanelArea()
		{
			var sceneWindow = EditorWindow.GetWindow<SceneView>();
			var windowArea = sceneWindow.position.size;

			float width = 300f, height = 250f, margin = 10f;
			return new(
				windowArea.x - width - margin,
				windowArea.y - height - margin,
				width,
				height
			);
		}

		void DrawEditPanel(Rect area)
		{
			Handles.BeginGUI();
			GUILayout.BeginArea(area, GUI.skin.box);

			GUILayout.Label("Interior Structure Edit Panel", new GUIStyle(GUI.skin.label)
			{
				fontStyle = FontStyle.Bold,
				fontSize = Mathf.FloorToInt(GUI.skin.label.fontSize * 1.25f),
			});

			GUILayout.Label("Selection Mode");
			using(new GUILayout.HorizontalScope())
			{
				if(GUILayout.Toggle(Mode == SelectionMode.Vertices, "Vertices"))
					Mode = SelectionMode.Vertices;

				if(GUILayout.Toggle(Mode == SelectionMode.Walls, "Walls"))
					Mode = SelectionMode.Walls;

				if(GUILayout.Toggle(Mode == SelectionMode.Rooms, "Rooms"))
					Mode = SelectionMode.Rooms;
			}

			switch(Mode)
			{
				case SelectionMode.Vertices:
					break;
				case SelectionMode.Walls:
					break;
				case SelectionMode.Rooms:
					break;
			}

			GUILayout.EndArea();
			Handles.EndGUI();
		}

		void DrawVertex(Vertex v)
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;
			bool focused = HandleUtility.nearestControl == id;

			Vector3 pos = generator.transform.TransformPoint(v.position);

			switch(e.GetTypeForControl(id))
			{
				case EventType.Layout:
					float dist = HandleUtility.DistanceToCircle(pos,
						HandleUtility.GetHandleSize(pos) * vertexSize);
					HandleUtility.AddControl(id, dist);
					break;

				case EventType.MouseDown:
					if(focused && e.button == 0)
					{
						if(!selectedVertices.Contains(v))
							selectedVertices.Add(v);
						else
							selectedVertices.Remove(v);

						e.Use();
					}
					break;

				case EventType.Repaint:
					Handles.color = focused ? focusedColor :
						selectedVertices.Contains(v) ? selectedColor : normalColor;

					Handles.SphereHandleCap(id, pos,
						Quaternion.identity,
						HandleUtility.GetHandleSize(pos) * vertexSize,
						EventType.Repaint);
					break;
			}
		}

		void DrawWall(Wall w)
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;
			bool focused = HandleUtility.nearestControl == id;

			Vector3
				fromPos = generator.transform.TransformPoint(w.from.Position),
				toPos = generator.transform.TransformPoint(w.to.Position);

			switch(e.GetTypeForControl(id))
			{
				case EventType.Layout:
					float dist = Mathf.Max(0, HandleUtility.DistanceToLine(fromPos, toPos) - wallThickness + 1);
					HandleUtility.AddControl(id, dist);
					break;

				case EventType.MouseDown:
					if(focused && e.button == 0)
					{
						if(!selectedWalls.Contains(w))
							selectedWalls.Add(w);
						else
							selectedWalls.Remove(w);

						e.Use();
					}
					break;

				case EventType.Repaint:
					Handles.color = focused ? focusedColor :
						selectedWalls.Contains(w) ? selectedColor : normalColor;

					Handles.DrawLine(fromPos, toPos, wallThickness);
					break;
			}
		}

		void DrawRoom(Room r)
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;
			bool focused = HandleUtility.nearestControl == id;

			Vector3[] vertexPositions = r.Vertices.Select(p => generator.transform.TransformPoint(p.position)).ToArray();
			Vector2[] poly = vertexPositions.Select(p => HandleUtility.WorldToGUIPoint(p)).ToArray();
			Vector2 mouse = Event.current.mousePosition;

			switch(e.GetTypeForControl(id))
			{
				case EventType.Layout:
					float dist = MathUtility.DistanceToPolygon(mouse, poly);
					HandleUtility.AddControl(id, dist);
					break;

				case EventType.MouseDown:
					if(focused && e.button == 0)
					{
						if(!selectedRooms.Contains(r))
							selectedRooms.Add(r);
						else
							selectedRooms.Remove(r);

						e.Use();
					}
					break;

				case EventType.Repaint:
					Handles.color = focused ? focusedColor :
						selectedRooms.Contains(r) ? selectedColor : normalColor;
					DrawPolygon(vertexPositions);
					break;
			}
		}
		#endregion

		#region Auxiliary
		static void DrawPolygon(Vector3[] poly)
		{
			DynamicMesh mesh = new();
			mesh.CreateFace(poly);
			mesh.Triangularize();
			foreach(var f in mesh.GetFaces())
				Handles.DrawAAConvexPolygon(f.Vertices.Select(v => v.position).ToArray());
		}
		#endregion
	}
}
