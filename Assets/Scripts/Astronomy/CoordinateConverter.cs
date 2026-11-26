using System;

public static class CoordinateConverter
{
    const double D2R = Math.PI / 180.0;

    public static double JulianDate(DateTime utc)
    {
        int y = utc.Year, m = utc.Month;
        double d = utc.Day + (utc.Hour + (utc.Minute + utc.Second / 60.0) / 60.0) / 24.0;
        if (m <= 2) { y -= 1; m += 12; }
        int a = y / 100;
        int b = 2 - a + a / 4;
        return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5;
    }

    static double Norm(double deg)
    {
        deg %= 360.0;
        return deg < 0 ? deg + 360.0 : deg;
    }

    public static void RaDecToAltAz(double raHours, double decDeg, double latDeg, double lonDeg,
                                    DateTime utc, out double altDeg, out double azDeg)
    {
        double jd = JulianDate(utc);

        double T = (jd - 2451545.0) / 36525.0;
        double zeta = (2306.2181 * T + 0.30188 * T * T + 0.017998 * T * T * T) / 3600.0 * D2R;
        double z = (2306.2181 * T + 1.09468 * T * T + 0.018203 * T * T * T) / 3600.0 * D2R;
        double theta = (2004.3109 * T - 0.42665 * T * T - 0.041833 * T * T * T) / 3600.0 * D2R;

        double ra0 = raHours * 15.0 * D2R;
        double dec0 = decDeg * D2R;
        double A = Math.Cos(dec0) * Math.Sin(ra0 + zeta);
        double B = Math.Cos(theta) * Math.Cos(dec0) * Math.Cos(ra0 + zeta) - Math.Sin(theta) * Math.Sin(dec0);
        double C = Math.Sin(theta) * Math.Cos(dec0) * Math.Cos(ra0 + zeta) + Math.Cos(theta) * Math.Sin(dec0);
        double ra = Math.Atan2(A, B) + z;
        double dec = Math.Asin(C);

        Horizontal(ra, dec, latDeg, lonDeg, jd, out altDeg, out azDeg);
    }

    public static void EquatorialOfDateToAltAz(double raHours, double decDeg, double latDeg, double lonDeg,
                                               DateTime utc, out double altDeg, out double azDeg)
    {
        Horizontal(raHours * 15.0 * D2R, decDeg * D2R, latDeg, lonDeg, JulianDate(utc), out altDeg, out azDeg);
    }

    static void Horizontal(double raRad, double decRad, double latDeg, double lonDeg, double jd,
                           out double altDeg, out double azDeg)
    {
        double d = jd - 2451545.0;
        double gmstHours = 18.697374558 + 24.06570982441908 * d;
        double lstDeg = Norm(gmstHours * 15.0 + lonDeg);

        double H = lstDeg * D2R - raRad;
        double lat = latDeg * D2R;

        double sinAlt = Math.Sin(decRad) * Math.Sin(lat) + Math.Cos(decRad) * Math.Cos(lat) * Math.Cos(H);
        sinAlt = Math.Max(-1.0, Math.Min(1.0, sinAlt));
        altDeg = Math.Asin(sinAlt) / D2R;

        double az = Math.Atan2(Math.Sin(H), Math.Cos(H) * Math.Sin(lat) - Math.Tan(decRad) * Math.Cos(lat)) / D2R + 180.0;
        azDeg = Norm(az);
    }
}
