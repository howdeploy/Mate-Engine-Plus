using UnityEngine;
using UnityEngine.EventSystems;

public sealed class LinuxWindowDragHandle : MonoBehaviour, IPointerDownHandler,
    IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private bool dragging;

    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left ||
            MenuActions.IsMovementBlocked() || WindowManager.Instance == null) return;
        dragging = true;
        WindowManager.Instance.OnPointerDown(data);
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (!dragging || data.button != PointerEventData.InputButton.Left) return;
        dragging = false;
        if (WindowManager.Instance != null) WindowManager.Instance.OnPointerUp(data);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (WindowManager.Instance != null) WindowManager.Instance.OnPointerEnter(data);
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (WindowManager.Instance != null) WindowManager.Instance.OnPointerExit(data);
    }

    private void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        if (WindowManager.Instance != null) WindowManager.Instance.OnPointerUp(null);
    }
}
