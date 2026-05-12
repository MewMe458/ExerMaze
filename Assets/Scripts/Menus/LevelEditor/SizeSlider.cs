using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SizeSlider : MonoBehaviour
{
    [SerializeField] private Slider Slider;
    [SerializeField] private TMP_Text SliderValueText;

    void Start()
    {
        if (Slider != null && SliderValueText != null)
        {
            Slider.onValueChanged.AddListener(v => SliderValueText.text = v.ToString("0"));
        }
    }
}