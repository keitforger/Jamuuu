using UnityEngine;

public class HouseSleepTrigger2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            UIManager.Instance.TrySleepAndSkipNight();
        }
    }
}