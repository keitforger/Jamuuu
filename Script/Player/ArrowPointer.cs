using UnityEngine;

public class ArrowPointer : MonoBehaviour
{
    public Transform target; // target world position
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (target == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position + offset);
        transform.position = screenPos;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
