using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.FPS.Gameplay
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Tooltip("Sensitivity multiplier for moving the camera around")]
        public float LookSensitivity = 1f;

        [Tooltip("Additional sensitivity multiplier for WebGL")]
        public float WebglLookSensitivityMultiplier = 0.25f;

        [Tooltip("Used to flip the vertical input axis")]
        public bool InvertYAxis = false;

        [Tooltip("Used to flip the horizontal input axis")]
        public bool InvertXAxis = false;

        InputSystem_Actions m_InputActions;
        GameFlowManager m_GameFlowManager;

        Vector2 m_LookInput;
        Vector2 m_MoveInput;

        void Awake()
        {
            m_InputActions = new InputSystem_Actions();
        }

        void OnEnable()
        {
            m_InputActions.Player.Enable();
        }

        void OnDisable()
        {
            m_InputActions.Player.Disable();
        }

        void Start()
        {
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();
            DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, PlayerInputHandler>(m_GameFlowManager, this);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public bool CanProcessInput()
        {
            return Cursor.lockState == CursorLockMode.Locked && !m_GameFlowManager.GameIsEnding;
        }

        public Vector3 GetMoveInput()
        {
            if (CanProcessInput())
            {
                Vector2 move = m_InputActions.Player.Move.ReadValue<Vector2>();
                return new Vector3(move.x, 0f, move.y);
            }

            return Vector3.zero;
        }

        public float GetLookInputsHorizontal()
        {
            if (CanProcessInput())
            {
                float lookX = m_InputActions.Player.Look.ReadValue<Vector2>().x;
                if (InvertXAxis) lookX *= -1f;
                return lookX * LookSensitivity * 0.01f;
            }
            return 0f;
        }

        public float GetLookInputsVertical()
        {
            if (CanProcessInput())
            {
                float lookY = m_InputActions.Player.Look.ReadValue<Vector2>().y;
                if (InvertYAxis) lookY *= -1f;
                return lookY * LookSensitivity * 0.01f;
            }
            return 0f;
        }

        public bool GetJumpInputDown()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Jump.WasPressedThisFrame();
            }
            return false;
        }

        public bool GetJumpInputHeld()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Jump.IsPressed();
            }
            return false;
        }

        public bool GetFireInputDown()
        {
            if(CanProcessInput())
            {
                return m_InputActions.Player.Attack.WasPressedThisFrame();
            }
            return false;
        }

        public bool GetFireInputReleased()
        {
            if(CanProcessInput())
            {
                return m_InputActions.Player.Attack.WasReleasedThisFrame();
            }
            return false;
        }

        public bool GetFireInputHeld()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Attack.IsPressed();
            }
            return false;
        }

        public bool GetAimInputHeld()
        {
            // This action was not in the input actions asset, returning false.
            // You can add an "Aim" action to the "Player" action map.
            return false;
        }

        public bool GetSprintInputHeld()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Sprint.IsPressed();
            }
            return false;
        }

        public bool GetCrouchInputDown()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Crouch.WasPressedThisFrame();
            }
            return false;
        }

        public bool GetCrouchInputReleased()
        {
            if (CanProcessInput())
            {
                return m_InputActions.Player.Crouch.WasReleasedThisFrame();
            }
            return false;
        }

        public bool GetReloadButtonDown()
        {
            // This action was not in the input actions asset, returning false.
            // You can add a "Reload" action to the "Player" action map.
            return false;
        }

        public int GetSwitchWeaponInput()
        {
            if (CanProcessInput())
            {
                if (m_InputActions.Player.Previous.WasPressedThisFrame())
                    return -1;
                if (m_InputActions.Player.Next.WasPressedThisFrame())
                    return 1;
            }
            return 0;
        }

        public int GetSelectWeaponInput()
        {
            // This is better handled by listening to OnNumber in a different script
            // or by adding 1-9 actions to the input asset.
            // For now, returning 0.
            return 0;
        }
    }
}