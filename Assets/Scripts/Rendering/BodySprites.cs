using System;
using UnityEngine;

public class BodySprites : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Sun, Moon, Mercury, Venus, Mars, Jupiter, Saturn (same names as in SolarSystemObjects)")]
        public string name = "Saturn";

        [Tooltip("PNG with transparent background. If empty, loaded from Assets/Resources/Bodies/<name>")]
        public Texture2D texture;

        [Tooltip("Share of the image width taken by the body itself (Sun 0.25, Saturn 0.42, others 0.9)")]
        [Range(0.05f, 1f)] public float discFraction = 0.4f;

        [Tooltip("Planets: enlarge by Planet Enlarge. Sun and Moon: real size")]
        public bool enlarge = true;

        [Tooltip("Extra size multiplier for this body (1 = default)")]
        public float sizeMultiplier = 1f;

        [Tooltip("FOV below which the point starts turning into the picture")]
        public float showBelowFov = 35f;

        [Tooltip("FOV below which the picture is fully sharp")]
        public float fullBelowFov = 20f;

        [Tooltip("Enable for the Moon (phase and light direction towards the Sun)")]
        public bool usePhase;

        [HideInInspector] public Vector3 dir;
        [HideInInspector] public float alt;
        [HideInInspector] public float diameterDeg;
        [HideInInspector] public float pointDeg;
        [HideInInspector] public Color tint = Color.white;
        [HideInInspector] public bool placed;
        [HideInInspector] public Transform tr;
        [HideInInspector] public MeshRenderer mr;
        [HideInInspector] public Material mat;
    }

    static BodySprites instance;

    [Header("General")]
    public float radius = 99f;

    [Tooltip("Common size multiplier for planets (keeps their proportions)")]
    public float planetEnlarge = 60f;

    [Tooltip("The picture is never smaller than the point of this body (1.15 = 15% larger)")]
    public float minOverPoint = 1.15f;

    [Header("Bodies (defaults are preset)")]
    public Entry[] entries = DefaultEntries();

    Camera cam;

    static Entry Make(string name, float discFraction, bool enlarge, float showBelow, float fullBelow, bool usePhase = false, float sizeMul = 1f)
    {
        return new Entry
        {
            name = name,
            discFraction = discFraction,
            enlarge = enlarge,
            showBelowFov = showBelow,
            fullBelowFov = fullBelow,
            usePhase = usePhase,
            sizeMultiplier = sizeMul
        };
    }

    static Entry[] DefaultEntries()
    {
        return new[]
        {
            Make("Sun",     0.25f, false, 999f, 998f),
            Make("Moon",    0.90f, false, 999f, 998f, true),
            Make("Mercury", 0.90f, true,  999f, 998f),
            Make("Venus",   0.90f, true,  999f, 998f),
            Make("Mars",    0.90f, true,  999f, 998f),
            Make("Jupiter", 0.90f, true,  999f, 998f),
            Make("Saturn",  0.42f, true,  999f, 998f),
        };
    }

    [ContextMenu("Restore defaults")]
    void Reset()
    {
        radius = 99f;
        planetEnlarge = 60f;
        minOverPoint = 1.15f;
        var old = entries;
        entries = DefaultEntries();
        if (old != null)
            foreach (var n in entries)
                foreach (var o in old)
                    if (o != null && o.name == n.name) n.texture = o.texture;
    }

    public static void Place(string bodyName, float altDeg, float azDeg, float diameterDeg,
                             float mag, float bv, float sizeDeg)
    {
        if (instance == null) return;
        var sf = StarField.Instance;

        foreach (var e in instance.entries)
        {
            if (!string.Equals(e.name, bodyName, StringComparison.OrdinalIgnoreCase)) continue;

            e.dir = StarField.AltAzToDirection(altDeg, azDeg);
            e.alt = altDeg;
            e.diameterDeg = diameterDeg;
            e.pointDeg = sizeDeg > 0f ? sizeDeg : (sf != null ? sf.CoreSizeDeg(mag) : 0.1f);
            e.tint = sf != null ? sf.PointColor(mag, bv) : Color.white;
            e.placed = true;
        }
    }

    void Awake()
    {
        instance = this;

        if (entries == null || entries.Length == 0)
            entries = DefaultEntries();

        var mesh = new Mesh { name = "BodyQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

        var shader = Shader.Find("OmniZenith/BodySprite");

        foreach (var e in entries)
        {
            if (e.texture == null)
                e.texture = Resources.Load<Texture2D>("Bodies/" + e.name);
            if (e.texture == null)
                Debug.LogWarning("BodySprites: missing image for " + e.name + " (assign it in the Inspector or put it in Resources/Bodies/" + e.name + ")");

            var go = new GameObject("Body_" + e.name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            e.mr = go.AddComponent<MeshRenderer>();
            e.mat = new Material(shader);
            e.mat.SetTexture("_MainTex", e.texture);
            e.mr.sharedMaterial = e.mat;
            e.tr = go.transform;
            e.mr.enabled = false;
        }
    }

    Entry Find(string n)
    {
        foreach (var e in entries)
            if (string.Equals(e.name, n, StringComparison.OrdinalIgnoreCase)) return e;
        return null;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        var sf = StarField.Instance;
        float halo = sf != null ? sf.haloScale : 4f;
        float fov = cam.fieldOfView;
        var sun = Find("Sun");

        foreach (var e in entries)
        {
            if (!e.placed || e.alt < 0f) { e.mr.enabled = false; continue; }
            e.mr.enabled = true;

            // 0 = point like a star, 1 = fully sharp picture
            float detail = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(e.showBelowFov, e.fullBelowFov, fov));

            Vector3 d = e.dir.normalized;
            Vector3 up = Vector3.up;

            if (e.usePhase && sun != null && sun.placed)
            {
                Vector3 sd = sun.dir.normalized;
                float elong = Vector3.Angle(sd, d);
                e.mat.SetFloat("_UsePhase", 1f);
                e.mat.SetFloat("_Phase", elong / 360f);
                e.mat.SetFloat("_DiscR", e.discFraction);

                // lit side of the Moon faces the Sun
                Vector3 right = sd - Vector3.Dot(sd, d) * d;
                if (right.sqrMagnitude > 1e-6f) up = Vector3.Cross(d, right.normalized);
            }

            e.tr.position = d * radius;
            e.tr.rotation = Quaternion.LookRotation(d, up);

            // size of the disc (picture) and of the point (like a star), in degrees
            float disc = (e.enlarge ? e.diameterDeg * planetEnlarge : e.diameterDeg) * e.sizeMultiplier;
            disc = Mathf.Max(disc, e.pointDeg * minOverPoint);
            float discQuad = disc / e.discFraction;
            float pointQuad = e.pointDeg * halo;
            float quadDeg = Mathf.Max(discQuad, pointQuad);

            float size = 2f * radius * Mathf.Tan(quadDeg * Mathf.Deg2Rad * 0.5f);
            e.tr.localScale = new Vector3(size, size, 1f);

            e.mat.SetFloat("_Detail", detail);
            e.mat.SetColor("_Tint", e.tint);
            e.mat.SetFloat("_PointRel", pointQuad / quadDeg);
            e.mat.SetFloat("_TexScale", quadDeg / discQuad);
        }
    }
}
