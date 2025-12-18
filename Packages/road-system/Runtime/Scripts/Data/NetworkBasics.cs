using UnityEngine;

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
	public class Spline
	{
		[System.NonSerialized] public INode start, end;
		/// 三阶贝塞尔曲线的控制点，按到 start、end 锚点的偏移量计。
		public Vector3 startCP, endCP;

		public Bezier3 Bezier
		{
			get
			{
				Vector3 s = StartPosition, e = EndPosition;
				return new()
				{
					anchor0 = s,
					cp0 = s + startCP,
					cp1 = e + endCP,
					anchor1 = e,
				};
			}
		}

		public Vector3 StartPosition => start.GetPosition();
		public Vector3 EndPosition => end.GetPosition();
		public float Length => Bezier.Length;

		public Vector3 SamplePositionByPortion(float t)
			=> Bezier.SampleByT(t);

		public Vector3 SamplePositionByDistance(float distance)
			=> Bezier.SampleByDistance(distance);
	}
}
