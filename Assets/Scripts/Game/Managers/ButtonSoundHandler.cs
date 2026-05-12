using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSoundHandler : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private bool playOnClick = true; // Option to enable/disable click sound
    [SerializeField] private bool playOnHover = true; // Option to enable/disable hover sound

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (playOnClick)
        {
            SoundManager.Instance?.PlayClickSound();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playOnHover)
        {
            // Called when mouse pointer enters the button
            SoundManager.Instance?.PlayHoverSound();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Called when the button is selected (e.g., via keyboard navigation)
        //AudioManager.Instance?.PlayHoverSound(); // Temporarily commented out to avoid double sound on selection
    }
}