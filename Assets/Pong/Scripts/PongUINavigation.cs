using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(EventSystem))]
[DefaultExecutionOrder(-100)]
public class PongUINavigation : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float repeatDelay = 0.35f;
    [SerializeField, Min(0.02f)] private float repeatInterval = 0.12f;

    private EventSystem eventSystem;
    private bool previousNavigationSetting;
    private bool previousConfirmHeld;
    private bool exclusiveScreenInput;

    private GameObject pressedObject;
    private PongUIFeedback pressedFeedback;

    private MoveDirection heldDirection = MoveDirection.None;
    private float nextMoveTime;

    public bool IsKeyboardPressActive => pressedObject != null;

    public void SetExclusiveScreenInput(bool exclusive)
    {
        exclusiveScreenInput = exclusive;
        CancelKeyboardPress();
        ResetMoveRepeat();
        previousConfirmHeld = IsConfirmHeld(Keyboard.current);
    }
    private void Awake()
    {
        eventSystem = GetComponent<EventSystem>();
    }

    private void OnEnable()
    {
        previousNavigationSetting = eventSystem.sendNavigationEvents;
        eventSystem.sendNavigationEvents = false;

        previousConfirmHeld = IsConfirmHeld(Keyboard.current);
        ResetMoveRepeat();
    }

    private void OnDisable()
    {
        CancelKeyboardPress();

        if (eventSystem != null)
            eventSystem.sendNavigationEvents = previousNavigationSetting;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelKeyboardPress();
            ResetMoveRepeat();
            previousConfirmHeld = IsConfirmHeld(Keyboard.current);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool confirmHeld = IsConfirmHeld(keyboard);
        bool confirmStarted = confirmHeld && !previousConfirmHeld;

        previousConfirmHeld = confirmHeld;

        if (exclusiveScreenInput)
        {
            CancelKeyboardPress();
            ResetMoveRepeat();
            return;
        }
        Selectable current = GetSelectedControl();

        if (keyboard == null || current == null || IsEditingText(current) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed))
        {
            CancelKeyboardPress();
            ResetMoveRepeat();
            return;
        }

        if (pressedObject != null &&
            pressedObject != eventSystem.currentSelectedGameObject)
        {
            CancelKeyboardPress();
        }

        if (confirmStarted &&
            ExecuteEvents.CanHandleEvent<ISubmitHandler>(current.gameObject))
        {
            pressedObject = current.gameObject;
            pressedFeedback = current.GetComponent<PongUIFeedback>();

            if (pressedFeedback != null)
                pressedFeedback.SetKeyboardHeld(true);

            ResetMoveRepeat();
        }

        if (!confirmHeld && pressedObject != null)
        {
            SubmitOnRelease();
            return;
        }

        if (confirmHeld)
            return;

        MoveDirection direction = ReadDirection(keyboard);

        if (direction == MoveDirection.None)
        {
            ResetMoveRepeat();
            return;
        }

        if (direction != heldDirection)
        {
            heldDirection = direction;
            MoveSelection(current, direction);
            nextMoveTime = Time.unscaledTime + repeatDelay;
        }
        else if (Time.unscaledTime >= nextMoveTime)
        {
            MoveSelection(current, direction);
            nextMoveTime = Time.unscaledTime + repeatInterval;
        }
    }

    private Selectable GetSelectedControl()
    {
        GameObject selected = eventSystem.currentSelectedGameObject;

        if (selected == null)
            return null;

        Selectable control = selected.GetComponent<Selectable>();

        return control != null && control.IsActive() && control.IsInteractable()
            ? control
            : null;
    }

    private static bool IsEditingText(Selectable control)
    {
        TMP_InputField tmpInput = control as TMP_InputField;
        InputField input = control as InputField;

        return (tmpInput != null && tmpInput.isFocused) ||
            (input != null && input.isFocused);
    }

    private static bool IsConfirmHeld(Keyboard keyboard)
    {
        return keyboard != null &&
            (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
    }

    private static MoveDirection ReadDirection(Keyboard keyboard)
    {
        int vertical =
            ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0) -
            ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0);

        int horizontal =
            ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0) -
            ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);

        if (vertical > 0) return MoveDirection.Up;
        if (vertical < 0) return MoveDirection.Down;
        if (horizontal > 0) return MoveDirection.Right;
        if (horizontal < 0) return MoveDirection.Left;

        return MoveDirection.None;
    }

    private void MoveSelection(Selectable current, MoveDirection direction)
    {
        Selectable next = null;

        switch (direction)
        {
            case MoveDirection.Up:
                next = current.FindSelectableOnUp();
                break;

            case MoveDirection.Down:
                next = current.FindSelectableOnDown();
                break;

            case MoveDirection.Left:
                next = current.FindSelectableOnLeft();
                break;

            case MoveDirection.Right:
                next = current.FindSelectableOnRight();
                break;
        }

        if (next != null && next.IsActive() && next.IsInteractable())
            next.Select();
    }

    private void SubmitOnRelease()
    {
        GameObject target = pressedObject;
        CancelKeyboardPress();

        if (target == null || eventSystem.currentSelectedGameObject != target)
            return;

        Selectable control = target.GetComponent<Selectable>();

        if (control != null && control.IsActive() && control.IsInteractable())
        {
            ExecuteEvents.Execute(
                target,
                new BaseEventData(eventSystem),
                ExecuteEvents.submitHandler);
        }
    }

    private void CancelKeyboardPress()
    {
        if (pressedFeedback != null)
            pressedFeedback.SetKeyboardHeld(false);

        pressedObject = null;
        pressedFeedback = null;
    }

    private void ResetMoveRepeat()
    {
        heldDirection = MoveDirection.None;
        nextMoveTime = 0f;
    }
}