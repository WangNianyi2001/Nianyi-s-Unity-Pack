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
		InteriorStructure Structure => generator.config;
		bool isEditing = false;

		#region Selection mode
		readonly HashSet<Vertex> selectedVertices = new();
		readonly HashSet<Wall> selectedWalls = new();
		readonly HashSet<Room> selectedRooms = new();

		IEnumerable<IGeometry> GeometriesOfCurrentType => mode switch
		{
			SelectionMode.Vertices => Structure.vertices,
			SelectionMode.Walls => Structure.walls,
			SelectionMode.Rooms => Structure.rooms,
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
			TriggerInspectorRefresh();
		}

		void ClearSelection()
		{
			selectedVertices.Clear();
			selectedWalls.Clear();
			selectedRooms.Clear();
			TriggerInspectorRefresh();
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
				ClearSelection();

				switch(value)
				{
					case SelectionMode.Vertices:
						foreach(var v in vertices)
							selectedVertices.Add(v);
						break;
					case SelectionMode.Walls:
						foreach(var w in Structure.walls)
						{
							if(vertices.Contains(w.from.vertex) && vertices.Contains(w.to.vertex))
								selectedWalls.Add(w);
						}
						break;
					case SelectionMode.Rooms:
						foreach(var r in Structure.rooms)
						{
							if(r.GetVertices().All(v => vertices.Contains(v)))
								selectedRooms.Add(r);
						}
						break;
				}

				mode = value;
			}
		}

		Vertex[] GetSelectedVertices()
		{
			return CurrentSelection
				.SelectMany(g => g.GetVertices())
				.Distinct()
				.ToArray();
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
				ClearSelection();
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

				if(isEditing)
				{
					DrawEditGui();
					EditorGUILayout.Space();
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

			// Hide native controls.
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			foreach(var g in GeometriesOfCurrentType)
				DrawGeometry(g);

			DrawMoveControl();

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
		int subdivisionCount;

		void DrawEditGui()
		{
			// Header
			GUILayout.Label("Edit", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, });

			// Selection
			GUILayout.Label("Selection mode");
			using(new GUILayout.HorizontalScope())
			{
				if(GUILayout.Toggle(Mode == SelectionMode.Vertices, "Vertices"))
					Mode = SelectionMode.Vertices;
				if(GUILayout.Toggle(Mode == SelectionMode.Walls, "Walls"))
					Mode = SelectionMode.Walls;
				if(GUILayout.Toggle(Mode == SelectionMode.Rooms, "Rooms"))
					Mode = SelectionMode.Rooms;
			}

			// Selection-ex
			if(Structure.vertices.Count == 0)
			{
				if(GUILayout.Button("Create default room"))
				{
					RecordBeforeUndo("Create default room");
					Structure.CreateDefaultRoom();
					ReportChange();
				}
			}
			else
			{
				if(GUILayout.Button("Prune geometries"))
				{
					RecordBeforeUndo("Prune geometries");
					Structure.PruneGeometries();
					ReportChange();
				}
			}

			// Mode-specific
			switch(Mode)
			{
				case SelectionMode.Vertices:
					DrawVertexGui();
					break;
				case SelectionMode.Walls:
					DrawWallGui();
					break;
				case SelectionMode.Rooms:
					DrawRoomGui();
					break;
			}
		}

		void DrawVertexGui()
		{
			if(GUILayout.Button("Dissolve vertices"))
			{
				RecordBeforeUndo("Dissolve vertices");
				foreach(var v in selectedVertices)
					Structure.DissolveVertex(v);
				ReportChange();
			}
			if(GUILayout.Button("Delete vertices"))
			{
				if(EditorUtility.DisplayDialog("Confirm", "Delete vertices?", "Delete", "Cancel"))
				{
					RecordBeforeUndo("Delete vertices");
					foreach(var v in selectedVertices)
						Structure.DeleteVertex(v);
					ReportChange();
				}
			}
			if(selectedVertices.Count == 2)
			{
				if(GUILayout.Button("Connect vertices"))
				{
					RecordBeforeUndo("Connect vertices");
					Vertex[] vertices = selectedVertices.ToArray();
					Structure.ConnectVertices(vertices[0], vertices[1]);
					ReportChange();
				}
			}
		}

		void DrawWallGui()
		{
			if(GUILayout.Button("Extrude walls"))
			{
				RecordBeforeUndo("Extrude walls");
				var extruded = Structure.ExtrudeWalls(selectedWalls.ToArray(), default);
				ClearSelection();
				foreach(var w in extruded)
					selectedWalls.Add(w);
				ReportChange();
			}
			if(GUILayout.Button("Dissolve walls"))
			{
				RecordBeforeUndo("Dissolve walls");
				foreach(var w in selectedWalls)
					Structure.DissolveWall(w);
				ReportChange();
			}
			if(GUILayout.Button("Delete walls"))
			{
				if(EditorUtility.DisplayDialog("Confirm", "Delete walls?", "Delete", "Cancel"))
				{
					RecordBeforeUndo("Delete walls");
					foreach(var w in selectedWalls)
						Structure.DeleteWall(w);
					ReportChange();
				}
			}
			using(new GUILayout.HorizontalScope())
			{
				subdivisionCount = Mathf.Max(1, EditorGUILayout.IntField(subdivisionCount));
				if(GUILayout.Button("Subdvide"))
				{
					RecordBeforeUndo("Subdivide walls");
					foreach(var w in selectedWalls)
						Structure.SubdivideWallEvenly(w, subdivisionCount);
					ReportChange();
				}
			}

			if(selectedWalls.Count >= 3)
			{
				if(GUILayout.Button("Make room"))
				{
					RecordBeforeUndo("Make room");
					var w = Structure.CreateRoomFromWalls(selectedWalls);
					if(w != null)
					{
						ClearSelection();
						selectedRooms.Add(w);
						Mode = SelectionMode.Rooms;
						ReportChange();
					}
				}
			}

			// Inspection.
			if(selectedWalls.Count == 1)
			{
				var w = selectedWalls.First();
				var index = Structure.walls.IndexOf(w);
				var property = serializedObject.FindProperty($"config.walls.Array.data[{index}]");
				if(property != null)
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(property, new GUIContent("Wall Properties"), true);
					if(EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}
			}
		}

		void DrawRoomGui()
		{
			if(GUILayout.Button("Delete rooms"))
			{
				if(EditorUtility.DisplayDialog("Confirm", "Delete rooms?", "Delete", "Cancel"))
				{
					RecordBeforeUndo("Delete rooms");
					foreach(var r in selectedRooms)
						Structure.DeleteRoom(r);
					ReportChange();
				}
			}

			// Inspection.
			if(selectedRooms.Count == 1)
			{
				var r = selectedRooms.First();
				var index = Structure.rooms.IndexOf(r);
				var property = serializedObject.FindProperty($"config.rooms.Array.data[{index}]");
				if(property != null)
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(property, new GUIContent("Room Properties"), true);
					if(EditorGUI.EndChangeCheck())
						serializedObject.ApplyModifiedProperties();
				}
			}
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
							dist = Mathf.Max(0, HandleUtility.DistanceToLine(vertices[0], vertices[1]) - wallThickness + 1);
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
						if(!Event.current.shift)
						{
							// Individual selection.
							foreach(var selected in CurrentSelection.ToArray())
								SetSelectedState(selected, false);
							SetSelectedState(g, true);
						}
						else
						{
							// Multiple selection.
							SetSelectedState(g, !CurrentSelection.Contains(g));
						}
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

		void DrawMoveControl()
		{
			var verts = GetSelectedVertices();
			if(verts.Length == 0)
				return;

			Vector3 pivot = generator.transform.TransformPoint(MathUtility.Average(verts.Select(v => v.position)));
			Quaternion pivotOrientation = generator.transform.rotation;

			EditorGUI.BeginChangeCheck();

			Vector3 newPivot = Handles.PositionHandle(pivot, pivotOrientation);
			newPivot = pivotOrientation * ApplySnap(Quaternion.Inverse(pivotOrientation) * newPivot);

			if(EditorGUI.EndChangeCheck())
			{
				RecordBeforeUndo("Move interior structure vertices");

				Vector3 deltaWorld = newPivot - pivot;
				Vector3 deltaLocal = generator.transform.InverseTransformVector(deltaWorld);

				foreach(var v in verts)
					v.position += deltaLocal;

				ReportChange();
			}
		}
		#endregion

		#region Auxiliary
		void RecordBeforeUndo(string undoName)
		{
			Undo.RecordObject(generator, undoName);
		}

		void ReportChange()
		{
			generator.Generate();
		}

		void TriggerInspectorRefresh()
		{
			Repaint();
		}

		static void DrawPolygon(Vector3[] poly)
		{
			DynamicMesh mesh = new();
			mesh.CreateFace(poly);
			mesh.Triangularize();
			foreach(var f in mesh.GetFaces())
				Handles.DrawAAConvexPolygon(f.Vertices.Select(v => v.position).ToArray());
		}

		Vector3 ApplySnap(Vector3 pos)
		{
			if(!(EditorSnapSettings.gridSnapActive || EditorSnapSettings.incrementalSnapActive))
				return pos;
			Vector3 snap = EditorSnapSettings.move;
			pos.x = Handles.SnapValue(pos.x, snap.x);
			pos.y = Handles.SnapValue(pos.y, snap.y);
			pos.z = Handles.SnapValue(pos.z, snap.z);
			return pos;
		}
		#endregion
	}
}
