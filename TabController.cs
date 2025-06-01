using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button tabJamuButton;
    public Button tabRempahButton;

    [Header("Content Panels")]
    public GameObject contentJamu;
    public GameObject contentRempah;

    private int currentTabIndex = -1; // -1 supaya tab pertama tetap trigger

    private void Start()
    {
        tabJamuButton.onClick.AddListener(() => SwitchTab(0));
        tabRempahButton.onClick.AddListener(() => SwitchTab(1));

        // Default to first tab
        SwitchTab(0);
    }

    private void SwitchTab(int index)
    {
        // Selalu update, bahkan jika tab yang sama
        currentTabIndex = index;

        bool isJamu = index == 0;

        contentJamu.SetActive(isJamu);
        contentRempah.SetActive(!isJamu);

        // Panggil sistem almanac untuk muat data
        if (AlmanacSystem.Instance != null)
        {
            if (isJamu)
                AlmanacSystem.Instance.SetFilter("Jamu");
            else
                AlmanacSystem.Instance.SetFilter("Rempah");
        }
    }
}
