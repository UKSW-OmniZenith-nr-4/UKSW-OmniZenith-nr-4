using UnityEngine;

// N/E/S/W labels standing on the ground (KA-11).
// Convention: North = +Z, East = +X, azimuth clockwise from north.
public class CardinalLabels : MonoBehaviour
{
    public float groundY = -1.6f;
    public float distance = 12f;
    public float height = 0.8f;
    public float characterSize = 0.1f;
    public int fontSize = 64;
    public Color northColor = new Color(1f, 0.35f, 0.3f);
    public Color otherColor = Color.white;

    void Start()
    {
        Make("N", 0f, northColor);
        Make("E", 90f, otherColor);
        Make("S", 180f, otherColor);
        Make("W", 270f, otherColor);
    }

    void Make(string text, float azDeg, Color color)
    {
        float az = azDeg * Mathf.Deg2Rad;
        Vector3 flat = new Vector3(Mathf.Sin(az), 0f, Mathf.Cos(az));

        var go = new GameObject("Label_" + text);
        go.transform.SetParent(transform, false);
        go.transform.position = flat * distance + Vector3.up * (groundY + height);
        go.transform.rotation = Quaternion.LookRotation(flat); // upright, readable from the origin

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = fontSize;
        tm.characterSize = characterSize;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;

        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = f;
        go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
    }
}