# OmniZenith



## Data sources

Star, city and constellation data are stored locally in the repository under `Data/Sources/` (copies kept in case a source disappears). Details and download dates: `Data/Sources/SOURCES.md`.

| Data | Source | License |
|---|---|---|
| Star catalog (positions, magnitude, B-V) | HYG Database v4.2: https://codeberg.org/astronexus/hyg (mirror: https://www.astronexus.com/hyg) | CC BY-SA 4.0 |
| Cities (name, coordinates, time zone) | GeoNames, file `cities15000.zip`: https://download.geonames.org/export/dump/ | CC BY 4.0 |
| Constellations (lines connecting stars) | Stellarium, "western" sky culture: https://github.com/Stellarium/stellarium/tree/master/skycultures/western | see `description.md` in the source folder |
| Satellite orbital data (TLE) | CelesTrak GP API: https://celestrak.org/NORAD/documentation/gp-data-formats.php (downloaded when the app starts) | public data |
| Eclipses (Besselian elements) | NASA Eclipse Web Site: https://eclipse.gsfc.nasa.gov/ | NASA public data |
| Algorithms for Sun, Moon and planet positions | J. Meeus, *Astronomical Algorithms* | book |

Results are verified against Stellarium, timeanddate.com and heavens-above.com.
