using UnityEngine;

namespace Nianyi.UnityPack
{
	[System.Serializable]
	public struct Bezier3
	{
		public Vector3 anchor0;
		public Vector3 anchor1;
		public Vector3 cp0;
		public Vector3 cp1;

		public readonly float Length => MathUtility.Length(this);
	}
}
