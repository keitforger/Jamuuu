using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BajajTeleportManager : MonoBehaviour
{
    [System.Serializable]
    public class CityData
    {
        public string namaKota;
        public Transform posisiTeleport;
        public int hargaTeleport;
        public Button tombolKota;
    }

    public GameObject panelTeleport;
    public List<CityData> daftarKota;
    public Transform player;
    public Text notifText;
    public Camera MainCamera;


    private void Start()
    {
        if (panelTeleport != null) panelTeleport.SetActive(false);

        foreach (var kota in daftarKota)
        {
            if (kota.tombolKota != null)
            {
                string nama = kota.namaKota;
                kota.tombolKota.onClick.AddListener(() => TeleportKeKota(nama));
            }
        }
        if (notifText != null)
            notifText.text = "";
    }

    void OnMouseDown()
    {
        OnBajajClicked();
    }

    public void OnBajajClicked()
    {
        if (panelTeleport != null)
            panelTeleport.SetActive(true);
    }

    public void TeleportKeKota(string namaKota)
    {
        var kota = daftarKota.Find(k => k.namaKota == namaKota);
        if (kota == null || kota.posisiTeleport == null) return;

        if (GameManager.instance != null && GameManager.instance.SpendMoney(kota.hargaTeleport))
        {
            player.position = kota.posisiTeleport.position;
            if (panelTeleport != null)
                panelTeleport.SetActive(false); // Panel hanya tertutup jika teleport berhasil
            Camera.main.transform.position = new Vector3(
            player.position.x,
            player.position.y,
            -10f
            );
        }
        else
        {
            notifText.text = $"Uang tidak cukup untuk teleport ke {kota.namaKota}!";
            // Panel tetap terbuka
        }
    }
}