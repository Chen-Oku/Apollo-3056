using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShieldMeterUI : MonoBehaviour
{
    public ShieldController shield;
    public Slider slider; // or Image with fillAmount
    public TextMeshProUGUI readyText;

    void Start()
    {
        if (shield != null)
        {
            shield.OnChargeChanged.AddListener(OnChargeChanged);
            shield.OnShieldActivated.AddListener(OnActivated);
            shield.OnShieldDeactivated.AddListener(OnDeactivated);
            // ensure slider max matches shield scale
            if (slider != null) slider.maxValue = shield.maxCharge;
            OnChargeChanged(shield.CurrentCharge);
        }
    }

    void OnDestroy()
    {
        if (shield != null)
        {
            shield.OnChargeChanged.RemoveListener(OnChargeChanged);
            shield.OnShieldActivated.RemoveListener(OnActivated);
            shield.OnShieldDeactivated.RemoveListener(OnDeactivated);
        }
    }

    void OnChargeChanged(float v)
    {
        if (slider != null) slider.value = v;
        if (readyText != null) readyText.gameObject.SetActive(v >= (shield != null ? shield.maxCharge - 1e-3f : 0f));
    }

    void OnActivated()
    {
        // show shield active visuals on UI
    }

    void OnDeactivated()
    {
        // hide shield active visuals
    }
}