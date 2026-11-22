using UnityEngine;
using UnityEngine.InputSystem;

namespace OperacionCuatro
{
    /// <summary>
    /// Lector de entrada por jugador. Combina el mapa de teclado asignado
    /// con un gamepad dedicado (si hay uno disponible).
    /// </summary>
    public class PlayerInputReader
    {
        private const float StickDeadzone = 0.25f;

        private readonly InputActionMap map;
        private readonly InputAction moveAction;
        private readonly InputAction jumpAction;
        private readonly InputAction fireAction;
        private readonly InputAction bombAction;

        private bool enabled;

        public readonly int GamepadSlot;

        public PlayerInputReader(InputActionAsset asset, string mapName, int gamepadSlot)
        {
            GamepadSlot = gamepadSlot;

            if (asset == null)
            {
                Debug.LogError("[OC] InputActionAsset no asignado en PlayerInputReader.");
                return;
            }

            map = asset.FindActionMap(mapName, false);
            if (map == null)
            {
                Debug.LogError("[OC] No se encontró el mapa de acciones '" + mapName + "'.");
                return;
            }

            moveAction = map.FindAction("Move", false);
            jumpAction = map.FindAction("Jump", false);
            fireAction = map.FindAction("Fire", false);
            bombAction = map.FindAction("Bomb", false);
        }

        public void Enable()
        {
            if (map == null || enabled) return;
            map.Enable();
            enabled = true;
        }

        public void Disable()
        {
            if (map == null || !enabled) return;
            map.Disable();
            enabled = false;
        }

        private Gamepad Pad
        {
            get
            {
                var all = Gamepad.all;
                return all.Count > GamepadSlot ? all[GamepadSlot] : null;
            }
        }

        public Vector2 Move
        {
            get
            {
                Vector2 v = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
                var pad = Pad;
                if (pad != null)
                {
                    var stick = pad.leftStick.ReadValue();
                    if (stick.magnitude < StickDeadzone) stick = Vector2.zero;
                    if (stick.sqrMagnitude > v.sqrMagnitude) v = stick;

                    var dpad = pad.dpad.ReadValue();
                    if (dpad.sqrMagnitude > v.sqrMagnitude) v = dpad;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public bool JumpPressed
        {
            get
            {
                if (jumpAction != null && jumpAction.WasPressedThisFrame()) return true;
                var pad = Pad;
                return pad != null && pad.buttonSouth.wasPressedThisFrame;
            }
        }

        public bool JumpHeld
        {
            get
            {
                if (jumpAction != null && jumpAction.IsPressed()) return true;
                var pad = Pad;
                return pad != null && pad.buttonSouth.isPressed;
            }
        }

        public bool FirePressed
        {
            get
            {
                if (fireAction != null && fireAction.WasPressedThisFrame()) return true;
                var pad = Pad;
                return pad != null && pad.buttonWest.wasPressedThisFrame;
            }
        }

        public bool FireHeld
        {
            get
            {
                if (fireAction != null && fireAction.IsPressed()) return true;
                var pad = Pad;
                return pad != null && pad.buttonWest.isPressed;
            }
        }

        public bool BombPressed
        {
            get
            {
                if (bombAction != null && bombAction.WasPressedThisFrame()) return true;
                var pad = Pad;
                if (pad == null) return false;
                return pad.buttonNorth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
            }
        }

        public bool CrouchHeld
        {
            get
            {
                var pad = Pad;
                return Move.y < -0.5f || (pad != null && pad.buttonEast.isPressed);
            }
        }
    }
}
