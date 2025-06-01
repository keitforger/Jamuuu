using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            eventData.pointerDrag.transform.SetParent(transform);
            eventData.pointerDrag.transform.position = transform.position;
        }
    }

    public string GetItemName()
    {
        if (transform.childCount > 0)
        {
            return transform.GetChild(0).name;
        }
        return null;
    }
}
