using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Katalog gwiazd: ikona na pasku otwiera listê, klikniêcie gwiazdy pokazuje jej dane
public class KatalogGwiazd : MonoBehaviour
{
    [Header("Katalog")]
    public float maksJasnosc = 6.5f;   // gwiazdy do tej jasnoœci trafiaj¹ do katalogu
    public int wierszeNaStronie = 10;

    [Header("Wygl¹d")]
    public int rozmiarCzcionki = 22;
    public Color kolorPanelu = new Color(0.05f, 0.07f, 0.12f, 0.96f);
    public Color kolorAkcentu = new Color(1f, 0.82f, 0.35f);

    static readonly CultureInfo K = PobierzKulture();
    static Sprite spriteGwiazdy;

    static readonly Color kolorWiersza = new Color(0.17f, 0.21f, 0.30f, 1f);
    static readonly Color kolorWybranego = new Color(0.50f, 0.40f, 0.12f, 1f);

    List<Gwiazda> wszystkie = new List<Gwiazda>();
    List<Gwiazda> widoczne = new List<Gwiazda>();
    Dictionary<string, string> nazwyKonstelacji = new Dictionary<string, string>();
    Gwiazda wybrana;
    int strona;

    Font czcionka;
    GameObject panel;
    InputField poleSzukaj;
    Text tekstLicznik, tekstStrona, tekstNazwa, tekstSzczegoly;
    Image probkaKoloru;
    Button przyciskPoprzednia, przyciskNastepna;
    Button[] wiersze;
    Text[] nazwyWierszy;
    Text[] infoWierszy;

    // ---------- Start ----------

    void Start()
    {
        czcionka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var dbManager = FindFirstObjectByType<DbManager>();
        if (dbManager == null || dbManager.Db == null)
        {
            Debug.LogWarning("KatalogGwiazd: nie znaleziono DbManager w scenie");
            return;
        }

        WczytajDane(dbManager);
        ZapewnijEventSystem();
        ZbudujPanel();
        ZbudujPrzyciskIkony();

        Filtruj("");
        if (widoczne.Count > 0) Wybierz(widoczne[0]);
        panel.SetActive(false);
    }

    void WczytajDane(DbManager dbm)
    {
        float limit = maksJasnosc;
        wszystkie = dbm.Db.Table<Gwiazda>()
            .Where(g => g.Mag <= limit)
            .OrderBy(g => g.Mag)
            .ToList();
        wszystkie.RemoveAll(g => g.Nazwa == "Sol");   // S³oñce nie nale¿y do katalogu

        foreach (var k in dbm.Db.Table<Konstelacja>().ToList())
            if (!string.IsNullOrEmpty(k.Skrot)) nazwyKonstelacji[k.Skrot] = k.Nazwa;

        Debug.Log("KatalogGwiazd: gwiazd w katalogu: " + wszystkie.Count);
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

    // ---------- Otwieranie i zamykanie ----------

    public void Przelacz()
    {
        if (panel == null) return;
        bool pokaz = !panel.activeSelf;
        panel.SetActive(pokaz);
        if (pokaz) OdswiezListe();
    }

    void Zamknij()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ---------- Lista, wyszukiwanie, strony ----------

    void Filtruj(string tekst)
    {
        string q = (tekst ?? "").Trim().ToLowerInvariant();
        widoczne = q.Length == 0
            ? new List<Gwiazda>(wszystkie)
            : wszystkie.Where(g => Pasuje(g, q)).ToList();
        strona = 0;
        OdswiezListe();
    }

    bool Pasuje(Gwiazda g, string q)
    {
        if (!string.IsNullOrEmpty(g.Nazwa) && g.Nazwa.ToLowerInvariant().Contains(q)) return true;
        if (!string.IsNullOrEmpty(g.Bf) && g.Bf.ToLowerInvariant().Contains(q)) return true;
        if (!string.IsNullOrEmpty(g.Con))
        {
            if (g.Con.ToLowerInvariant().Contains(q)) return true;
            if (nazwyKonstelacji.TryGetValue(g.Con, out string nk) &&
                !string.IsNullOrEmpty(nk) && nk.ToLowerInvariant().Contains(q)) return true;
        }
        return g.Hip > 0 && g.Hip.ToString().Contains(q);
    }

    void ZmienStrone(int zmiana)
    {
        strona += zmiana;
        OdswiezListe();
    }

    void KlikWiersz(int i)
    {
        int idx = strona * wierszeNaStronie + i;
        if (idx < widoczne.Count) Wybierz(widoczne[idx]);
    }

    void Wybierz(Gwiazda g)
    {
        wybrana = g;
        PokazSzczegoly(g);
        OdswiezListe();
    }

    void OdswiezListe()
    {
        int n = wierszeNaStronie;
        int liczbaStron = Mathf.Max(1, Mathf.CeilToInt(widoczne.Count / (float)n));
        strona = Mathf.Clamp(strona, 0, liczbaStron - 1);

        for (int i = 0; i < n; i++)
        {
            int idx = strona * n + i;
            bool jest = idx < widoczne.Count;
            wiersze[i].gameObject.SetActive(jest);
            if (!jest) continue;

            var g = widoczne[idx];
            nazwyWierszy[i].text = NazwaGwiazdy(g);
            infoWierszy[i].text = (string.IsNullOrEmpty(g.Con) ? "" : g.Con + "  ·  ") +
                                  g.Mag.ToString("0.00", K);
            wiersze[i].GetComponent<Image>().color = (g == wybrana) ? kolorWybranego : kolorWiersza;
        }

        tekstStrona.text = "Strona " + (strona + 1) + " / " + liczbaStron;
        tekstLicznik.text = widoczne.Count + " z " + wszystkie.Count + " gwiazd";
        przyciskPoprzednia.interactable = strona > 0;
        przyciskNastepna.interactable = strona < liczbaStron - 1;
    }

    static string NazwaGwiazdy(Gwiazda g)
    {
        if (!string.IsNullOrEmpty(g.Nazwa)) return g.Nazwa;
        if (!string.IsNullOrWhiteSpace(g.Bf)) return g.Bf.Trim();
        if (g.Hip > 0) return "HIP " + g.Hip;
        return "Gwiazda #" + g.Id;
    }

    // ---------- Dane wybranej gwiazdy ----------

    void PokazSzczegoly(Gwiazda g)
    {
        tekstNazwa.text = NazwaGwiazdy(g);

        string gwiazdozbior = null;
        if (!string.IsNullOrEmpty(g.Con))
        {
            gwiazdozbior = g.Con;
            if (nazwyKonstelacji.TryGetValue(g.Con, out string nk) && !string.IsNullOrEmpty(nk))
                gwiazdozbior += " (" + nk + ")";
        }

        string odleglosc = null;
        if (g.Dist.HasValue && g.Dist.Value > 0f)
        {
            float ly = g.Dist.Value * 3.26156f;
            odleglosc = (ly < 100f ? ly.ToString("0.0", K) : ly.ToString("0", K)) +
                        " lat œwietlnych (" + g.Dist.Value.ToString("0.0", K) + " pc)";
        }

        string bv = null, temperatura = null, kolor = null;
        Color kolorGwiazdy = new Color(0.85f, 0.88f, 0.95f);
        if (g.Ci.HasValue)
        {
            float t = TemperaturaZBV(g.Ci.Value);
            bv = g.Ci.Value.ToString("0.00", K);
            temperatura = "ok. " + Mathf.RoundToInt(t / 10f) * 10 + " K";
            kolor = OpisKoloru(g.Ci.Value);
            kolorGwiazdy = KolorZTemperatury(t);
        }
        probkaKoloru.color = kolorGwiazdy;

        var sb = new StringBuilder();
        Linia(sb, "Oznaczenie", string.IsNullOrWhiteSpace(g.Bf) ? null : g.Bf.Trim());
        Linia(sb, "Numer HIP", g.Hip > 0 ? g.Hip.ToString() : null);
        Linia(sb, "Gwiazdozbiór", gwiazdozbior);
        Linia(sb, "Jasnoœæ widoma", g.Mag.ToString("0.00", K) + " mag");
        Linia(sb, "Jasnoœæ absolutna", g.AbsMag.HasValue ? g.AbsMag.Value.ToString("0.00", K) + " mag" : null);
        Linia(sb, "Odleg³oœæ", odleglosc);
        Linia(sb, "Typ widmowy", string.IsNullOrWhiteSpace(g.Spect) ? null : g.Spect.Trim());
        Linia(sb, "WskaŸnik barwy B-V", bv);
        Linia(sb, "Temperatura", temperatura);
        Linia(sb, "Barwa", kolor);
        Linia(sb, "Rektascensja", FormatRA(g.Ra));
        Linia(sb, "Deklinacja", FormatDec(g.Dec));
        tekstSzczegoly.text = sb.ToString();
    }

    static void Linia(StringBuilder sb, string etykieta, string wartosc)
    {
        sb.Append("<color=#8fa3c0>").Append(etykieta).Append(":</color>  ")
          .Append(string.IsNullOrEmpty(wartosc) ? "brak danych" : wartosc).Append('\n');
    }

    static string FormatRA(float godziny)
    {
        godziny = ((godziny % 24f) + 24f) % 24f;
        int h = (int)godziny;
        float m = (godziny - h) * 60f;
        int mm = (int)m;
        float s = (m - mm) * 60f;
        return string.Format(K, "{0}h {1:00}m {2:00.0}s", h, mm, s);
    }

    static string FormatDec(float stopnie)
    {
        char znak = stopnie < 0f ? '-' : '+';
        stopnie = Mathf.Abs(stopnie);
        int d = (int)stopnie;
        float m = (stopnie - d) * 60f;
        int mm = (int)m;
        int s = (int)((m - mm) * 60f);
        return string.Format(K, "{0}{1}° {2:00}' {3:00}\"", znak, d, mm, s);
    }

    // ---------- Barwa gwiazdy z B-V ----------

    // Przybli¿enie Ballesterosa: temperatura (K) z B-V
    static float TemperaturaZBV(float bv)
    {
        bv = Mathf.Clamp(bv, -0.4f, 2.0f);
        return 4600f * (1f / (0.92f * bv + 1.7f) + 1f / (0.92f * bv + 0.62f));
    }

    static string OpisKoloru(float bv)
    {
        if (bv < -0.1f) return "niebieska";
        if (bv < 0.15f) return "bia³o-niebieska";
        if (bv < 0.45f) return "¿ó³tawo-bia³a";
        if (bv < 0.75f) return "¿ó³ta";
        if (bv < 1.2f) return "pomarañczowa";
        return "czerwona";
    }

    // Przybli¿ony kolor cia³a doskonale czarnego dla danej temperatury
    static Color KolorZTemperatury(float kelwiny)
    {
        float t = kelwiny / 100f;
        float r, g, b;

        if (t <= 66f)
        {
            r = 255f;
            g = 99.4708025861f * Mathf.Log(t) - 161.1195681661f;
        }
        else
        {
            r = 329.698727446f * Mathf.Pow(t - 60f, -0.1332047592f);
            g = 288.1221695283f * Mathf.Pow(t - 60f, -0.0755148492f);
        }

        if (t >= 66f) b = 255f;
        else if (t <= 19f) b = 0f;
        else b = 138.5177312231f * Mathf.Log(t - 10f) - 305.0447927307f;

        return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f));
    }

    // ---------- Budowanie interfejsu ----------

    void ZbudujPanel()
    {
        var canvasObj = new GameObject("KatalogCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        int n = wierszeNaStronie;
        float wysokosc = 124f + n * 46f + 80f;

        var tlo = NowyObraz("Panel", canvasObj.transform, kolorPanelu);
        panel = tlo.gameObject;
        var prt = tlo.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = new Vector2(0f, 25f);
        prt.sizeDelta = new Vector2(1100f, wysokosc);

        // Nag³ówek
        var tytul = NowyTekst("Tytul", panel.transform, "Katalog gwiazd", 34, TextAnchor.MiddleLeft, kolorAkcentu);
        tytul.fontStyle = FontStyle.Bold;
        Ustaw(tytul.rectTransform, 24, 14, 400, 44);

        tekstLicznik = NowyTekst("Licznik", panel.transform, "", 20, TextAnchor.MiddleRight, new Color(0.7f, 0.77f, 0.9f));
        Ustaw(tekstLicznik.rectTransform, 460, 14, 470, 44);

        var zamknij = NowyPrzycisk("Zamknij", panel.transform, "Zamknij", Zamknij);
        Ustaw((RectTransform)zamknij.transform, 950, 14, 126, 44);

        // Wyszukiwarka
        poleSzukaj = StworzPoleSzukaj(panel.transform);
        Ustaw((RectTransform)poleSzukaj.transform, 24, 68, 500, 44);

        // Wiersze listy
        wiersze = new Button[n];
        nazwyWierszy = new Text[n];
        infoWierszy = new Text[n];
        for (int i = 0; i < n; i++)
        {
            int indeks = i;
            var b = NowyPrzycisk("Wiersz" + i, panel.transform, null, () => KlikWiersz(indeks));
            Ustaw((RectTransform)b.transform, 24, 124 + i * 46, 500, 44);

            nazwyWierszy[i] = NowyTekst("Nazwa", b.transform, "", rozmiarCzcionki, TextAnchor.MiddleLeft, Color.white);
            Rozciagnij(nazwyWierszy[i].rectTransform, 12, 0, 170, 0);

            infoWierszy[i] = NowyTekst("Info", b.transform, "", rozmiarCzcionki - 2, TextAnchor.MiddleRight, new Color(0.75f, 0.82f, 0.95f));
            Rozciagnij(infoWierszy[i].rectTransform, 300, 0, 12, 0);

            wiersze[i] = b;
        }

        // Prze³¹czanie stron
        float yStron = 124 + n * 46 + 12;
        przyciskPoprzednia = NowyPrzycisk("Poprzednia", panel.transform, "<", () => ZmienStrone(-1));
        Ustaw((RectTransform)przyciskPoprzednia.transform, 24, yStron, 120, 44);

        tekstStrona = NowyTekst("Strona", panel.transform, "", 20, TextAnchor.MiddleCenter, Color.white);
        Ustaw(tekstStrona.rectTransform, 154, yStron, 240, 44);

        przyciskNastepna = NowyPrzycisk("Nastepna", panel.transform, ">", () => ZmienStrone(1));
        Ustaw((RectTransform)przyciskNastepna.transform, 404, yStron, 120, 44);

        // Panel danych gwiazdy
        float wysDanych = wysokosc - 68f - 24f;
        var dane = NowyObraz("Dane", panel.transform, new Color(0.08f, 0.11f, 0.17f, 1f));
        dane.raycastTarget = false;
        Ustaw(dane.rectTransform, 548, 68, 528, wysDanych);

        tekstNazwa = NowyTekst("DaneNazwa", dane.transform, "", 34, TextAnchor.MiddleLeft, kolorAkcentu);
        tekstNazwa.fontStyle = FontStyle.Bold;
        Ustaw(tekstNazwa.rectTransform, 20, 16, 430, 56);

        probkaKoloru = NowyObraz("Probka", dane.transform, Color.white);
        probkaKoloru.sprite = SpriteGwiazdy();
        probkaKoloru.raycastTarget = false;
        Ustaw(probkaKoloru.rectTransform, 460, 20, 48, 48);

        tekstSzczegoly = NowyTekst("DaneTresc", dane.transform, "", 21, TextAnchor.UpperLeft, Color.white);
        tekstSzczegoly.supportRichText = true;
        tekstSzczegoly.lineSpacing = 1.2f;
        Ustaw(tekstSzczegoly.rectTransform, 20, 88, 488, wysDanych - 104f);
    }

    void ZbudujPrzyciskIkony()
    {
        Transform rodzic = Clock.PasekTransform;
        bool naPasku = rodzic != null;

        if (naPasku)
        {
            // Canvas paska potrzebuje GraphicRaycaster, ¿eby przycisk reagowa³ na klikniêcia
            var canvasPaska = rodzic.GetComponentInParent<Canvas>();
            if (canvasPaska != null && canvasPaska.GetComponent<GraphicRaycaster>() == null)
                canvasPaska.gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Zapasowo (brak paska zegara): w³asny Canvas, ikona na dole poœrodku
            var c = new GameObject("KatalogIkonaCanvas");
            var canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            c.AddComponent<GraphicRaycaster>();
            rodzic = c.transform;
        }

        var b = NowyPrzycisk("IkonaKatalog", rodzic, null, Przelacz);
        var rt = (RectTransform)b.transform;
        Vector2 kotwica = naPasku ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
        rt.anchorMin = rt.anchorMax = rt.pivot = kotwica;
        rt.anchoredPosition = naPasku ? Vector2.zero : new Vector2(0f, 5f);
        rt.sizeDelta = new Vector2(230f, 40f);

        var ikona = NowyObraz("Ikona", b.transform, kolorAkcentu);
        ikona.sprite = SpriteGwiazdy();
        ikona.raycastTarget = false;
        var irt = ikona.rectTransform;
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(10f, 0f);
        irt.sizeDelta = new Vector2(28f, 28f);

        var napis = NowyTekst("Napis", b.transform, "Katalog gwiazd", 22, TextAnchor.MiddleLeft, Color.white);
        Rozciagnij(napis.rectTransform, 48, 0, 8, 0);
    }

    InputField StworzPoleSzukaj(Transform rodzic)
    {
        var tlo = NowyObraz("Szukaj", rodzic, new Color(1f, 1f, 1f, 0.12f));

        var tekst = NowyTekst("Tekst", tlo.transform, "", rozmiarCzcionki, TextAnchor.MiddleLeft, Color.white);
        tekst.supportRichText = false;
        Rozciagnij(tekst.rectTransform, 12, 0, 12, 0);

        var podpowiedz = NowyTekst("Podpowiedz", tlo.transform, "Szukaj: nazwa, gwiazdozbiór lub HIP",
            rozmiarCzcionki - 2, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.45f));
        podpowiedz.fontStyle = FontStyle.Italic;
        Rozciagnij(podpowiedz.rectTransform, 12, 0, 12, 0);

        // InputField dodajemy na koñcu, gdy teksty ju¿ istniej¹
        var pole = tlo.gameObject.AddComponent<InputField>();
        pole.targetGraphic = tlo;
        pole.textComponent = tekst;
        pole.placeholder = podpowiedz;
        pole.onValueChanged.AddListener(Filtruj);
        return pole;
    }

    // ---------- Pomocnicze elementy UI ----------

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
            Rozciagnij(t.rectTransform, 0, 0, 0, 0);
        }
        return b;
    }

    // Pozycja od lewego górnego rogu rodzica
    static void Ustaw(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    // Wype³nia rodzica z marginesami
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

    // ---------- Ikona gwiazdy (rysowana w kodzie, bez pliku graficznego) ----------

    static Sprite SpriteGwiazdy()
    {
        if (spriteGwiazdy != null) return spriteGwiazdy;

        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        // Piêcioramienna gwiazda: na przemian wierzcho³ki zewnêtrzne i wewnêtrzne
        var w = new Vector2[10];
        float c = N / 2f, R = N * 0.48f, r = R * 0.40f;
        for (int i = 0; i < 10; i++)
        {
            float kat = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float p = (i % 2 == 0) ? R : r;
            w[i] = new Vector2(c + Mathf.Cos(kat) * p, c + Mathf.Sin(kat) * p);
        }

        var piksele = new Color[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                // Wyg³adzanie krawêdzi: 3x3 próbek na piksel
                int trafienia = 0;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                        if (WWielokacie(new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f), w))
                            trafienia++;
                piksele[y * N + x] = new Color(1f, 1f, 1f, trafienia / 9f);
            }
        }
        tex.SetPixels(piksele);
        tex.Apply();

        spriteGwiazdy = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        return spriteGwiazdy;
    }

    static bool WWielokacie(Vector2 p, Vector2[] w)
    {
        bool wewnatrz = false;
        for (int i = 0, j = w.Length - 1; i < w.Length; j = i++)
        {
            if (((w[i].y > p.y) != (w[j].y > p.y)) &&
                (p.x < (w[j].x - w[i].x) * (p.y - w[i].y) / (w[j].y - w[i].y) + w[i].x))
                wewnatrz = !wewnatrz;
        }
        return wewnatrz;
    }
}