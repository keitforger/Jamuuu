using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Panel Tutorial")]
    public GameObject panelTutorial;
    public TMP_Text tutorialText;
    public Button nextButton;

    [Header("Referensi UI Game")]
    public GameObject inventoryPanel;
    public GameObject almanacPanel;
    public GameObject shopPanel;
    public GameObject combinePanel;

    [Header("Referensi Lokasi & UI Tambahan")]
    public Transform teleportTriggerTransform; // Posisi pintu rumah
    public Transform warungTransform;          // Posisi warung
    public GameObject warungMasukButton;       // Tombol masuk warung (harus diaktifkan setelah dekat)

    [Header("Tutorial Arrow")]
    public GameObject arrowObject; // UI panah
    private ArrowPointer arrowScript;


    private int langkah = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        panelTutorial.SetActive(false);
        nextButton.onClick.AddListener(LanjutTutorial);
    }

    void Start()
    {
        if (!GameManager.instance.HasWatchedCutscene())
        {
            StartCoroutine(TutorialAwal());
        }

        arrowScript = arrowObject.GetComponent<ArrowPointer>();
        arrowObject.SetActive(false);

    }

    IEnumerator TutorialAwal()
    {
        langkah = 0;
        panelTutorial.SetActive(true);
        tutorialText.text = "Gunakan tombol panah atau WASD untuk bergerak.";
        Vector3 startPos = PlayerMovement.Instance.transform.position;

        yield return WaitUntilWithTimeout(() => PemainSudahBergerak(startPos));
        langkah++; // Lanjut ke langkah selanjutnya

        tutorialText.text = "Selamat datang di dunia jamu!\nMari kita pelajari dasar-dasarnya.";
        yield return new WaitUntil(() => langkah > 1);

        tutorialText.text = "Ini adalah koin yang kamu miliki untuk berbelanja.";
        GameManager.instance.AddMoney(500);
        yield return new WaitUntil(() => langkah > 1);

        tutorialText.text = "Level kamu akan meningkat setelah membuat jamu.";
        yield return new WaitUntil(() => langkah > 2);

        tutorialText.text = "Waktu berjalan, mempengaruhi tanamanmu.";
        yield return new WaitUntil(() => langkah > 3);

        tutorialText.text = "Ini inventory kamu. Segala barangmu disimpan di sini.";
        Inventory.Instance.show();
        yield return new WaitUntil(() => langkah > 4);
        Inventory.Instance.hide();

        tutorialText.text = "Almanak berisi info tentang rempah dan jamu yang kamu temukan.";
        AlmanacSystem.Instance.OpenAlmanac();
        yield return new WaitUntil(() => langkah > 5);
        AlmanacSystem.Instance.CloseAlmanac();

        tutorialText.text = "Sekarang, ayo beli bibit di toko.";
        shopPanel.SetActive(true);
        yield return WaitUntilWithTimeout(() => Inventory.InstanceHasBenih());

        tutorialText.text = "Bagus! Sekarang tanam benihmu di ladang.";
        yield return WaitUntilWithTimeout(() => PlantingSystem.InstanceTanamSelesai());

        tutorialText.text = "Beli bahan tambahan di toko untuk membuat jamu.";
        shopPanel.SetActive(true);
        yield return WaitUntilWithTimeout(() => Inventory.InstanceHasBahan());

        tutorialText.text = "Ayo pulang dan buat jamu pertamamu!";
        combinePanel.SetActive(true);
        yield return WaitUntilWithTimeout(() => CraftingSukses());

        tutorialText.text = "Ayo masuk ke rumah! Pergilah ke pintu.";
        arrowObject.SetActive(true);
        arrowScript.SetTarget(teleportTriggerTransform);
        yield return WaitUntilWithTimeout(() =>
        {
            Vector3 jarak = PlayerMovement.Instance.transform.position - teleportTriggerTransform.position;
            return jarak.magnitude < 1f; // atau kondisi lebih spesifik
        });

        arrowObject.SetActive(false);

        tutorialText.text = "Tabrak warung untuk masuk mode crafting!";
        arrowObject.SetActive(true);
        arrowScript.SetTarget(warungTransform); // drag warung GameObject

        yield return WaitUntilWithTimeout(() =>
        {
            return warungMasukButton.activeSelf;
        });

        arrowObject.SetActive(false);


        tutorialText.text = "Selamat! Kamu naik dari Level 1 ke Level 2!";
        GameManager.instance.SetCutsceneWatched(); // tandai tutorial selesai
        yield return new WaitForSeconds(2f);

        panelTutorial.SetActive(false);
    }

    IEnumerator WaitUntilWithTimeout(System.Func<bool> condition, float timeout = 10f)
    {
        float timer = 0;
        while (!condition())
        {
            if (timer > timeout) yield break;
            timer += Time.deltaTime;
            yield return null;
        }
    }


    void LanjutTutorial()
    {
        langkah++;
    }

    // Untuk pengecekan kondisi crafting sukses (implementasi bisa di CraftingManager)
    private bool CraftingSukses()
    {
        // Ganti dengan flag boolean dari CraftingManager saat crafting berhasil
        return CraftingManagerHelper.TutorialCraftingBerhasil;
    }

    private bool PemainSudahBergerak(Vector3 awal)
    {
        if (!PlayerMovement.Instance) return false;
        float jarak = Vector3.Distance(awal, PlayerMovement.Instance.transform.position);
        return jarak > 0.5f;
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("datagame"); // Atau `hasWatchedCutscene`
        Debug.Log("Tutorial di-reset.");
    }

}

public static class CraftingManagerHelper
{
    public static bool TutorialCraftingBerhasil = false;
}

