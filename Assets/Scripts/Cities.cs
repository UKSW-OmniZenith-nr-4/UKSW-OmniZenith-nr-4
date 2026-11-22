using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public static class City
{
    public static string Nazwa { get; private set; } = "";
    public static float Lat { get; private set; }
    public static float Lon { get; private set; }
    public static bool Ustawiona { get; private set; }

    public static event Action Zmieniona;

    public static void Ustaw(string nazwa, float lat, float lon)
    {
        Nazwa = nazwa;
        Lat = lat;
        Lon = lon;
        Ustawiona = true;
        if (Zmieniona != null) Zmieniona.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Zeruj()
    {
        Zmieniona = null;
        Ustawiona = false;
    }
}

public class Cities : MonoBehaviour
{
    [Header("Wygl¹d")]
    public int rozmiarCzcionki = 22;
    public Color kolorAkcentu = new Color(1f, 0.82f, 0.35f);
    public Color kolorPanelu = new Color(0.05f, 0.07f, 0.12f, 0.97f);

    [Header("Lokalizacja startowa (gdy nie ma zapisanego wyboru)")]
    public string domyslneMiasto = "Warsaw";

    const string KluczPrefs = "lokalizacja_geonameid";

    static readonly CultureInfo K = PobierzKulture();
    static readonly Color kolorWiersza = new Color(0.17f, 0.21f, 0.30f, 1f);
    static readonly Color kolorWybranego = new Color(0.50f, 0.40f, 0.12f, 1f);

    static readonly string[] Kontynenty =
    {
        "Europa", "Azja", "Afryka", "Ameryka Pó³nocna", "Ameryka Po³udniowa", "Australia i Oceania"
    };

    struct Kandydat
    {
        public string nazwy;
        public float lat, lon;
        public int kontynent;

        public Kandydat(string nazwy, float lat, float lon, int kontynent)
        {
            this.nazwy = nazwy;
            this.lat = lat;
            this.lon = lon;
            this.kontynent = kontynent;
        }
    }

    static readonly Kandydat[] Kandydaci =
    {
        // Europa
        new Kandydat("Reykjavik", 64.1f, -21.9f, 0),
        new Kandydat("London", 51.5f, -0.1f, 0),
        new Kandydat("Warsaw", 52.2f, 21.0f, 0),
        new Kandydat("Athens", 38.0f, 23.7f, 0),
        // Azja
        new Kandydat("Tokyo", 35.7f, 139.7f, 1),
        new Kandydat("Beijing|Peking", 39.9f, 116.4f, 1),
        new Kandydat("Mumbai|Bombay", 19.1f, 72.9f, 1),
        new Kandydat("Dubai", 25.3f, 55.3f, 1),
        new Kandydat("Singapore", 1.3f, 103.8f, 1),
        new Kandydat("Jakarta", -6.2f, 106.8f, 1),
        // Afryka
        new Kandydat("Cairo", 30.0f, 31.2f, 2),
        new Kandydat("Lagos", 6.5f, 3.4f, 2),
        new Kandydat("Nairobi", -1.3f, 36.8f, 2),
        new Kandydat("Johannesburg", -26.2f, 28.0f, 2),
        new Kandydat("Cape Town", -33.9f, 18.4f, 2),
        // Ameryka Pó³nocna
        new Kandydat("Anchorage", 61.2f, -149.9f, 3),
        new Kandydat("New York|New York City", 40.7f, -74.0f, 3),
        new Kandydat("Los Angeles", 34.1f, -118.2f, 3),
        new Kandydat("Mexico City|Ciudad de Mexico", 19.4f, -99.1f, 3),
        // Ameryka Po³udniowa
        new Kandydat("Quito", -0.2f, -78.5f, 4),
        new Kandydat("Bogota", 4.7f, -74.1f, 4),
        new Kandydat("Lima", -12.0f, -77.0f, 4),
        new Kandydat("Sao Paulo", -23.5f, -46.6f, 4),
        new Kandydat("Buenos Aires", -34.6f, -58.4f, 4),
        new Kandydat("Ushuaia", -54.8f, -68.3f, 4),
        // Australia i Oceania
        new Kandydat("Perth", -31.9f, 115.9f, 5),
        new Kandydat("Sydney", -33.9f, 151.2f, 5),
        new Kandydat("Auckland", -36.9f, 174.8f, 5),
    };

    class Miejsce
    {
        public string nazwa, kraj;
        public float lat, lon;
        public int geonameId, kontynent;
    }

    List<Miejsce> miejsca = new List<Miejsce>();
    Miejsce biezaca;
    Button[] przyciski;
    Text tekstPrzycisku;
    GameObject okno;
    Font czcionka;



    void Start()
    {
        czcionka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var dbManager = FindFirstObjectByType<DbManager>();
        if (dbManager == null || dbManager.Db == null)
        {
            Debug.LogWarning("Cities: nie znaleziono DbManager w scenie");
            return;
        }

        WczytajMiasta(dbManager);
        if (miejsca.Count == 0)
        {
            Debug.LogWarning("Cities: ¿adne z miast nie zosta³o znalezione w bazie");
            return;
        }

        ZapewnijEventSystem();
        ZbudujOkno();
        ZbudujPrzycisk();
        UstawDomyslna();
        okno.SetActive(false);
    }

    void WczytajMiasta(DbManager dbm)
    {
        var zWspolrzednymi = dbm.Db.Table<Miasto>().ToList().Where(c => c.MaWspolrzedne).ToList();

        var poNazwie = new Dictionary<string, List<Miasto>>();
        foreach (var c in zWspolrzednymi)
        {
            string klucz = Normalizuj(c.Nazwa);
            if (!poNazwie.TryGetValue(klucz, out List<Miasto> lista))
            {
                lista = new List<Miasto>();
                poNazwie[klucz] = lista;
            }
            lista.Add(c);
        }

        foreach (var kand in Kandydaci)
        {
            Miasto najlepsze = null;
            float najlepszaOdleglosc = float.MaxValue;

            foreach (string alt in kand.nazwy.Split('|'))
            {
                if (!poNazwie.TryGetValue(Normalizuj(alt), out List<Miasto> lista)) continue;

                foreach (var c in lista)
                {
                    float dLat = Mathf.Abs(c.Lat - kand.lat);
                    float dLon = Mathf.Abs(c.Lon - kand.lon);
                    if (dLat > 3f || dLon > 3f) continue;

                    if (dLat + dLon < najlepszaOdleglosc)
                    {
                        najlepsze = c;
                        najlepszaOdleglosc = dLat + dLon;
                    }
                }
            }

            if (najlepsze == null)
            {
                Debug.LogWarning("Cities: nie znaleziono w bazie miasta: " + kand.nazwy);
                continue;
            }

            miejsca.Add(new Miejsce
            {
                nazwa = najlepsze.Nazwa,
                kraj = najlepsze.Kraj,
                lat = najlepsze.Lat,
                lon = najlepsze.Lon,
                geonameId = najlepsze.GeonameId,
                kontynent = kand.kontynent
            });
        }

        miejsca = miejsca.OrderBy(x => x.kontynent).ThenByDescending(x => x.lat).ToList();

        int liczbaKontynentow = miejsca.Select(x => x.kontynent).Distinct().Count();
        Debug.Log("Cities: miasta na liœcie: " + miejsca.Count +
                  " (kontynentów: " + liczbaKontynentow + ")");
        if (miejsca.Count < 20 || liczbaKontynentow < 5)
            Debug.LogWarning("Cities: lista nie spe³nia wymagañ (min. 20 miast, min. 5 kontynentów). " +
                             "SprawdŸ nazwy w cities.csv i dopisz alternatywne po znaku '|'.");
    }

    static string Normalizuj(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
    }


    void UstawDomyslna()
    {
        Miejsce start = null;

        int zapisane = PlayerPrefs.GetInt(KluczPrefs, 0);
        if (zapisane != 0) start = miejsca.Find(x => x.geonameId == zapisane);

        if (start == null)
        {
            string domyslne = Normalizuj(domyslneMiasto);
            start = miejsca.Find(x => Normalizuj(x.nazwa) == domyslne);
        }
        if (start == null) start = miejsca[0];

        UstawLokalizacje(start);
    }

    void UstawLokalizacje(Miejsce m)
    {
        biezaca = m;
        City.Ustaw(m.nazwa, m.lat, m.lon);

        PlayerPrefs.SetInt(KluczPrefs, m.geonameId);
        PlayerPrefs.Save();

        tekstPrzycisku.text = "Miasto: " + m.nazwa;
        for (int i = 0; i < przyciski.Length; i++)
            przyciski[i].GetComponent<Image>().color = (miejsca[i] == biezaca) ? kolorWybranego : kolorWiersza;

        Debug.Log("Wybrano lokalizacjê: " + m.nazwa + ", " + m.kraj + " (" + FormatWsp(m.lat, m.lon) + ")");
    }

    void Wybierz(Miejsce m)
    {
        UstawLokalizacje(m);
        okno.SetActive(false);
    }

    public void Przelacz()
    {
        if (okno != null) okno.SetActive(!okno.activeSelf);
    }

    void Zamknij()
    {
        if (okno != null) okno.SetActive(false);
    }

    static string FormatWsp(float lat, float lon)
    {
        return Mathf.Abs(lat).ToString("0.0", K) + "° " + (lat >= 0f ? "N" : "S") + "  " +
               Mathf.Abs(lon).ToString("0.0", K) + "° " + (lon >= 0f ? "E" : "W");
    }



    void ZapewnijEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    Canvas NowyCanvas(string nazwa, int kolejnosc)
    {
        var o = new GameObject(nazwa);
        var canvas = o.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = kolejnosc;
        var scaler = o.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        o.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    void ZbudujOkno()
    {
        var canvas = NowyCanvas("CityCanvas", 21);

        var pojemnik = new GameObject("Okno", typeof(RectTransform));
        pojemnik.transform.SetParent(canvas.transform, false);
        Rozciagnij((RectTransform)pojemnik.transform, 0, 0, 0, 0);
        okno = pojemnik;

        var tlo = NowyObraz("Tlo", pojemnik.transform, new Color(0f, 0f, 0f, 0.45f));
        Rozciagnij(tlo.rectTransform, 0, 0, 0, 0);
        var przyciskTla = tlo.gameObject.AddComponent<Button>();
        przyciskTla.transition = Selectable.Transition.None;
        przyciskTla.onClick.AddListener(Zamknij);

        const int kolumny = 3;
        int wiersze = Mathf.CeilToInt(miejsca.Count / (float)kolumny);
        float wysokosc = 74f + wiersze * 58f + 14f;

        var panel = NowyObraz("Panel", pojemnik.transform, kolorPanelu);
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = new Vector2(0f, 25f);
        prt.sizeDelta = new Vector2(1100f, wysokosc);

        var tytul = NowyTekst("Tytul", panel.transform, "Wybierz lokalizacjê", 34, TextAnchor.MiddleLeft, kolorAkcentu);
        tytul.fontStyle = FontStyle.Bold;
        Ustaw(tytul.rectTransform, 24, 14, 520, 44);

        int liczbaKontynentow = miejsca.Select(x => x.kontynent).Distinct().Count();
        var info = NowyTekst("Info", panel.transform,
            miejsca.Count + " miast, " + liczbaKontynentow + " kontynentów",
            20, TextAnchor.MiddleRight, new Color(0.7f, 0.77f, 0.9f));
        Ustaw(info.rectTransform, 560, 14, 370, 44);

        var zamknij = NowyPrzycisk("Zamknij", panel.transform, "Zamknij", Zamknij);
        Ustaw((RectTransform)zamknij.transform, 950, 14, 126, 44);

        przyciski = new Button[miejsca.Count];
        for (int i = 0; i < miejsca.Count; i++)
        {
            var m = miejsca[i];
            int kol = i / wiersze;
            int wie = i % wiersze;

            var b = NowyPrzycisk("Miasto" + i, panel.transform, null, () => Wybierz(m));
            Ustaw((RectTransform)b.transform, 24 + kol * 356, 74 + wie * 58, 336, 56);

            string nazwa = m.nazwa + (string.IsNullOrEmpty(m.kraj) ? "" : ", " + m.kraj);
            var t1 = NowyTekst("Nazwa", b.transform, nazwa, 22, TextAnchor.MiddleLeft, Color.white);
            t1.resizeTextForBestFit = true;
            t1.resizeTextMinSize = 13;
            t1.resizeTextMaxSize = 22;
            Ustaw(t1.rectTransform, 12, 3, 316, 28);

            var t2 = NowyTekst("Wspolrzedne", b.transform,
                Kontynenty[m.kontynent] + "  ·  " + FormatWsp(m.lat, m.lon),
                16, TextAnchor.MiddleLeft, new Color(0.75f, 0.82f, 0.95f));
            Ustaw(t2.rectTransform, 12, 31, 316, 22);

            przyciski[i] = b;
        }
    }

    void ZbudujPrzycisk()
    {
        Transform rodzic = Clock.PasekTransform;
        bool naPasku = rodzic != null;

        if (naPasku)
        {
            var canvasPaska = rodzic.GetComponentInParent<Canvas>();
            if (canvasPaska != null && canvasPaska.GetComponent<GraphicRaycaster>() == null)
                canvasPaska.gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            rodzic = NowyCanvas("CityPrzyciskCanvas", 10).transform;
        }

        var b = NowyPrzycisk("PrzyciskLokalizacji", rodzic, "City", Przelacz);
        var rt = (RectTransform)b.transform;
        Vector2 kotwica = naPasku ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
        rt.anchorMin = rt.anchorMax = rt.pivot = kotwica;
        rt.anchoredPosition = new Vector2(305f, naPasku ? 0f : 5f);
        rt.sizeDelta = new Vector2(340f, 40f);

        tekstPrzycisku = b.GetComponentInChildren<Text>();
        tekstPrzycisku.resizeTextForBestFit = true;
        tekstPrzycisku.resizeTextMinSize = 12;
        tekstPrzycisku.resizeTextMaxSize = 22;
    }


    Image NowyObraz(string nazwa, Transform rodzic, Color kolor)
    {
        var o = new GameObject(nazwa, typeof(RectTransform), typeof(Image));
        o.transform.SetParent(rodzic, false);
        var img = o.GetComponent<Image>();
        img.color = kolor;
        return img;
    }

    Text NowyTekst(string nazwa, Transform rodzic, string tresc, int rozmiar, TextAnchor wyrownanie, Color kolor)
    {
        var o = new GameObject(nazwa, typeof(RectTransform), typeof(Text));
        o.transform.SetParent(rodzic, false);
        var t = o.GetComponent<Text>();
        t.font = czcionka;
        t.text = tresc;
        t.fontSize = rozmiar;
        t.alignment = wyrownanie;
        t.color = kolor;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    Button NowyPrzycisk(string nazwa, Transform rodzic, string napis, UnityEngine.Events.UnityAction akcja)
    {
        var img = NowyObraz(nazwa, rodzic, kolorWiersza);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;

        var kolory = b.colors;
        kolory.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        kolory.highlightedColor = Color.white;
        kolory.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        kolory.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        b.colors = kolory;

        b.onClick.AddListener(akcja);

        if (napis != null)
        {
            var t = NowyTekst("Napis", img.transform, napis, rozmiarCzcionki, TextAnchor.MiddleCenter, Color.white);
            Rozciagnij(t.rectTransform, 8, 0, 8, 0);
        }
        return b;
    }

    static void Ustaw(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void Rozciagnij(RectTransform rt, float lewo, float dol, float prawo, float gora)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(lewo, dol);
        rt.offsetMax = new Vector2(-prawo, -gora);
    }

    static CultureInfo PobierzKulture()
    {
        try { return new CultureInfo("pl-PL"); }
        catch (Exception) { return CultureInfo.InvariantCulture; }
    }
}