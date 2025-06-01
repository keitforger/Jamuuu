using UnityEngine;

public class ProximityButton : MonoBehaviour
{
    public GameObject buttonPanel; // Panel yang berisi button
    public string playerTag = "Player"; // Pastikan MC punya tag "Player"

    private void Start()
    {
        // Di awal panel disembunyikan
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kalau yang masuk area adalah player
        if (other.CompareTag(playerTag))
        {
            if (buttonPanel != null)
            {
                buttonPanel.SetActive(true); // Tampilkan button
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Kalau player keluar dari area
        if (other.CompareTag(playerTag))
        {
            if (buttonPanel != null)
            {
                buttonPanel.SetActive(false); // Sembunyikan button
            }
        }
    }
}
