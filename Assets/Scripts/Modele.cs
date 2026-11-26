using SQLite;

// sqlite-net mapuje w³aœciwoœci { get; set; }, a nie pola

public class Miasto
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int GeonameId { get; set; }
    [Indexed] public string Nazwa { get; set; }
    public string Kraj { get; set; }
    public string Region { get; set; }
    public bool MaWspolrzedne { get; set; }
    public float Lat { get; set; }
    public float Lon { get; set; }
}

public class Gwiazda
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int Hip { get; set; }
    public string Nazwa { get; set; }   // kolumna "proper" z HYG
    public string Bf { get; set; }      // oznaczenie Bayera/Flamsteeda, np. "9Alp CMa"
    public string Con { get; set; }     // skrót gwiazdozbioru, np. "CMa"
    public string Spect { get; set; }   // typ widmowy, np. "A0m..."
    public float Ra { get; set; }       // rektascensja (godziny)
    public float Dec { get; set; }      // deklinacja (stopnie)
    [Indexed] public float Mag { get; set; }
    public float? AbsMag { get; set; }  // jasnoœæ absolutna (null gdy brak odleg³oœci)
    public float? Dist { get; set; }    // odleg³oœæ w parsekach (null gdy nieznana)
    public float? Ci { get; set; }      // B-V
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class Konstelacja
{
    [PrimaryKey] public string Id { get; set; }   // np. "CON western Ori"
    public string Skrot { get; set; }             // np. "Ori"
    public string Nazwa { get; set; }             // np. "Hunter"
}

// Linie konstelacji rozbite na odcinki: HipA -> HipB
public class LiniaKonstelacji
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public string KonstelacjaId { get; set; }
    public int HipA { get; set; }
    public int HipB { get; set; }
}

public class OpisKultury
{
    [PrimaryKey] public string Nazwa { get; set; }   // np. "western"
    public string Tekst { get; set; }
}