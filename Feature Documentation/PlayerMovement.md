# Player movement

## Overview

Third-person character movement is implemented in `ThirdPersonController` (`Assets/SCRIPTS/Player/ThirdPersonController.cs`). It is a project-specific copy of Unity Starter Assets, extended for Petropolis (character selection, pause integration, and locomotion–animation mapping).

## Components

| Piece | Role |
|--------|------|
| `CharacterController` | Collision and non-physics movement. |
| `StarterAssetsInputs` | Reads move, look, jump, sprint, analog movement flags. |
| `PlayerInput` (Input System) | Binds the generated input actions. |
| `Animator` (optional) | Locomotion, jump, and fall when present. |
| `CinemachineCameraTarget` | Yaw/pitch target the virtual camera follows. |
| `MainCamera` (tag) | World yaw for **camera-relative** move direction. |

## Movement and rotation

- **Planar movement** uses `CharacterController.Move` with a horizontal direction derived from `input.move` and the main camera’s Y rotation so W moves “into” the view.
- **Speed** eases toward walk (`MoveSpeed`) or sprint (`SprintSpeed`) at `SpeedChangeRate`. No input zeroes the target speed.
- **Sprint** toggles the higher cap via `StarterAssetsInputs.sprint`.
- **Facing** the move direction uses `RotationSmoothTime` and `Mathf.SmoothDampAngle` on the character’s Y axis when there is non-zero move input.
- **Jump** sets vertical velocity from `JumpHeight` and `Gravity` while grounded; a short `JumpTimeout` limits repeat jumps. A separate fall timeout controls the “in air” animator state.
- **Grounding** is a `Physics.CheckSphere` at the feet using `GroundedOffset`, `GroundedRadius`, and `GroundLayers` (must include the floor layer or grounded checks fail).

## Camera

- In `LateUpdate`, **look** input adjusts `_cinemachineTargetYaw` / `_cinemachineTargetPitch` on `CinemachineCameraTarget`, clamped by `TopClamp` / `BottomClamp`.
- `LockCameraPosition` and `CameraAngleOverride` allow locking or offsetting the rig.
- While **NPC world chat** is open (`NpcProximityChatOpener.IsWorldNpcChatOpen`), `CameraRotation` is skipped so the player is not still orbiting the camera.

## Animation

- The **Speed** float is not raw m/s. `StarterAssetsLocomotionBlendSpeed` maps planar speed to the blend tree (idle / walk / run thresholds 0 / 2 / 6).
- **Motion speed** uses input magnitude, or 1.0 when not using analog movement.
- **Animator** playback speed: base `AnimatorSpeed`, with an extra `WalkAnimationPlaybackScale` when grounded, walking, and not sprinting (foot cadence only; does not change move speed).
- **Footsteps** and **landing** can be driven by animation events (`OnFootstep`, `OnLand`) using `AudioSettings.SFXVolume`.

## When movement stops

- If `WorldPauseController.IsPaused` is true, `Update` and `LateUpdate` return early: no move, jump, gravity integration, or camera rotation while paused.
- The controller file header notes a third-person / capsule pattern with animator null checks for flexible prefabs.

## Related scripts

- `PlayerFollow` — companion follow logic, separate from the player’s `CharacterController` path.
- `WorldPauseController` — static `IsPaused` flag used by the player and many UI / world systems.

## File reference

- `Assets/SCRIPTS/Player/ThirdPersonController.cs`
