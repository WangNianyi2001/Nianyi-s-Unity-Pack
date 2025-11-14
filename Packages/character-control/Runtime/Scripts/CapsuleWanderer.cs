using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nianyi.UnityPack
{
	[ExecuteAlways]
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
#if UNITY_EDITOR
			if(SceneUtility.IsEditing)
			{
				EditorUpdate();
				return;
			}
#endif
			GetComponentReferences();

			if(Profile == null)
				profile = LoadDefaultProfile();
			ApplyProfile();

			desiredRotation = Orientation;
		}

		void Update()
		{
#if UNITY_EDITOR
			if(SceneUtility.IsEditing)
			{
				EditorUpdate();
				return;
			}
#endif
		}

		void FixedUpdate()
		{
			float dt = Time.fixedDeltaTime;

			UpdateGroundedState();
			if(dt > 0)
			{
				Vector3 startingPosition = Body.position;
				ProcessMovement(dt);
				ProcessOrientation(dt);
				ProcessGravity(dt);
				ResolveCollision();
				ProcessBufferedVelocityChange(dt);
				Velocity = (Body.position - startingPosition) / dt;
			}
		}

#if UNITY_EDITOR
		void EditorUpdate()
		{
			GetComponentReferences();
			ApplyProfile();
		}

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
		Vector3 bufferedVelocityChange;

		void ProcessBufferedVelocityChange(float dt)
		{
			Vector3 desiredMovement = bufferedVelocityChange * dt;
			const int maxStep = 4;
			for(int i = 0; i < maxStep; ++i)
			{
				if(desiredMovement.magnitude < ContactOffset)
					break;
				TruncateMovement(desiredMovement, dt, out var step, out desiredMovement, out var impulses);
				ApplyImpulses(impulses);
				Body.position += step;
			}
			bufferedVelocityChange = Vector3.zero;
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

		void TruncateMovement(Vector3 desiredMovement, float dt, out Vector3 truncated, out Vector3 wallClip, out OutputImpulse[] impulses)
		{
			PhysicsUtility.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
			bool hasHit = Physics.CapsuleCast(
				topCenter, bottomCenter,
				capsule.radius - ContactOffset,
				desiredMovement, out var hit, desiredMovement.magnitude,
				Profile.collisionLayerMask
			);
			if(!hasHit)
			{
				truncated = desiredMovement;
				wallClip = Vector3.zero;
				impulses = null;
				return;
			}
			truncated = desiredMovement.normalized * (hit.distance - ContactOffset);
			desiredMovement -= truncated;
			var collisions = DetectCollisionsOnDirection(desiredMovement);

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

		#region Contact
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

		CollisionInfo CreateCollision(RaycastHit hit)
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

		CollisionInfo[] DetectCollisions(Vector3 path, Vector3 displacement = default)
		{
			PhysicsUtility.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);

			IEnumerable<RaycastHit> hits = Physics.CapsuleCastAll(
				topCenter + displacement, bottomCenter + displacement,
				capsule.radius - ContactOffset, path,
				path.magnitude + ContactOffset
			).Where(hit => !(hit.point == default && hit.distance == 0f));

			return hits.Select(CreateCollision).Where(x => x != null).ToArray();
		}
		CollisionInfo[] DetectCollisionsOnDirection(Vector3 direction, float depth) => DetectCollisions(direction.normalized * depth);
		CollisionInfo[] DetectCollisionsOnDirection(Vector3 direction) => DetectCollisionsOnDirection(direction, ContactOffset * 2);

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
		#endregion

		#region Gravity
		public bool IsGrounded { get; private set; }
		public Vector3 GroundNormal { get; private set; }

		void UpdateGroundedState()
		{
			var collisions = DetectCollisionsOnDirection(Physics.gravity);
			IsGrounded = collisions.Any(c => c.isGround);
			if(IsGrounded)
			{
				GroundNormal = collisions
					.Where(c => c.isGround)
					.Select(c => c.normal)
					.Aggregate((a, b) => a + b)
					.normalized;
			}
		}

		void ProcessGravity(float dt)
		{
			if(IsGrounded)
				return;
			Vector3 vy = Vector3.Project(Velocity, Physics.gravity);
			vy += Physics.gravity * dt;
			Vector3 desiredDy = vy * dt;

			const int maxStep = 4;
			for(int i = 0; i < maxStep; ++i)
			{
				TruncateMovement(desiredDy, dt, out var step, out desiredDy, out var impulses);
				ApplyImpulses(impulses);
				Body.position += step;
			}
		}
		#endregion
		#endregion

		#region Wander
		#region Movement
		public float MovementSpeed => Profile.movementSpeed;

		Vector3 desiredVelocity;
		public Vector3 Velocity { get; private set; }

		public bool IsActivelyMoving => desiredVelocity.sqrMagnitude > 0 && Velocity.sqrMagnitude > 0;

		public void MoveByVelocity(Vector3 worldVelocity)
		{
			desiredVelocity = worldVelocity;
		}

		void ProcessMovement(float dt)
		{
			// Calculate estimated movement.
			Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, Physics.gravity);
			float acceleration = Vector3.Distance(planarVelocity, desiredVelocity);
			float t = Mathf.Clamp01(Profile.acceleration * dt / acceleration);
			Vector3 movement = Vector3.Lerp(planarVelocity, desiredVelocity, t) * dt;

			if(IsGrounded)
			{
				// Boost the movement according to ground slope.
				movement += Vector3.Project(Vector3.ProjectOnPlane(movement, GroundNormal).normalized * movement.magnitude, Physics.gravity);

				if(CheckForStaircaseStepping(movement, in dt, out var steppingHeight))
					Jump_Internal(steppingHeight);
			}

			// Truncate movement based on wall sliding.
			const int maxStep = 4;
			for(int i = 0; i < maxStep && movement.magnitude >= ContactOffset; ++i)
			{
				TruncateMovement(movement, dt, out var step, out movement, out var impulses);

				if(!IsGrounded)
				{
					step.y = Mathf.Min(0, step.y); // TODO: Use gravity.
					movement = Vector3.ProjectOnPlane(movement, Physics.gravity);
				}

				Body.position += step;
				ApplyImpulses(impulses);
			}
		}

		bool CheckForStaircaseStepping(Vector3 movement, in float dt, out float height)
		{
			height = default;
			Vector3 horizontalMovement = Vector3.ProjectOnPlane(movement, Physics.gravity);
			if(horizontalMovement.magnitude < ContactOffset)
				return false;
			movement = horizontalMovement.normalized * (capsule.radius * 2);
			Vector3 originalPosition = Body.position;
			Vector3 upward = -Physics.gravity.normalized;

			// First detection: Decides the height.
			Body.position += upward * Profile.maxStepHeight + movement.normalized * -ContactOffset;
			TruncateMovement(movement, dt, out movement, out _, out _);
			Body.position = originalPosition;
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
		public void Jump()
		{
			if(!IsGrounded)
				return;
			Jump_Internal(Profile.jumpHeight);
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
		#endregion
		#endregion
	}
}
