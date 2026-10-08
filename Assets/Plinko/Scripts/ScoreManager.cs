using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int score = 0;

    [Header("Simple IMGUI display")]
    public Rect scoreRect = new Rect(24f, 20f, 360f, 64f);
    public int fontSize = 34;
    public Color textColor = new Color(0.21f, 0.91f, 1f, 1f);

    public int Score => score;

    public void AddScore(int amount)
    {
        score += amount;
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };

        style.normal.textColor = textColor;
        GUI.Label(scoreRect, $"Score: {score}", style);
    }
}
