using UnityEngine;

public class MinimapRotation : MonoBehaviour
{
    public RectTransform northIndicatorHolder;

    void Update()
    {
        northIndicatorHolder.rotation = Quaternion.Euler(0, 0, transform.eulerAngles.y);
    }
}
