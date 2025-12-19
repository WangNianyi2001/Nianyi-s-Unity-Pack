using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Nianyi.UnityPack.RoadSystem
{
	public interface INode
	{
		public Vector3 GetPosition();
	}

	[System.Serializable]
	public class AnchorNode : INode
	{
		public Vector3 position;

		public Vector3 GetPosition()
		{
			return position;
		}
	}

	[System.Serializable]
	public class RoadSpline
	{
		[System.NonSerialized] public INode start, end;

		[System.Serializable]
		public struct ControlPoint
		{
			public Vector3 position;

			/// 三阶贝塞尔曲线的控制点，按到控制点的偏移量计。
			public Vector3 handlePrev, handleNext;

			/// 超高，以横向斜率计。
			public float slope;
		}

		public List<ControlPoint> controlPoints = new() { default, default };

		public int SectionCount => controlPoints.Count - 1;

		public Bezier3 GetSection(int i)
		{
			if(i < 0 || i >= SectionCount)
				throw new System.IndexOutOfRangeException();

			bool isStart = i == 0, isEnd = i == SectionCount - 1;

			Vector3 start = isStart ? this.start.GetPosition() : controlPoints[i].position;
			Vector3 end = isEnd ? this.end.GetPosition() : controlPoints[i + 1].position;

			return new()
			{
				anchor0 = start,
				cp0 = start + controlPoints[i].handleNext,
				cp1 = end + controlPoints[i + 1].handlePrev,
				anchor1 = end,
			};
		}

		public Bezier3[] GetAllSections()
		{
			Bezier3[] sections = new Bezier3[SectionCount];
			for(int i = 0; i < SectionCount; ++i)
				sections[i] = GetSection(i);
			return sections;
		}

		public float Length => GetAllSections().Select(s => s.Length).Sum();

		public bool GetSectionByDistance(in float distance, out int index, out float frac)
		{
			if(distance < 0)
			{
				index = -1;
				frac = distance;
				return false;
			}

			float accLen = 0f;
			for(index = 0; index < SectionCount; ++index)
			{
				float secLen = GetSection(index).Length;
				frac = distance - accLen;
				if(frac < secLen)
					return true;
				accLen += secLen;
			}

			index = SectionCount;
			frac = distance - accLen;
			return false;
		}

		public Vector3 GetPosition(float distance)
		{
			if(!GetSectionByDistance(distance, out var i, out var frac))
				return i == 0 ? start.GetPosition() : end.GetPosition();
			return GetSection(i).SampleByDistance(frac);
		}

		public Matrix4x4 GetBasis(in float distance)
		{
			if(!GetSectionByDistance(distance, out var i, out var frac))
				return default;

			var section = GetSection(i);
			var length = section.Length;

			var offset = section.SampleByDistance(frac);

			float slope = Mathf.Lerp(controlPoints[i].slope, controlPoints[i + 1].slope, frac / length);

			Vector3 tangent = section.DerivativeByDistance(frac).normalized;
			Vector3 horizontalNormal = (Vector3.Cross(Vector3.up, tangent) + Vector3.up * slope).normalized;
			Vector3 verticalNormal = Vector3.Cross(tangent, horizontalNormal).normalized;

			return new(horizontalNormal, verticalNormal, tangent, new Vector4(0, 0, 0, 1) + (Vector4)offset);
		}

		/// <param name="road">X: Horizontal offset; Y: height; Z: Along road length.</param>
		public Vector3 RoadToLocal(Vector3 road)
		{
			float distance = road.z;
			road.z = 0;

			var basis = GetBasis(distance);
			return basis.MultiplyPoint(road);
		}
	}
}
