using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

			if(dt > 0)
			{
				ProcessMovement(dt);
				ProcessOrientation(dt);
			}
		}

#if UNITY_EDITOR
		void EditorUpdate()
		{
			GetComponentReferences();
			ApplyProfile();
		}
#endif
		#endregion

		#region Wander
		#region Movement
		public float MovementSpeed => Profile.movementSpeed;

		Vector3 desiredVelocity;
		public Vector3 Velocity { get; private set; }
		public bool IsMoving => desiredVelocity.sqrMagnitude > 0 && Velocity.sqrMagnitude > 0;
		public bool IsGrounded { get; private set; }

		public void MoveByVelocity(Vector3 worldVelocity)
		{
			desiredVelocity = worldVelocity;
		}

		void ProcessMovement(float dt)
		{
			Vector3 velocity = desiredVelocity;
			if(Profile.useAcceleration)
			{
				float delta = Vector3.Distance(Velocity, desiredVelocity);
				float t = Mathf.Clamp01(Profile.acceleration * dt / delta);
				velocity = Vector3.Lerp(Velocity, desiredVelocity, t);
			}

			Velocity = velocity;
			Body.position += Velocity * dt;
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
			Vector3 rotation = desiredRotation;
			if(!Profile.useSmoothOrientation)
				desiredRotation = default;
			else
			{
				float t = Mathf.Clamp01(Profile.smoothOrientationCoefficient * dt);
				rotation *= t;
				desiredRotation *= 1 - t;
			}

			Orientation = Orientation.RotateEulerWithZenithClamped(rotation, Profile.zenithLimit);
		}
		#endregion
		#endregion
	}
}
