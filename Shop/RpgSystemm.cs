using UnityEngine;
using UnityEngine.UI;

public class RpgSystemm : MonoBehaviour
{
    [SerializeField]
    UnityEngine.UI.Text txtKoin;

    public DataGame dtg;
    string namaPP = "datagame";

    void Start()
    {
        dtg = ManagerPP<DataGame>.Get(namaPP);
        txtKoin.text = dtg.koin.ToString();
    }

    void Update()
    {

    }
}
