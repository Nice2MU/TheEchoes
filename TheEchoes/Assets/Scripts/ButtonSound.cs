using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler,
    ISelectHandler,
    ISubmitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.PlayUiPoint();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.PlayUiClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        UIManager.PlayUiPoint();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        UIManager.PlayUiClick();
    }
}