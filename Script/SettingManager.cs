
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

public class SettingManager : UdonSharpBehaviour
{
    [SerializeField] TMP_Dropdown langDropDown;
    [SerializeField] TextMeshProUGUI currentMaxEndText;
    [SerializeField] Slider xSlider;
    [SerializeField] Slider ySlider;
    [SerializeField] Slider zSlider;

    const int limitMaxEnd = 10;
    const int limitMinEnd = 1;
    [UdonSynced] int currentMaxEnd = 1;

    public void AddCurrentMaxEnd()
    {
        if(!Networking.IsOwner(Networking.LocalPlayer,gameObject)) { Networking.SetOwner(Networking.LocalPlayer, gameObject);  }

        if(currentMaxEnd >= limitMaxEnd) { return; }
        currentMaxEnd++;
        RequestSerialization();
        SetCurrentMaxEndText();
    }
    public void ReduceCurrentMaxEnd()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, gameObject)) { Networking.SetOwner(Networking.LocalPlayer, gameObject); }

        if (currentMaxEnd <= limitMinEnd) { return; }
        currentMaxEnd--;
        RequestSerialization();
        SetCurrentMaxEndText();
    }
    public override void OnDeserialization()
    {
        SetCurrentMaxEndText();
    }
    private void SetCurrentMaxEndText()
    {
        currentMaxEndText.text = currentMaxEnd.ToString();
    }
    public int GetCurrentLanguage()
    {
        return langDropDown.value;
    }
    public int GetMaxEnd()
    {
        return currentMaxEnd;
    }
    public Vector3 GetDisplayPosition()
    {
        return new Vector3(xSlider.value,ySlider.value, zSlider.value);
    }
}
