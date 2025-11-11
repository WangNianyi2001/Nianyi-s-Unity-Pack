using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

			ClearHotContacts();
			if(dt > 0)
			{
				Vector3 startingPosition = Position;
				ProcessBufferedVelocityChange(dt);
				ProcessMovement(dt);
				ProcessOrientation(dt);
				ProcessGravity(dt);
				ResolveCollision();
				Velocity = (Position - startingPosition) / dt;
			}
		}

		void OnCollisionEnter(Collision collision)
		{
			UpdateContact(collision);
		}

		void OnCollisionStay(Collision collision)
		{
			UpdateContact(collision);
		}

		void OnCollisionExit(Collision collision)
		{
			RemoveContacts(collision.collider);
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
			Gizmos.DrawLine(Position, Position + desiredVelocity);

			Gizmos.color = Color.red;
			Gizmos.DrawLine(Position, Position + Velocity);

			Gizmos.color = new(1, 0, 0, .3f);
			foreach(var contact in contacts)
			{
				Gizmos.DrawSphere(contact.position, 0.1f);
			}
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
				Position += step;
			}
			bufferedVelocityChange = Vector3.zero;
		}

		Vector3 Position
		{
			get => Body.position;
			set
			{
				Body.position = value;
				ClearHotContacts();
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
			DetectHotContactOnDirection(desiredMovement);

			if(!Profile.usePhysics)
			{
				var constraints = contacts.Select(c => new PhysicsUtility.WallClipConstraint()
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
				var constraints = contacts.Select(contact =>
				{
					PhysicsUtility.WallClipConstraint constraint = new()
					{
						normal = contact.normal,
						depth = 0f,
					};

					if(!contact.isStatic)
					{
						var target = contact.rigidbody;
						Vector3 targetVelocity = PhysicsUtility.RigidbodyVelocityAtPoint(contact.position,
							target.centerOfMass, target.velocity, target.angularVelocity
						);
						Vector3 relativeVelocity = Velocity - targetVelocity;
						float mu = rigidbody.mass * target.mass / (rigidbody.mass + target.mass);
						Vector3 impulse = mu * relativeVelocity;
						Vector3 targetMovement = impulse * (dt / target.mass);
						float depth = Vector3.Project(targetMovement, contact.normal).magnitude;
						constraint.depth = Mathf.Clamp(0, ContactOffset, depth);
					}

					return constraint;
				}).ToList();

				var movement = PhysicsUtility.WallClip(desiredMovement, constraints, ContactOffset);
				wallClip = movement;

				// TODO: Lateral frictions.
				impulses = contacts.Where(c => !c.isStatic).Select(contact =>
				{
					var target = contact.rigidbody;
					float mu = rigidbody.mass * target.mass / (rigidbody.mass + target.mass);
					return new OutputImpulse()
					{
						impulse = Vector3.Project(movement, contact.normal) * (2 * mu / dt),
						position = contact.position,
						rigidbody = target,
					};
				}).ToArray();
			}
		}

		#region Contact
		float ContactOffset => capsule.contactOffset;

		class ContactInfo
		{
			public Vector3 position;
			public Vector3 normal;

			public Collider collider;
			public Rigidbody rigidbody;

			public bool isHot;
			public bool isGround;
			public bool isStatic;

			public float mass;
		}

		readonly List<ContactInfo> contacts = new();

		void AddContact(ContactInfo contact)
		{
			contact.isGround = Vector3.Angle(contact.normal, Vector3.up) <= Profile.maxMovingSlope;
			contact.isStatic = !contact.collider.TryGetComponent(out contact.rigidbody);
			contact.mass = contact.isStatic ? Mathf.Infinity : contact.rigidbody.mass;

			contacts.Add(contact);
		}

		void UpdateContact(Collision collision)
		{
			Collider collider = collision.collider;
			RemoveContacts(collider);
			foreach(var contact in collision.contacts)
			{
				AddContact(new()
				{
					position = contact.point,
					normal = contact.normal,
					collider = collider,
					isHot = false,
				});
			}
		}

		void UpdateContact(RaycastHit hit, bool hot = false)
		{
			Collider collider = hit.collider;
			if(collider == null || collider == capsule)
				return;
			RemoveContacts(collider);
			AddContact(new()
			{
				position = hit.point,
				normal = hit.normal,
				collider = collider,
				isHot = hot,
			});
		}

		void RemoveContacts(Collider collider)
		{
			contacts.RemoveAll(c => c.collider == collider);
		}

		void ClearHotContacts()
		{
			contacts.RemoveAll(c => c.isHot);
		}

		void DetectHotContactOnDirection(Vector3 direction)
		{
			PhysicsUtility.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
			var hits = Physics.CapsuleCastAll(topCenter, bottomCenter, capsule.radius - ContactOffset, direction, ContactOffset * 2);
			foreach(var hit in hits)
				UpdateContact(hit, true);
		}

		void ResolveCollision()
		{
			const int maxRound = 2;
			for(int i = 0; i < maxRound; ++i)
			{
				bool flag = false;
				Vector3 sum = Vector3.zero;
				foreach(var contact in contacts)
				{
					var collider = contact.collider;
					if(!Physics.ComputePenetration(
						capsule, Position, Body.rotation,
						collider, collider.transform.position, collider.transform.rotation,
						out var direction, out var distance
					))
						continue;
					sum += direction * distance;
					flag = true;
				}
				Position += sum;
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
			DetectHotContactOnDirection(Vector3.down);
			IsGrounded = contacts.Any(c => c.isGround);
			if(!IsGrounded)
				GroundNormal = Vector3.zero;
			else
			{
				GroundNormal = contacts
					.Where(c => c.isGround)
					.Select(c => c.normal)
					.Aggregate((a, b) => a + b)
					.normalized;
			}
		}

		void ProcessGravity(float dt)
		{
			Vector3 vy = Vector3.Project(Velocity, Physics.gravity);
			vy += Physics.gravity * dt;
			Vector3 desiredDy = vy * dt;

			const int maxStep = 4;
			for(int i = 0; i < maxStep; ++i)
			{
				UpdateGroundedState();
				if(IsGrounded)
					break;

				TruncateMovement(desiredDy, dt, out var step, out desiredDy, out var impulses);
				ApplyImpulses(impulses);
				Position += step;
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

			// Boost the movement according to ground slope.
			UpdateGroundedState();
			if(IsGrounded)
				movement += Vector3.Project(Vector3.ProjectOnPlane(movement, GroundNormal).normalized * movement.magnitude, Physics.gravity);

			// Truncate movement based on wall sliding.
			const int maxAuditStep = 4;
			for(int i = 0; i < maxAuditStep; ++i)
			{
				TruncateMovement(movement, dt, out var step, out movement, out var impulses);
				ApplyImpulses(impulses);
				if(!IsGrounded)
					step.y = Mathf.Min(0, step.y);
				Position += step;
				UpdateGroundedState();
				if(!IsGrounded)
					movement = Vector3.ProjectOnPlane(movement, Physics.gravity);
				if(movement.magnitude < ContactOffset)
					break;
			}
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
			bufferedVelocityChange += -Physics.gravity.normalized * Mathf.Sqrt(2 * Profile.jumpHeight * Physics.gravity.magnitude);
		}
		#endregion
		#endregion
	}
}
