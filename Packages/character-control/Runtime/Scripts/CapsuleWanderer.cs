using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nianyi.UnityPack
{
	[RequireComponent(typeof(CapsuleCollider))]
	public class CapsuleWanderer : MonoBehaviour
	{
		#region Profile
		[SerializeField] CapsuleWandererProfile profile;

		public CapsuleWandererProfile Profile
		{
			get => profile;
			set
			{
				profile = value;
				ApplyProfile();
			}
		}

		void ApplyProfile()
		{
			var profile = Profile;
			if(!profile)
				profile = LoadDefaultProfile();

			capsule.height = profile.height;
			capsule.radius = profile.radius;
			capsule.center = Vector3.up * (capsule.height / 2);

			if(!profile.usePhysics)
				gameObject.RemoveComponent(ref rigidbody);
			else
			{
				gameObject.EnsureComponent(out rigidbody);

				rigidbody.isKinematic = true;
				rigidbody.mass = profile.mass;
			}
		}

		CapsuleWandererProfile LoadDefaultProfile()
		{
			var profile = ScriptableObject.CreateInstance<CapsuleWandererProfile>();

			// Copy shape settings from the capsule collider.
			profile.height = capsule.height;
			profile.radius = capsule.radius;

			return profile;
		}
		#endregion

		#region Component references
		CapsuleCollider capsule;
#if UNITY_EDITOR
		new
#endif
		Rigidbody rigidbody;

		public Transform Body => transform;

		[SerializeField] Transform head;
		public Transform Head => head;

		void GetComponentReferences()
		{
			capsule = GetComponent<CapsuleCollider>();
		}
		#endregion

		#region Life cycle
		void Awake()
		{
			GetComponentReferences();

			if(Profile == null)
				profile = LoadDefaultProfile();
			ApplyProfile();

			desiredRotation = Orientation;
			lastPosition = Body.position;
		}

		void FixedUpdate()
		{
			lastPosition = Body.position;
			float dt = Time.fixedDeltaTime;
			if(dt > 0)
			{
				UpdateGroundedState(dt);
				ProcessJump(dt);
				ProcessMovement(dt);
				ProcessOrientation(dt);
				ProcessGravity(dt);
				ResolveCollision();
				ProcessBufferedVelocityChange(dt);
				Velocity = Velocity_Internal;
			}
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(Body.position, Body.position + desiredVelocity);

			Gizmos.color = Color.red;
			Gizmos.DrawLine(Body.position, Body.position + Velocity);
		}
#endif
		#endregion

		#region Physics
		delegate void MoveTruncated_PostProcess(ref Vector3 movement, ref Vector3 step);
		void MoveTruncated(Vector3 movement, float dt, MoveTruncated_PostProcess postProcess = null)
		{
			const int maxStep = 4;
			for(int i = 0; i < maxStep && movement != Vector3.zero; ++i)
			{
				TruncateMovement(movement, out var step);
				movement -= step;
				WallClipWithMass(movement, dt, out movement, out var impulses);
				postProcess?.Invoke(ref movement, ref step);
				Body.position += step;
				ApplyImpulses(impulses);
			}
		}

		#region Collision
		float ContactOffset => capsule.contactOffset;

		class CollisionInfo
		{
			public Vector3 position;
			public Vector3 normal;
			public float distance;

			public Collider collider;
			public Rigidbody rigidbody;

			public bool isGround;
			public bool isStatic;

			public float mass;
		}

		CollisionInfo MakeCollision(RaycastHit hit)
		{
			Collider collider = hit.collider;
			if(collider == null || collider == capsule)
				return null;
			CollisionInfo contact = new()
			{
				position = hit.point,
				normal = hit.normal,
				collider = collider,
				distance = hit.distance,
			};
			contact.isGround = Vector3.Angle(contact.normal, Vector3.up) <= Profile.maxMovingSlope;
			contact.isStatic = !contact.collider.TryGetComponent(out contact.rigidbody);
			contact.mass = contact.isStatic ? Mathf.Infinity : contact.rigidbody.mass;
			return contact;
		}

		CollisionInfo DetectCollision(Vector3 path, Vector3 displacement = default)
		{
			PhysicsUtility.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
			bool hasHit = Physics.CapsuleCast(
				topCenter + displacement, bottomCenter + displacement,
				capsule.radius - ContactOffset,
				path, out var hit, path.magnitude,
				Profile.collisionLayerMask
			);
			if(hasHit)
				return MakeCollision(hit);
			else
				return null;
		}

		CollisionInfo[] DetectCollisions(Vector3 path, Vector3 displacement = default)
		{
			PhysicsUtility.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);

			IEnumerable<RaycastHit> hits = Physics.CapsuleCastAll(
				topCenter + displacement, bottomCenter + displacement,
				capsule.radius - ContactOffset, path,
				path.magnitude + ContactOffset
			).Where(hit => !(hit.point == default && hit.distance == 0f));

			return hits.Select(MakeCollision).Where(x => x != null).ToArray();
		}
		CollisionInfo[] DetectContacts(Vector3 direction) => DetectCollisions(direction.normalized * (ContactOffset * 2));

		void ResolveCollision()
		{
			const int maxRound = 2;
			for(int i = 0; i < maxRound; ++i)
			{
				bool flag = false;
				Vector3 sum = Vector3.zero;
				// TODO: Get all touching contacts.
				//foreach(var contact in contacts)
				//{
				//	Collider collider = contact.collider;
				//	if(!Physics.ComputePenetration(
				//		capsule, Body.position, Body.rotation,
				//		collider, collider.transform.position, collider.transform.rotation,
				//		out var direction, out var distance
				//	))
				//		continue;
				//	sum += direction * distance;
				//	flag = true;
				//}
				Body.position += sum;
				if(!flag)
					break;
			}
		}

		void TruncateMovement(Vector3 desiredMovement, out Vector3 truncated, Vector3 displacement = default)
		{
			var collision = DetectCollision(desiredMovement, displacement);
			if(collision != null)
				truncated = desiredMovement.normalized * (collision.distance - ContactOffset);
			else
				truncated = desiredMovement;
		}

		void WallClipWithMass(Vector3 desiredMovement, float dt, out Vector3 wallClip, out OutputImpulse[] impulses)
		{
			var collisions = DetectContacts(desiredMovement);

			if(!Profile.usePhysics)
			{
				var constraints = collisions.Select(c => new PhysicsUtility.WallClipConstraint()
				{
					normal = c.normal,
					depth = 0f,
				}).ToList();
				wallClip = PhysicsUtility.WallClip(desiredMovement, constraints, ContactOffset);
				impulses = null;
			}
			else
			{
				// TODO: Use local cache to avoid repetitive computation.
				// TODO: Coefficient of restitution.
				var constraints = collisions.Select(collision =>
				{
					PhysicsUtility.WallClipConstraint constraint = new()
					{
						normal = collision.normal,
						depth = 0f,
					};

					if(!collision.isStatic)
					{
						var target = collision.rigidbody;
						Vector3 targetVelocity = PhysicsUtility.RigidbodyVelocityAtPoint(collision.position,
							target.centerOfMass, target.velocity, target.angularVelocity
						);
						Vector3 relativeVelocity = Velocity - targetVelocity;
						float mu = rigidbody.mass * target.mass / (rigidbody.mass + target.mass);
						Vector3 impulse = mu * relativeVelocity;
						Vector3 targetMovement = impulse * (dt / target.mass);
						float depth = Vector3.Project(targetMovement, collision.normal).magnitude;
						constraint.depth = Mathf.Clamp(0, ContactOffset, depth);
					}

					return constraint;
				}).ToList();

				var movement = PhysicsUtility.WallClip(desiredMovement, constraints, ContactOffset);
				wallClip = movement;

				// TODO: Lateral frictions.
				impulses = collisions.Where(c => !c.isStatic).Select(collision =>
				{
					var target = collision.rigidbody;
					float mu = rigidbody.mass * target.mass / (rigidbody.mass + target.mass);
					return new OutputImpulse()
					{
						impulse = Vector3.Project(movement, collision.normal) * (2 * mu / dt),
						position = collision.position,
						rigidbody = target,
					};
				}).ToArray();
			}
		}

		struct OutputImpulse
		{
			public Rigidbody rigidbody;
			public Vector3 position;
			public Vector3 impulse;
		}

		void ApplyImpulses(IEnumerable<OutputImpulse> impulses)
		{
			if(impulses == null)
				return;
			foreach(var impulse in impulses)
			{
				if(!impulse.rigidbody)
					continue;
				impulse.rigidbody.AddForceAtPosition(impulse.impulse, impulse.position, ForceMode.Impulse);
			}
		}
		#endregion

		#region Velocity buffer
		Vector3 bufferedVelocityChange;

		void ProcessBufferedVelocityChange(float dt)
		{
			MoveTruncated(bufferedVelocityChange * dt, dt);
			bufferedVelocityChange = Vector3.zero;
		}
		#endregion

		#region Gravity
		public bool IsGrounded { get; private set; }
		public Vector3 GroundNormal { get; private set; }

		public bool IsGrounded_Coyoted => IsGrounded || coyoteTime > 0f;
		float coyoteTime;

		void UpdateGroundedState(float dt)
		{
			bool wasGrounded = IsGrounded;

			var collisions = DetectContacts(Physics.gravity);
			IsGrounded = collisions.Any(c => c.isGround);
			if(IsGrounded)
			{
				GroundNormal = collisions
					.Where(c => c.isGround)
					.Select(c => c.normal)
					.Aggregate((a, b) => a + b)
					.normalized;
			}

			if(!IsGrounded)
			{
				if(wasGrounded && Profile.useCoyoteTime)
					coyoteTime = Profile.coyoteTime;
				coyoteTime = Mathf.Max(0, coyoteTime - dt);
			}
		}

		void ProcessGravity(float dt)
		{
			if(IsGrounded)
				return;
			Vector3 vy = Vector3.Project(Velocity, Physics.gravity);
			vy += Physics.gravity * dt;
			MoveTruncated(vy * dt, dt);
		}
		#endregion
		#endregion

		#region Wander
		#region Movement
		public float MovementSpeed => Profile.movementSpeed;

		Vector3 desiredVelocity;
		Vector3 lastPosition;
		Vector3 Velocity_Internal => (Body.position - lastPosition) / Time.fixedDeltaTime;
		public Vector3 Velocity { get; set; }

		public bool IsActivelyMoving => desiredVelocity.sqrMagnitude > 0 && Velocity.sqrMagnitude > 0;

		public void MoveByVelocity(Vector3 worldVelocity)
		{
			desiredVelocity = worldVelocity;
		}

		void ProcessMovement(float dt)
		{
			// Calculate estimated movement.
			Vector3 movement;
			if(!Profile.useAcceleration)
				movement = desiredVelocity * dt;
			else
			{
				Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, Physics.gravity);
				float acceleration = Vector3.Distance(planarVelocity, desiredVelocity);
				float t = Mathf.Clamp01(Profile.acceleration * dt / acceleration);
				movement = Vector3.Lerp(planarVelocity, desiredVelocity, t) * dt;
			}

			if(IsGrounded)
			{
				// Boost the movement according to ground slope.
				movement += Vector3.Project(Vector3.ProjectOnPlane(movement, GroundNormal).normalized * movement.magnitude, Physics.gravity);
			}

			if(IsGrounded_Coyoted)
			{
				if(CheckForStaircaseStepping(movement, out var steppingHeight))
					Jump_Internal(steppingHeight);
			}

			MoveTruncated(movement, dt, MovementPostProcess);
		}

		void MovementPostProcess(ref Vector3 movement, ref Vector3 step)
		{
			if(IsGrounded)
				return;
			step.y = Mathf.Min(0, step.y); // TODO: Use gravity.
			movement = Vector3.ProjectOnPlane(movement, Physics.gravity);
		}

		bool CheckForStaircaseStepping(Vector3 movement, out float height)
		{
			height = default;
			Vector3 horizontalMovement = Vector3.ProjectOnPlane(movement, Physics.gravity);
			if(horizontalMovement.magnitude < ContactOffset)
				return false;
			movement = horizontalMovement.normalized * (capsule.radius * 2);
			Vector3 upward = -Physics.gravity.normalized;

			// Zeroth detection: See if running into wall.
			var upfrontCollision = DetectCollision(movement);
			if(upfrontCollision == null || upfrontCollision.isGround)
				return false;

			// First detection: Decides the height.
			TruncateMovement(
				movement, out movement,
				upward * Profile.maxStepHeight + movement.normalized * -ContactOffset
			);
			if(!GetStaircaseHeight(movement, out height))
				return false;

			// Second detection: Decides the distance.
			float t = Mathf.Sqrt(2f * height / Physics.gravity.magnitude);
			movement = Vector3.ProjectOnPlane(Velocity, Physics.gravity) * t;
			if(!GetStaircaseHeight(movement, out height))
				return false;

			// Check for elevated slope bad cases.
			Vector3 estimatedSlopeMovement = movement - Vector3.Dot(movement, GroundNormal) / Vector3.Dot(GroundNormal, Physics.gravity) * Physics.gravity;
			if(height - estimatedSlopeMovement.y <= ContactOffset * 3)
				return false;

			return true;
		}

		bool GetStaircaseHeight(in Vector3 movement, out float height)
		{
			height = 0f;
			Vector3 upward = -Physics.gravity.normalized;
			var groundContacts = DetectCollisions(
				upward * (-Profile.maxStepHeight - ContactOffset),
				upward * Profile.maxStepHeight + movement * (1 - ContactOffset / movement.magnitude)
			).Where(c => c.isGround).ToList();
			if(groundContacts.Count == 0)
				return false;

			var highest = groundContacts.Aggregate((a, b) => Vector3.Dot(a.position - b.position, upward) > 0 ? a : b);
			if(!highest.isGround)
				return false;
			height = Vector3.Dot(highest.position - Body.position, upward);
			if(height <= ContactOffset)
				return false;
			return true;
		}
		#endregion

		#region Orientation
		public Vector3 Orientation
		{
			get => Head.eulerAngles;
			private set
			{
				Body.eulerAngles = new(0, value.y, 0);
				Head.localEulerAngles = new(value.x, 0, 0);
			}
		}

		Vector3 desiredRotation;

		public void OrientByDelta(Vector3 delta)
		{
			desiredRotation += delta;
		}

		public void ProcessOrientation(float dt)
		{
			Vector3 rotation;
			if(!Profile.useSmoothOrientation)
			{
				rotation = desiredRotation;
				desiredRotation = Vector3.zero;
			}
			else
			{
				float t = Mathf.Clamp01(Profile.smoothOrientationCoefficient * dt);
				rotation = desiredRotation * t;
				desiredRotation *= 1 - t;
			}

			Orientation = Orientation.RotateEulerWithZenithClamped(rotation, Profile.zenithLimit);
		}
		#endregion

		#region Jumping
		float inputBufferTime;

		public void Jump()
		{
			inputBufferTime = Profile.inputBufferTime;
		}

		void Jump_Internal(float height)
		{
			if(height <= 0f)
				return;
			Vector3 upDirection = -Physics.gravity.normalized;
			float desiredVy = Mathf.Sqrt(2 * height * Physics.gravity.magnitude);
			if(Vector3.Dot(Velocity, upDirection) >= desiredVy)
				return;
			bufferedVelocityChange += upDirection * desiredVy - Vector3.Project(Velocity, upDirection);
		}

		void ProcessJump(float dt)
		{
			inputBufferTime = Mathf.Max(0f, inputBufferTime - dt);
			if(inputBufferTime <= 0f)
				return;
			if(!IsGrounded_Coyoted)
				return;
			Jump_Internal(Profile.jumpHeight);
		}
		#endregion
		#endregion
	}
}
