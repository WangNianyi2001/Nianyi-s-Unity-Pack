using UnityEngine;

namespace Nianyi.UnityPack
{
	[CreateAssetMenu(menuName = "Nianyi's Unity Pack/Character Control/Capsule Wanderer Profile")]
	public class CapsuleWandererProfile : ScriptableObject
	{
		#region Shape
		[Header("Shape")]

		[Min(0)] public float height = 2f;

		[Min(0)] public float radius = 0.5f;
		#endregion

		#region Physics
		[Header("Physics")]

		public LayerMask collisionLayerMask = ~0;

		public bool usePhysics = true;

		[ShowWhen("usePhysics", true)]
		[Min(0)] public float mass = 1f;
		#endregion

		#region Movement
		[Header("Movement")]

		[Tooltip("Meters per second.")]
		[Min(0)] public float movementSpeed = 5f;

		public bool useAcceleration = true;

		[Tooltip("Meters per square second.")]
		[ShowWhen("useAcceleration", true)]
		[Min(0)] public float acceleration = 30f;

		[Range(0, 90)] public float maxMovingSlope = 30f;
		[Min(0)] public float maxStepHeight = 0.5f;
		#endregion

		#region Orientation
		[Header("Orientation")]

		public bool useOrientationSpeedCap = false;

		[Tooltip("Degrees per second.")]
		[ShowWhen("useOrientationSpeedCap", true)]
		[Min(0)] public float orientaionSpeed = 360f;

		[Range(0, 90)] public float zenithLimit = 90f;

		public bool useSmoothOrientation = true;

		[Tooltip("Ratio per second.")]
		[ShowWhen("useSmoothOrientation", true)]
		[Min(1)] public float smoothOrientationCoefficient = 10f;
		#endregion
	}
}
