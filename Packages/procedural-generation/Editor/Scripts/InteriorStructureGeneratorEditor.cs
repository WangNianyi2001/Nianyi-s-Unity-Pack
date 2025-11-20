using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nianyi.UnityPack.Editor
{
	using static InteriorStructure;
	using static UnityEditor.PlayerSettings;

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

		IEnumerable<IGeometry> GeometriesOfCurrentType => mode switch
		{
			SelectionMode.Vertices => generator.config.vertices,
			SelectionMode.Walls => generator.config.walls,
			SelectionMode.Rooms => generator.config.rooms,
			_ => throw new System.NotImplementedException(),
		};

		IReadOnlyCollection<IGeometry> CurrentSelection => mode switch
		{
			SelectionMode.Vertices => selectedVertices,
			SelectionMode.Walls => selectedWalls,
			SelectionMode.Rooms => selectedRooms,
			_ => throw new System.NotImplementedException(),
		};

		void SetSelectedState(IGeometry g, bool selected)
		{
			switch(g)
			{
				case Vertex v:
					if(selected)
						selectedVertices.Add(v);
					else
						selectedVertices.Remove(v);
					break;
				case Wall w:
					if(selected)
						selectedWalls.Add(w);
					else
						selectedWalls.Remove(w);
					break;
				case Room r:
					if(selected)
						selectedRooms.Add(r);
					else
						selectedRooms.Remove(r);
					break;
			}
		}

		enum SelectionMode { Vertices, Walls, Rooms }
		SelectionMode mode = SelectionMode.Vertices;
		SelectionMode Mode
		{
			get => mode;
			set
			{
				if(mode == value)
					return;

				var vertices = CurrentSelection.SelectMany(g => g.GetVertices()).ToArray();

				switch(value)
				{
					case SelectionMode.Vertices:
						selectedVertices.Clear();
						foreach(var v in vertices)
							selectedVertices.Add(v);
						break;
					case SelectionMode.Walls:
						selectedWalls.Clear();
						foreach(var w in generator.config.walls)
						{
							if(vertices.Contains(w.from.vertex) && vertices.Contains(w.to.vertex))
								selectedWalls.Add(w);
						}
						break;
					case SelectionMode.Rooms:
						selectedRooms.Clear();
						foreach(var r in generator.config.rooms)
						{
							if(r.GetVertices().All(v => vertices.Contains(v)))
								selectedRooms.Add(r);
						}
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

			foreach(var g in GeometriesOfCurrentType)
				DrawGeometry(g);

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

		void DrawGeometry(IGeometry g)
		{
			Vector3[] vertices = g.GetVertices()
				.Select(p => generator.transform.TransformPoint(p.position))
				.ToArray();
			if(vertices.Length == 0)
				return;

			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;
			bool focused = HandleUtility.nearestControl == id;
			Vector2 mouse = Event.current.mousePosition;

			switch(e.GetTypeForControl(id))
			{
				case EventType.Layout:
					float dist;
					switch(vertices.Length)
					{
						case 1:
							dist = HandleUtility.DistanceToCircle(vertices[0], HandleUtility.GetHandleSize(vertices[0]) * vertexSize);
							break;
						case 2:
							dist = HandleUtility.DistancePointToLine(mouse, vertices[0], vertices[1]);
							break;
						default:
							Vector2[] poly = vertices.Select(p => HandleUtility.WorldToGUIPoint(p)).ToArray();
							dist = MathUtility.DistanceToPolygon(mouse, poly);
							break;
					}
					HandleUtility.AddControl(id, dist);
					break;

				case EventType.MouseDown:
					if(focused && e.button == 0)
					{
						SetSelectedState(g, !CurrentSelection.Contains(g));
						e.Use();
					}
					break;

				case EventType.Repaint:
					Color color = focused ? focusedColor :
						CurrentSelection.Contains(g) ? selectedColor : normalColor;
					if(vertices.Length >= 3)
						color.a *= 0.3f;

					Handles.color = color;


					switch(vertices.Length)
					{
						case 1:
							Handles.SphereHandleCap(id, vertices[0],
								Quaternion.identity,
								HandleUtility.GetHandleSize(vertices[0]) * vertexSize,
								EventType.Repaint
							);
							break;
						case 2:
							Handles.DrawLine(vertices[0], vertices[1], wallThickness);
							break;
						default:
							DrawPolygon(vertices);
							break;
					}
					
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
