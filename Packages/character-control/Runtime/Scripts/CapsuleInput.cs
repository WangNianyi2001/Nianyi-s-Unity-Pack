#if !(ENABLE_LEGACY_INPUT_MANAGER || ENABLE_INPUT_SYSTEM)
#error "The Character Control requires at least one input source (the legacy Input Manager or the new Input System) to be enabled."
#endif

#if (ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM) || (!ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM)
#define SINGLE_INPUT_SYSTEM
#endif

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Nianyi.UnityPack
{
	[RequireComponent(typeof(CapsuleWanderer))]
	public class CapsuleInput : MonoBehaviour
	{
		#region Component references
		CapsuleWanderer wanderer;
		#endregion

		#region General configurations
		public bool autoHideCursor = true;
		public bool invertY = false;
		#endregion

		#region Input sources
		public enum InputSource
		{
			Auto = 0,
#if ENABLE_LEGACY_INPUT_MANAGER
			LegacyInputManager = 1,
#endif
#if ENABLE_INPUT_SYSTEM
			InputSystem = 2,
#endif
		}
		public InputSource inputSource;

#if ENABLE_LEGACY_INPUT_MANAGER
		[System.Serializable]
		public class LegacyInputManagerConfig
		{
			public string horizontal = "Horizontal";
			public string vertical = "Vertical";
			public string mouseX = "Mouse X";
			public string mouseY = "Mouse Y";
			public string Jump = "Jump";
		}
		[
			ShowWhen("inputSource", InputSource.LegacyInputManager),
			ShowWhen("inputSource", InputSource.Auto),
			Expanded
		]
		public LegacyInputManagerConfig legacyInputManagerConfig = new();
#endif

#if ENABLE_INPUT_SYSTEM
		[System.Serializable]
		public class InputSystemConfig
		{
			[Tooltip("Currently not working.")]
			public string inputActionMap;
		}
		[
			ShowWhen("inputSource", InputSource.InputSystem),
			ShowWhen("inputSource", InputSource.Auto),
			Expanded
		]
		public InputSystemConfig inputSystemConfig = new();
#endif
		#endregion

		#region Unity life cycle
		void Awake()
		{
			wanderer = GetComponent<CapsuleWanderer>();
		}

		void Update()
		{
			ReceiveInputs();
			PostProcessInputs();
			ApplyInputs();
		}

		void OnEnable() => SetActive(true);
		void OnDisable() => SetActive(false);
		#endregion

		#region Life cycle
		void SetActive(bool value)
		{
			if(autoHideCursor)
				Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
		}

		void ReceiveInputs()
		{
			switch(inputSource)
			{
				case InputSource.Auto:
#if ENABLE_INPUT_SYSTEM
				case InputSource.InputSystem:
					ReceiveVelocity_is();
					ReceiveRotation_is();
					ReceiveJumping_is();
					break;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
				case InputSource.LegacyInputManager:
					ReceiveVelocity_lim();
					ReceiveRotation_lim();
					ReceiveJumping_lim();
					break;
#endif
				default:
					throw new System.NotSupportedException($"Input source type \"{inputSource}\" is not enabled or supported.");
			}
		}

		void PostProcessInputs()
		{
			// Movement
			inputVelocity.Normalize();

			// Orientation
			if(!invertY)
				inputRotation.x = -inputRotation.x;
			inputRotation *= 180f / Screen.height;

			// Jumping
		}

		void ApplyInputs()
		{
			// Movement
			Vector3 worldVelocity = wanderer.Body.transform.rotation * inputVelocity * wanderer.MovementSpeed;
			wanderer.MoveByVelocity(worldVelocity);

			// Orientation
			wanderer.OrientByDelta(inputRotation);

			// Jumping
			if(inputJumping)
			{
				wanderer.Jump();
				inputJumping = false;
			}
		}
		#endregion

		#region Movement
		Vector3 inputVelocity;

#if ENABLE_LEGACY_INPUT_MANAGER
		void ReceiveVelocity_lim()
		{
			inputVelocity = new(
				Input.GetAxis(legacyInputManagerConfig.horizontal),
				0,
				Input.GetAxis(legacyInputManagerConfig.vertical)
			);
		}
#endif

#if ENABLE_INPUT_SYSTEM
		Vector3 inputVelocity_is;

		void ReceiveVelocity_is()
		{
			inputVelocity = inputVelocity_is;
		}

		protected void OnMove(InputValue value)
		{
			var raw = value.Get<Vector2>();
			inputVelocity_is = new(raw.x, 0, raw.y);
		}
#endif
		#endregion

		#region Orientation
		Vector3 inputRotation;

#if ENABLE_LEGACY_INPUT_MANAGER
		void ReceiveRotation_lim()
		{
			inputRotation = new(
				Input.GetAxis(legacyInputManagerConfig.mouseY),
				Input.GetAxis(legacyInputManagerConfig.mouseX)
			);
		}
#endif

#if ENABLE_INPUT_SYSTEM
		Vector3 inputRotation_is;

		void ReceiveRotation_is()
		{
			inputRotation = inputRotation_is;
		}

		protected void OnOrient(InputValue value)
		{
			var raw = value.Get<Vector2>();
			inputRotation_is = new(raw.y, raw.x);
		}
#endif
		#endregion

		#region Jumping
		bool inputJumping;

#if ENABLE_LEGACY_INPUT_MANAGER
		void ReceiveJumping_lim()
		{
			inputJumping = Input.GetButtonDown(legacyInputManagerConfig.Jump);
		}
#endif

#if ENABLE_INPUT_SYSTEM
		bool inputJumping_is;

		void ReceiveJumping_is()
		{
			inputJumping = inputJumping_is;
			inputJumping_is = false;
		}

		protected void OnJump(InputValue value)
		{
			inputJumping_is = value.isPressed;
		}
#endif
		#endregion
	}
}