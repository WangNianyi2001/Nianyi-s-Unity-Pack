using UnityEngine;

namespace Nianyi.UnityPack
{
	public abstract class Wanderer : MonoBehaviour
	{
		#region Component references
		public abstract Transform Body { get; }
		public abstract Transform Head { get; }
		#endregion

		#region Grounding
		public abstract bool IsGrounded { get; }
		public abstract bool IsGrounded_Coyoted { get; }
		public abstract Vector3 GroundNormal { get; }
		#endregion

		#region Movement
		public abstract bool IsActivelyMoving { get; }
		public abstract float MovementSpeed { get; }
		public abstract Vector3 Velocity { get; }

		public abstract void MoveByVelocity(Vector3 worldVelocity);
		#endregion

		#region Orientation
		public abstract Vector3 Orientation { get; }

		public abstract void OrientByDelta(Vector3 rotation);
		#endregion

		#region Jumping
		public abstract void Jump();
		#endregion
	}
}
