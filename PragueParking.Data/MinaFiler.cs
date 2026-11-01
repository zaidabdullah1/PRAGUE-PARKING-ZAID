using System.Text.Json;
using PragueParking.Core;
namespace PragueParking.Data;

public sealed class MinaFiler
{
    private readonly string _mapp;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public MinaFiler(string mapp) { _mapp = mapp; Directory.CreateDirectory(mapp); }
    private string KonfigFil => Path.Combine(_mapp, "config.json");
    private string PrisFil => Path.Combine(_mapp, "priser.txt");
    private string DataFil => Path.Combine(_mapp, "parkering.json");

    public Konfiguration LasKonfiguration()
    {
        if (!File.Exists(KonfigFil)) File.WriteAllText(KonfigFil, JsonSerializer.Serialize(new Konfiguration(), JsonOptions));
        var config = JsonSerializer.Deserialize<Konfiguration>(File.ReadAllText(KonfigFil), JsonOptions)
            ?? throw new InvalidDataException("Konfigurationsfilen är tom.");
        config.Validera();
        return config;
    }
    public void SkapaStandardPriserOmSaknas()
    {
        if (!File.Exists(PrisFil)) File.WriteAllText(PrisFil,
            "# Redigera priser och gratisminuter. Allt efter # ignoreras.\n" +
            "GratisMinuter=10 # Gratis tid från ankomst\nBil=20 # CZK per påbörjad timme\nMC=10 # CZK per påbörjad timme\n");
    }
    public void LasPriser(Konfiguration config)
    {
        SkapaStandardPriserOmSaknas();
        var uppdateringar = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(PrisFil))
        {
            var rad = raw.Split('#', 2)[0].Trim();
            if (rad.Length == 0) continue;
            var delar = rad.Split('=', 2);
            if (delar.Length != 2 || !decimal.TryParse(delar[1].Trim(), System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var pris) || pris < 0)
                throw new InvalidDataException($"Ogiltig rad i priser.txt: {raw}");
            var nyckel = delar[0].Trim();
            if (uppdateringar.ContainsKey(nyckel)) throw new InvalidDataException($"Dubblett i priser.txt: {nyckel}");
            uppdateringar[nyckel] = pris;
        }
        var tillatna = config.Fordonstyper.Select(t => t.Namn).Append("GratisMinuter").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (uppdateringar.Keys.Any(k => !tillatna.Contains(k))) throw new InvalidDataException("Okänd nyckel i priser.txt.");
        if (uppdateringar.TryGetValue("GratisMinuter", out var gratis))
        {
            if (gratis != decimal.Truncate(gratis) || gratis > int.MaxValue) throw new InvalidDataException("GratisMinuter måste vara ett heltal.");
            config.GratisMinuter = (int)gratis;
        }
        foreach (var typ in config.Fordonstyper)
            if (uppdateringar.TryGetValue(typ.Namn, out var pris)) typ.PrisPerTimme = pris;
    }
    private sealed class FordonsPost
    {
        public string Typ { get; set; } = "";
        public string Registreringsnummer { get; set; } = "";
        public DateTime Ankomsttid { get; set; }
        public int Platsnummer { get; set; }
    }
    public void Spara(ParkeringsHus hus)
    {
        var poster = hus.Platser.SelectMany(p => p.Fordon.Select(f => new FordonsPost
        {
            Typ = f.Typ, Registreringsnummer = f.Registreringsnummer,
            Ankomsttid = f.Ankomsttid, Platsnummer = p.Nummer
        })).ToList();
        var tillfallig = DataFil + ".tmp";
        File.WriteAllText(tillfallig, JsonSerializer.Serialize(poster, JsonOptions));
        File.Move(tillfallig, DataFil, true);
    }
    public ParkeringsHus LasParkering(Konfiguration config)
    {
        var hus = new ParkeringsHus(config);
        if (!File.Exists(DataFil)) return hus;
        var poster = JsonSerializer.Deserialize<List<FordonsPost>>(File.ReadAllText(DataFil), JsonOptions)
            ?? throw new InvalidDataException("Parkeringsfilen är tom.");
        foreach (var post in poster)
        {
            Fordon f = post.Typ.ToUpperInvariant() switch
            {
                "BIL" => new Bil(post.Registreringsnummer, post.Ankomsttid),
                "MC" => new MC(post.Registreringsnummer, post.Ankomsttid),
                _ => throw new InvalidDataException($"Fordontypen {post.Typ} saknar implementation.")
            };
            hus.Parkera(f, post.Platsnummer);
        }
        return hus;
    }
}
