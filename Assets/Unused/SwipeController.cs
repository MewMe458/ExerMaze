using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeController : MonoBehaviour, IEndDragHandler
{
    [SerializeField] int maxButtons;
    int currentButton;
    Vector3 targetPos;
    [SerializeField] Vector3 buttonStep;
    [SerializeField] RectTransform levelButtonsRect;

    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    LTDescr tween;

    float dragThreshold;

    [SerializeField] Button prevBtn, nextBtn;

    private void Awake()
    {
        currentButton = 1;
        targetPos = levelButtonsRect.localPosition;
        dragThreshold = Screen.width / 15;
        updateArrowButton();
    }

    public void Next()
    {
        if (currentButton < maxButtons)
        {
            currentButton++;
            targetPos += buttonStep;
            MoveButton();
        }
    }

    public void Previous()
    {
        if (currentButton > 1)
        {
            currentButton--;
            targetPos -= buttonStep;
            MoveButton();
        }
    }

    void MoveButton()
    {
        //levelButtonsRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        if (tween != null)
            tween.reset();
        tween = levelButtonsRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
        updateArrowButton();
    }

    void updateArrowButton()
    {
        prevBtn.interactable = true;
        nextBtn.interactable = true;
        if (currentButton == 1) prevBtn.interactable = false;
        if (currentButton == maxButtons) nextBtn.interactable = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.position.x - eventData.pressPosition.x) > dragThreshold)
        {
            if (eventData.position.x > eventData.pressPosition.x) Previous();
            else Next();
        }
        else
        {
            MoveButton();
        }
    }
}
