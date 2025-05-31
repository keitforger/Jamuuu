using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string itemName;
    public int jumlah = 1;
    public Image itemImage;
    public Item Item;  // ✅ Tambahkan ini kembali jika kamu memang butuh akses ke data item

    [HideInInspector] public Transform originalParent;
    [HideInInspector] public Vector3 originalPosition;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;

        transform.SetParent(transform.root); // agar tidak terpotong UI
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
    }

    public void KurangiJumlah()
    {
        jumlah--;
        if (jumlah <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            // Update text jumlah jika ada
            Text jumlahText = GetComponentInChildren<Text>();
            if (jumlahText != null)
            {
                jumlahText.text = jumlah.ToString();
            }
        }
    }
}
