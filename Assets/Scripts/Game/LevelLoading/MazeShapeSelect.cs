using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MazeShapeSelect : MonoBehaviour
{
    [SerializeField] private Button SquareShapeButton;
    [SerializeField] private Button CircleShapeButton;
    [SerializeField] private Button TriangleShapeButton;

    void Awake()
    {
        if (SquareShapeButton != null)
            SquareShapeButton.onClick.AddListener(() => SceneManager.LoadSceneAsync("SquareLevelSelect"));
        else
            Debug.LogWarning("Square Shape button not assigned");

        if (CircleShapeButton != null)
            CircleShapeButton.onClick.AddListener(() => SceneManager.LoadSceneAsync("CircleLevelSelect"));
        else
            Debug.LogWarning("Circle Shape button not assigned");

        if (TriangleShapeButton != null)
           TriangleShapeButton.onClick.AddListener(() => SceneManager.LoadSceneAsync("TriangleLevelSelect"));
        else
            Debug.LogWarning("Triangle Shape button not assigned");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
