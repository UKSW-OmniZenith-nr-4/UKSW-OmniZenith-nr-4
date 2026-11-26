using UnityEngine;

// Button that hides/shows the trees (horizon ring) so objects below the horizon can be checked (KA-12).
public class TreesToggle : MonoBehaviour
{
    public GameObject trees;                          // empty = find object "Trees"
    public Rect buttonRect = new Rect(10, 60, 150, 36);
    public int fontSize = 18;

    void Start()
    {
        if (trees == null) trees = GameObject.Find("Trees");
    }

    void Toggle()
    {
        if (trees != null) trees.SetActive(!trees.activeSelf);
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        string state = (trees != null && trees.activeSelf) ? "ON" : "OFF";
        if (GUI.Button(buttonRect, "Drzewa: " + state, style)) Toggle();
    }
}