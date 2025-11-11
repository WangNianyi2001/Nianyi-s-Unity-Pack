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

			if(!profile.useRigidbody)
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
			if(Scene.IsEditing)
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
			if(Scene.IsEditing)
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
				Vector3 startingPosition = Body.position;
				ProcessMovement(dt);
				ProcessOrientation(dt);
				ProcessGravity(dt);
				ResolveCollision();
				Velocity = (Body.position - startingPosition) / dt;
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
			Gizmos.DrawLine(Body.position, Body.position + desiredVelocity);

			Gizmos.color = Color.red;
			Gizmos.DrawLine(Body.position, Body.position + Velocity);

			Gizmos.color = new(1, 0, 0, .3f);
			foreach(var contact in contacts)
			{
				Gizmos.DrawSphere(contact.position, 0.1f);
			}
		}
#endif
		#endregion

		#region Physics
		void TruncateMovement(Vector3 desiredMovement, out Vector3 truncated, out Vector3 wallSlide)
		{
			Math.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
			bool hasHit = Physics.CapsuleCast(
				topCenter, bottomCenter,
				capsule.radius - ContactOffset,
				desiredMovement, out var hit, desiredMovement.magnitude,
				Profile.collisionLayerMask
			);
			if(!hasHit)
			{
				truncated = desiredMovement;
				wallSlide = Vector3.zero;
			}
			else
			{
				truncated = desiredMovement.normalized * (hit.distance - ContactOffset);
				desiredMovement -= truncated;

				DetectHotContactOnDirection(desiredMovement);
				var normals = contacts.Select(c => c.normal).ToArray();
				wallSlide = Math.WallSlide(desiredMovement, normals, ContactOffset);
			}
		}

		#region Contact
		float ContactOffset => capsule.contactOffset;

		class ContactInfo
		{
			public bool isHot;
			public Vector3 position;
			public Vector3 normal;
			public Collider collider;
			public bool isGround;
		}

		readonly List<ContactInfo> contacts = new();

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

		void AddContact(ContactInfo contact)
		{
			contact.isGround = Vector3.Angle(contact.normal, Vector3.up) <= Profile.maxMovingSlope;
			contacts.Add(contact);
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
			Math.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
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
						capsule, Body.position, Body.rotation,
						collider, collider.transform.position, collider.transform.rotation,
						out var direction, out var distance
					))
						continue;
					sum += direction * distance;
					flag = true;
				}
				Body.position += sum;
				if(!flag)
					break;
			}
		}
		#endregion

		#region Gravity
		public bool IsGrounded { get; private set; }
		public Vector3 GroundNormal { get; private set; }

		void DetectGroundedState()
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
				DetectGroundedState();
				if(IsGrounded)
					break;

				TruncateMovement(desiredDy, out var step, out desiredDy);
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

			// Tweak the movement according to ground slope.
			DetectGroundedState();
			if(IsGrounded)
			{
				movement = Vector3.ProjectOnPlane(movement, GroundNormal).normalized * movement.magnitude;
			}

			// Truncate movement based on wall sliding.
			const int maxAuditStep = 4;
			for(int i = 0; i < maxAuditStep; ++i)
			{
				TruncateMovement(movement, out var step, out movement);
				Body.position += step;
				DetectGroundedState();
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
		#endregion
	}
}
