using UnityEngine;
using System.Collections;
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

			ClearContacts();
			if(dt > 0)
			{
				ProcessMovement(dt);
				ProcessOrientation(dt);
			}
			ResolveCollision();
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

		#region Contact
		class ContactInfo
		{
			public Vector3 position;
			public Vector3 normal;
			public Collider collider;
			public bool isGround;
		}

		readonly List<ContactInfo> contacts = new();

		void UpdateContact(RaycastHit hit)
		{
			Collider collider = hit.collider;
			if(collider == null || collider == capsule)
				return;
			RemoveContacts(collider);
			contacts.Add(new()
			{
				position = hit.point,
				normal = hit.normal,
				collider = collider,
			});
		}

		void RemoveContacts(Collider collider)
		{
			contacts.RemoveAll(c => c.collider == collider);
		}

		void ClearContacts()
		{
			contacts.Clear();
		}

		void DetectContactOnDirection(Vector3 direction)
		{
			Math.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
			float epsilon = capsule.contactOffset;
			var hits = Physics.CapsuleCastAll(topCenter, bottomCenter, capsule.radius - epsilon, direction, epsilon * 2);
			foreach(var hit in hits)
				UpdateContact(hit);
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

		#region Wander
		#region Movement
		public float MovementSpeed => Profile.movementSpeed;

		Vector3 desiredVelocity;
		public Vector3 Velocity { get; private set; }

		public bool IsMoving => desiredVelocity.sqrMagnitude > 0 && Velocity.sqrMagnitude > 0;

		public void MoveByVelocity(Vector3 worldVelocity)
		{
			desiredVelocity = worldVelocity;
		}

		void ProcessMovement(float dt)
		{
			// Recording initial data.
			Vector3 startingPosition = Body.position;

			// Calculate estimated movement.
			Vector3 movement;
			if(!Profile.useAcceleration)
				movement = desiredVelocity * dt;
			else
			{
				float acceleration = Vector3.Distance(Velocity, desiredVelocity);
				float t = Mathf.Clamp01(Profile.acceleration * dt / acceleration);
				movement = Vector3.Lerp(Velocity, desiredVelocity, t) * dt;
			}

			// Audit movement.
			float epsilon = capsule.contactOffset;
			Vector3 remainingMovement = movement;
			const int maxAuditStep = 4;
			for(int i = 0; i < maxAuditStep; ++i)
			{
				DetectContactOnDirection(remainingMovement);
				var normals = contacts.Select(c => c.normal).ToArray();
				Vector3 step = Math.WallSlide(remainingMovement, normals, epsilon);
				if(step.magnitude < epsilon)
					break;

				Math.GetCapsuleCenters(capsule, out var topCenter, out var bottomCenter);
				bool hasHit = Physics.CapsuleCast(
					topCenter, bottomCenter,
					capsule.radius - epsilon,
					step, out var hit, step.magnitude,
					Profile.collisionLayerMask
				);
				if(!hasHit)
				{
					Body.position += remainingMovement;
					remainingMovement = Vector3.zero;
					break;
				}
				UpdateContact(hit);

				float stepDistance = hit.distance - epsilon;
				step = step.normalized * stepDistance;
				Body.position += step;
				remainingMovement -= step;
			}

			// Updating the velocity record.
			Velocity = (Body.position - startingPosition) / dt;
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
