using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// Wyœwietla aktualn¹ godzinê i datê z zegara systemowego w pasku na dole ekranu
public class Clock : MonoBehaviour
{
    [Header("Wygl¹d paska")]
    public int wysokoscPaska = 50;
    public int rozmiarCzcionki = 26;
    public Color kolorTla = new Color(0f, 0f, 0f, 0.6f);
    public Color kolorTekstu = Color.white;

    Text tekstGodzina;
    Text tekstData;
    CultureInfo kultura;
    int ostatniaSekunda = -1;

    public static Transform PasekTransform { get; private set; }
    void Awake()
    {
        try { kultura = new CultureInfo("pl-PL"); }
        catch (Exception) { kultura = CultureInfo.InvariantCulture; }

        ZbudujInterfejs();
    }

    void Update()
    {
        if (tekstGodzina == null || tekstData == null)
        {
            ZbudujInterfejs();
            ostatniaSekunda = -1;
        }

        DateTime teraz = DateTime.Now; 

        if (teraz.Second == ostatniaSekunda) return;
        ostatniaSekunda = teraz.Second;

        tekstGodzina.text = teraz.ToString("HH:mm:ss", kultura);
        tekstData.text = teraz.ToString("dddd, d MMMM yyyy", kultura);
    }

    GameObject canvasObj;

    void OnDestroy()
    {
        if (canvasObj != null) Destroy(canvasObj);
    }

    // ---------- Budowanie interfejsu ----------

    void ZbudujInterfejs()
    {
        canvasObj = new GameObject("ClockCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var pasek = new GameObject("Pasek", typeof(RectTransform), typeof(Image));
        pasek.transform.SetParent(canvasObj.transform, false);
        PasekTransform = pasek.transform;
        var rt = pasek.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, wysokoscPaska);

        var tlo = pasek.GetComponent<Image>();
        tlo.color = kolorTla;
        tlo.raycastTarget = false;

        tekstGodzina = StworzTekst("Godzina", pasek.transform, TextAnchor.MiddleLeft);
        tekstData = StworzTekst("Data", pasek.transform, TextAnchor.MiddleRight);
    }

    Text StworzTekst(string nazwa, Transform rodzic, TextAnchor wyrownanie)
    {
        var obj = new GameObject(nazwa, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(rodzic, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);

        var t = obj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = rozmiarCzcionki;
        t.color = kolorTekstu;
        t.alignment = wyrownanie;
        t.raycastTarget = false;
        return t;
    }
}