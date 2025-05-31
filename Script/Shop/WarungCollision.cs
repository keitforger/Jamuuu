using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class WarungCollision : MonoBehaviour
{
    public GameObject masukButton;

    void Start()
    {
        masukButton.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            masukButton.SetActive(true);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            masukButton.SetActive(false);
        }
    }
}
