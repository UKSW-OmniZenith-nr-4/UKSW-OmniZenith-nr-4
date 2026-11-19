
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using SQLite;
using UnityEngine;

public class DbManager : MonoBehaviour
{
    // Gwiazdy ciemniejsze ni¿ ta wartoœæ nie bêd¹ importowane
    // (po zmianie usuñ plik dane.db, ¿eby import wykona³ siê od nowa)
    public float maksJasnosc = 8f;

    public SQLiteConnection Db { get; private set; }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    void Awake()
    {
        string sciezkaDb = Path.Combine(Application.persistentDataPath, "dane.db");
        Debug.Log("Baza danych: " + sciezkaDb);
        Db = new SQLiteConnection(sciezkaDb);

        Db.CreateTable<Miasto>();
        Db.CreateTable<Gwiazda>();
        Db.CreateTable<Konstelacja>();
        Db.CreateTable<LiniaKonstelacji>();
        Db.CreateTable<OpisKultury>();

        // Import tylko przy pierwszym uruchomieniu (puste tabele)
        string sa = Application.streamingAssetsPath;
        if (Db.Table<Miasto>().Count() == 0)
            ImportMiast(Path.Combine(sa, "cities.csv"));
        if (Db.Table<Gwiazda>().Count() == 0)
            ImportHyg(Path.Combine(sa, "hyg.csv"));
        if (Db.Table<Konstelacja>().Count() == 0)
            ImportStellarium(Path.Combine(sa, "stellarium"));

        Debug.Log("Miasta: " + Db.Table<Miasto>().Count() +
                  ", gwiazdy: " + Db.Table<Gwiazda>().Count() +
                  ", konstelacje: " + Db.Table<Konstelacja>().Count() +
                  ", odcinki: " + Db.Table<LiniaKonstelacji>().Count());

        // Szybki test zapytania: Syriusz ma numer HIP 32349
        var syriusz = Db.Table<Gwiazda>().Where(g => g.Hip == 32349).FirstOrDefault();
        Debug.Log(syriusz != null
            ? "Test: znaleziono Syriusza, jasnoœæ " + syriusz.Mag
            : "Test: nie znaleziono Syriusza (sprawdŸ import HYG)");
    }

    void OnDestroy()
    {
        if (Db != null) Db.Close();
    }

    // ---------- Import ----------

    void ImportMiast(string sciezka)
    {
        // Wspó³rzêdne z GeoNames (cities15000.txt), ³¹czone po geonameid
        var wspolrzedne = new Dictionary<int, Vector2>();
        string geo = Path.Combine(Application.streamingAssetsPath, "cities15000.txt");
        if (File.Exists(geo))
        {
            foreach (string linia in File.ReadLines(geo))
            {
                string[] p = linia.Split('\t');
                if (p.Length < 6) continue;
                if (!int.TryParse(p[0], out int id)) continue;
                if (!float.TryParse(p[4], NumberStyles.Float, Inv, out float la)) continue;
                if (!float.TryParse(p[5], NumberStyles.Float, Inv, out float lo)) continue;
                wspolrzedne[id] = new Vector2(la, lo);
            }
            Debug.Log("Wczytano wspó³rzêdne GeoNames: " + wspolrzedne.Count);
        }
        else
        {
            Debug.LogWarning("Brak cities15000.txt w StreamingAssets, miasta bêd¹ bez wspó³rzêdnych");
        }

        var lista = new List<Miasto>();
        int zWspolrzednymi = 0;
        foreach (var w in CzytajCsv(sciezka))
        {
            string nazwa = Pierwsze(w, "name", "city");
            if (string.IsNullOrEmpty(nazwa)) continue;

            int.TryParse(Pierwsze(w, "geonameid"), out int gid);

            var m = new Miasto
            {
                GeonameId = gid,
                Nazwa = nazwa,
                Kraj = Pierwsze(w, "country") ?? "",
                Region = Pierwsze(w, "subcountry") ?? ""
            };

            if (gid > 0 && wspolrzedne.TryGetValue(gid, out Vector2 v))
            {
                m.MaWspolrzedne = true;
                m.Lat = v.x;
                m.Lon = v.y;
                zWspolrzednymi++;
            }
            lista.Add(m);
        }

        Db.InsertAll(lista);
        Debug.Log("Zaimportowano miasta: " + lista.Count + " (ze wspó³rzêdnymi: " + zWspolrzednymi + ")");
    }

    void ImportHyg(string sciezka)
    {
        var lista = new List<Gwiazda>();
        foreach (var w in CzytajCsv(sciezka))
        {
            if (!float.TryParse(Pierwsze(w, "mag"), NumberStyles.Float, Inv, out float mag)) continue;
            if (mag > maksJasnosc) continue;

            float.TryParse(Pierwsze(w, "ra"), NumberStyles.Float, Inv, out float ra);
            float.TryParse(Pierwsze(w, "dec"), NumberStyles.Float, Inv, out float dec);
            float.TryParse(Pierwsze(w, "x"), NumberStyles.Float, Inv, out float x);
            float.TryParse(Pierwsze(w, "y"), NumberStyles.Float, Inv, out float y);
            float.TryParse(Pierwsze(w, "z"), NumberStyles.Float, Inv, out float z);
            int.TryParse(Pierwsze(w, "hip"), out int hip);
            float.TryParse(Pierwsze(w, "ci"), NumberStyles.Float, Inv, out float bv);

            lista.Add(new Gwiazda
            {
                Hip = hip,
                Nazwa = Pierwsze(w, "proper") ?? "",
                Ra = ra,
                Dec = dec,
                Mag = mag,
                Bv = bv,
                X = x,
                Y = y,
                Z = z
            });
        }
        Db.InsertAll(lista);
        Debug.Log("Zaimportowano gwiazdy: " + lista.Count);
    }

    void ImportStellarium(string folder)
    {
        Db.InsertOrReplace(new OpisKultury
        {
            Nazwa = "western",
            Tekst = File.ReadAllText(Path.Combine(folder, "description.md"))
        });

        JObject root = JObject.Parse(File.ReadAllText(Path.Combine(folder, "index.json")));
        JArray tab = root["constellations"] as JArray;
        if (tab == null)
        {
            Debug.LogWarning("index.json: brak tablicy 'constellations'");
            return;
        }

        var kons = new List<Konstelacja>();
        var odcinki = new List<LiniaKonstelacji>();

        foreach (JToken c in tab)
        {
            string id = (string)c["id"];
            string nazwa = c["common_name"] != null ? (string)c["common_name"]["english"] : null;

            kons.Add(new Konstelacja { Id = id, Skrot = (string)c["iau"], Nazwa = nazwa });

            JArray linie = c["lines"] as JArray;
            if (linie == null) continue;

            foreach (JToken polilinia in linie)
            {
                // Polilinia [a, b, c] -> odcinki a-b i b-c
                var hipy = new List<int>();
                foreach (JToken p in polilinia)
                    if (int.TryParse(p.ToString(), out int h)) hipy.Add(h);

                for (int i = 0; i < hipy.Count - 1; i++)
                    odcinki.Add(new LiniaKonstelacji
                    {
                        KonstelacjaId = id,
                        HipA = hipy[i],
                        HipB = hipy[i + 1]
                    });
            }
        }

        Db.InsertAll(kons);
        Db.InsertAll(odcinki);
    }

    // ---------- Pomocnicze (CSV) ----------

    // Zwraca pierwsz¹ niepust¹ wartoœæ spoœród podanych nazw kolumn
    string Pierwsze(Dictionary<string, string> w, params string[] klucze)
    {
        foreach (string k in klucze)
            if (w.TryGetValue(k, out string v) && !string.IsNullOrEmpty(v)) return v;
        return null;
    }

    // Czyta CSV strumieniowo; wiersz = s³ownik (nag³ówek -> wartoœæ), bez rozró¿niania wielkoœci liter
    IEnumerable<Dictionary<string, string>> CzytajCsv(string sciezka)
    {
        using (var sr = new StreamReader(sciezka))
        {
            string[] naglowki = ParsujLinie(sr.ReadLine());
            string linia;
            while ((linia = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(linia)) continue;
                string[] pola = ParsujLinie(linia);
                var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < naglowki.Length && i < pola.Length; i++)
                    d[naglowki[i].Trim()] = pola[i];
                yield return d;
            }
        }
    }

    // Parser linii CSV z obs³ug¹ cudzys³owów (np. "Warszawa, Polska")
    string[] ParsujLinie(string linia)
    {
        var wynik = new List<string>();
        var sb = new StringBuilder();
        bool wCudzyslowie = false;
        foreach (char c in linia)
        {
            if (c == '"') wCudzyslowie = !wCudzyslowie;
            else if (c == ',' && !wCudzyslowie) { wynik.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        wynik.Add(sb.ToString());
        return wynik.ToArray();
    }
}