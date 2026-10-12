using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PongUIPanelFocus : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;

    private Coroutine focusRoutine;

    private void OnEnable()
    {
        focusRoutine = StartCoroutine(SelectAfterOpening());
    }

    private IEnumerator SelectAfterOpening()
    {
        yield return null;
        focusRoutine = null;

        if (!isActiveAndEnabled || EventSystem.current == null)
            yield break;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null && current.transform.IsChildOf(transform))
        {
            Selectable control = current.GetComponent<Selectable>();

            if (control != null && control.IsActive() && control.IsInteractable())
                yield break;
        }

        if (firstSelected != null &&
            firstSelected.IsActive() && firstSelected.IsInteractable())
        {
            firstSelected.Select();
        }
    }

    private void OnDisable()
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        if (EventSystem.current == null)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current != null && current.transform.IsChildOf(transform))
            EventSystem.current.SetSelectedGameObject(null);
    }
}