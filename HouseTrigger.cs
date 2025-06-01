using UnityEngine;

public class HouseEntryTrigger : MonoBehaviour
{
    public Transform spawnPointOutside; // Drag posisi depan rumah di inspector
    public float nightHour = 18f;
    public float morningHour = 8f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Pastikan MC punya tag "Player"
        {
            float currentTime = FindAnyObjectByType<NPCSpawner>().GetCurrentTime();
            if (currentTime >= nightHour)
            {
                StartCoroutine(HandleSleepTransition(other.gameObject));
            }
        }
    }

    private System.Collections.IEnumerator HandleSleepTransition(GameObject mc)
    {
        // 1. Fade in
        yield return UIManager.Instance.FadeInRoutine();

        // 2. Set waktu ke pagi
        var npcSpawner = FindAnyObjectByType<NPCSpawner>();
        npcSpawner.SetDayTime();

        // 3. Pindahkan MC ke luar rumah
        mc.transform.position = spawnPointOutside.position;

        // 4. Fade out
        yield return UIManager.Instance.FadeOutRoutine();
    }
}