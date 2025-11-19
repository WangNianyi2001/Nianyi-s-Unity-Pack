using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack.Editor
{
	[CustomEditor(typeof(InteriorStructureGenerator))]
	public class InteriorStructureGeneratorEditor : ProceduralGeneratorEditor
	{
		InteriorStructureGenerator Generator => target as InteriorStructureGenerator;

		static readonly Color vertexHandleColor = new(.8f, .6f, 0f, 0.5f);
		const float vertexHandleSize = .25f;

		void OnSceneGUI()
		{
			//RepaintSceneGui();
			foreach(int i in Enumerable.Range(0, Generator.config.vertices.Count))
				DrawVertexHandle(i);
		}

		void RepaintSceneGui()
		{
			int id = GUIUtility.GetControlID(FocusType.Passive);

			Handles.color = vertexHandleColor;
			foreach(var v in Generator.config.vertices)
			{
				Vector3 position = Generator.transform.TransformPoint(v.position);
				Handles.SphereHandleCap(id,
					position, Quaternion.identity,
					HandleUtility.GetHandleSize(position) * vertexHandleSize,
					EventType.Repaint
				);
			}
		}

		// GPT gen
		void DrawVertexHandle(int i)
		{
			var v = Generator.config.vertices[i];
			Vector3 pos = Generator.transform.TransformPoint(v.position);

			int id = GUIUtility.GetControlID(FocusType.Passive);
			Event e = Event.current;

			switch(e.GetTypeForControl(id))
			{
				case EventType.Layout:
					float dist = HandleUtility.DistanceToCircle(pos,
						HandleUtility.GetHandleSize(pos) * 0.1f);
					HandleUtility.AddControl(id, dist);
					break;

				case EventType.MouseDown:
					if(e.button == 0 && HandleUtility.nearestControl == id)
					{
						GUIUtility.hotControl = id;
						e.Use();
					}
					break;

				case EventType.MouseDrag:
					if(GUIUtility.hotControl == id)
					{
						Undo.RecordObject(Generator, "Move vertex");
						Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
						// 简化：投射到水平平面
						float t = -ray.origin.y / ray.direction.y;
						Vector3 newPos = ray.origin + ray.direction * t;

						v.position = Generator.transform.InverseTransformPoint(newPos);
						e.Use();

						EditorApplication.delayCall += Generator.UpdateGeneration;
					}
					break;

				case EventType.MouseUp:
					if(GUIUtility.hotControl == id)
					{
						GUIUtility.hotControl = 0;
						e.Use();

						EditorApplication.delayCall += Generator.UpdateGeneration;
					}
					break;

				case EventType.Repaint:
					Handles.color = (HandleUtility.nearestControl == id)
						? Color.yellow
						: Color.red;

					Handles.SphereHandleCap(id, pos,
						Quaternion.identity,
						HandleUtility.GetHandleSize(pos) * 0.1f,
						EventType.Repaint);
					break;
			}
		}
	}
}
