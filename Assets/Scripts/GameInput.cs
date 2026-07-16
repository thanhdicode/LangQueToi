using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    // ----------------------------------------------------------
    // Events
    // ----------------------------------------------------------
    public event EventHandler OnUseToolAction;

    public event EventHandler OnToggleToolWheelAction;

    public event EventHandler OnToggleInventoryAction;

    public event EventHandler OnTalkToNPCAction;

    public event EventHandler OnTogglePausePanelAction;

    // ----------------------------------------------------------
    // Private state  (ENCAPSULATION)
    // ----------------------------------------------------------
    private InputSystem_Actions _inputActions;

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Player.Enable();
        _inputActions.Player.UseTool.performed += OnUseToolPerformed;
        _inputActions.Player.ToggleToolWheel.performed += ToggleToolWheel_performed;
        _inputActions.Player.ToggleInventory.performed += ToggleInventory_performed;
        _inputActions.Player.TalkToNPC.performed += TalkToNPC_performed;
        _inputActions.Player.TogglePausePanel.performed += TogglePausePanel_performed;
    }

    private void TalkToNPC_performed(InputAction.CallbackContext obj)
    {
        if (PauseManager.IsPaused) return;
        OnTalkToNPCAction?.Invoke(this, EventArgs.Empty);
    }

    private void TogglePausePanel_performed(InputAction.CallbackContext obj)
    {
        // Always fires — PauseManager itself handles the ESC toggle logic.
        OnTogglePausePanelAction?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleInventory_performed(InputAction.CallbackContext obj)
    {
        if (PauseManager.IsPaused) return;
        OnToggleInventoryAction?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleToolWheel_performed(InputAction.CallbackContext obj)
    {
        if (PauseManager.IsPaused) return;
        OnToggleToolWheelAction?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        if (_inputActions == null) return;

        _inputActions.Player.UseTool.performed -= OnUseToolPerformed;
        _inputActions.Player.ToggleToolWheel.performed -= ToggleToolWheel_performed;
        _inputActions.Player.ToggleInventory.performed -= ToggleInventory_performed;
        _inputActions.Player.TalkToNPC.performed -= TalkToNPC_performed;
        _inputActions.Player.TogglePausePanel.performed -= TogglePausePanel_performed;
        _inputActions.Player.Disable(); // required before Dispose to suppress InputSystem leak warning
        _inputActions.Dispose();
    }

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------
    public Vector2 GetMovementVectorNormalized()
    {
        return _inputActions.Player.Move.ReadValue<Vector2>().normalized;
    }

    // ----------------------------------------------------------
    // Private handlers
    // ----------------------------------------------------------
    private void OnUseToolPerformed(InputAction.CallbackContext ctx)
    {
        OnUseToolAction?.Invoke(this, EventArgs.Empty);
    }
}
