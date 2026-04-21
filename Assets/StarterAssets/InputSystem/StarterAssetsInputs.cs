using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[DefaultExecutionOrder(-100)]
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		bool _uiMenuOpen;

		/// <summary>Pause / fullscreen UI: keep cursor visible and ignore FPS lock-on-focus behavior.</summary>
		public void SetUiMenuOpen(bool open)
		{
			_uiMenuOpen = open;
			if (open)
				SetCursorState(false);
			else
				SetCursorState(cursorLocked);
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		void Update()
		{
			if (WorldPauseController.IsPaused)
			{
				look = Vector2.zero;
				move = Vector2.zero;
				jump = false;
				sprint = false;
				return;
			}

			if (NpcProximityChatOpener.IsWorldNpcChatOpen)
			{
				look = Vector2.zero;
				move = Vector2.zero;
				jump = false;
				sprint = false;
				return;
			}

			if (InventoryPanelController.IsOpen)
			{
				look = Vector2.zero;
				move = Vector2.zero;
				jump = false;
				sprint = false;
				return;
			}

			if (TeleportMenuController.IsOpen)
			{
				look = Vector2.zero;
				move = Vector2.zero;
				jump = false;
				sprint = false;
			}
		}

		private void Start()
		{
			SetCursorState(cursorLocked);
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (_uiMenuOpen || WorldPauseController.IsPaused)
			{
				SetCursorState(false);
				return;
			}
			// When game loses focus, unlock and show cursor so user can interact with other windows.
			// When game gains focus, lock and hide cursor if cursorLocked is set.
			SetCursorState(hasFocus && cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			if (_uiMenuOpen || WorldPauseController.IsPaused)
			{
				Cursor.lockState = CursorLockMode.None;
				// Npc chat calls SetUiMenuOpen every LateUpdate; gamepad idle-hide must still hide the OS cursor.
				Cursor.visible = !CursorVisibilityKeeper.SuppressMenuHardwareCursor;
				return;
			}
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
		}
	}
	
}