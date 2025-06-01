using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject panel;
    public bool isAlmanacPanel = false; // assign true di inspector untuk tombol Almanac!

    public void ToggleThisPanel()
    {
        if (panel != null && UIManager.Instance != null)
        {
            UIManager.Instance.TogglePanel(panel);

            // Jika ini panel Almanac, selalu refresh datanya!
            if (isAlmanacPanel && AlmanacSystem.Instance != null)
            {
                AlmanacSystem.Instance.OpenAlmanac();
            }

            // Pindahkan panel ke paling atas di canvas
            panel.transform.SetAsLastSibling();
        }
    }
}