using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Petropolis.Minigames.Pong
{
    /// <summary>
    /// Player-controlled paddle. Moves vertically between <see cref="minY"/> / <see cref="maxY"/>
    /// using arrow keys, W/S, or the left stick / D-pad on a gamepad.
    /// </summary>
    public class PongPaddle : MonoBehaviour
    {
        [SerializeField] float speed = 10f;
        [SerializeField] float minY = -4f;
        [SerializeField] float maxY = 4f;

        void Update()
        {
            float input = ReadVerticalInput();
            if (Mathf.Approximately(input, 0f)) return;

            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y + input * speed * Time.deltaTime, minY, maxY);
            transform.position = pos;
        }

        static float ReadVerticalInput()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) v += 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) v -= 1f;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                v += pad.leftStick.y.ReadValue();
                if (pad.dpad.up.isPressed) v += 1f;
                if (pad.dpad.down.isPressed) v -= 1f;
            }
#else
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) v -= 1f;
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }
    }
}
