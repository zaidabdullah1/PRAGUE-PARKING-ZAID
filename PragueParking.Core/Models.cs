namespace PragueParking.Core;

public abstract class Fordon
{
    protected Fordon(string registreringsnummer, DateTime ankomsttid)
    {
        if (string.IsNullOrWhiteSpace(registreringsnummer))
            throw new ArgumentException("Registreringsnummer måste anges.");
        Registreringsnummer = registreringsnummer.Trim().ToUpperInvariant();
        Ankomsttid = ankomsttid;
    }
    public string Registreringsnummer { get; }
    public DateTime Ankomsttid { get; }
    public abstract string Typ { get; }
}
public sealed class Bil : Fordon
{
    public Bil(string nummer, DateTime? ankomst = null) : base(nummer, ankomst ?? DateTime.Now) { }
    public override string Typ => "Bil";
}
public sealed class MC : Fordon
{
    public MC(string nummer, DateTime? ankomst = null) : base(nummer, ankomst ?? DateTime.Now) { }
    public override string Typ => "MC";
}

public sealed class ParkeringsPlats
{
    public int Nummer { get; }
    public int Kapacitet { get; }
    public List<Fordon> Fordon { get; } = new();
    public ParkeringsPlats(int nummer, int kapacitet = 2)
    {
        if (nummer < 1 || kapacitet < 1) throw new ArgumentOutOfRangeException();
        Nummer = nummer;
        Kapacitet = kapacitet;
    }
    public int AnvandKapacitet(Func<string, int> storlek) => Fordon.Sum(f => storlek(f.Typ));
    public bool KanParkera(Fordon fordon, Func<string, int> storlek) =>
        AnvandKapacitet(storlek) + storlek(fordon.Typ) <= Kapacitet;
}

public sealed class FordonsTypInställning
{
    public string Namn { get; set; } = "";
    public int Storlek { get; set; } = 1;
    public decimal PrisPerTimme { get; set; } = 0;
}
public sealed class Konfiguration
{
    public int AntalPlatser { get; set; } = 100;
    public int StandardKapacitet { get; set; } = 2;
    public int GratisMinuter { get; set; } = 10;
    public List<FordonsTypInställning> Fordonstyper { get; set; } = new()
    {
        new() { Namn = "Bil", Storlek = 2, PrisPerTimme = 20 },
        new() { Namn = "MC", Storlek = 1, PrisPerTimme = 10 }
    };
    // Nyckeln är platsnumret, värdet är platsens kapacitet.
    public Dictionary<int, int> PlatsKapaciteter { get; set; } = new();

    public void Validera()
    {
        if (AntalPlatser < 1 || AntalPlatser > 10000 || StandardKapacitet < 1 || GratisMinuter < 0)
            throw new InvalidOperationException("Ogiltigt antal platser, standardkapacitet eller gratistid.");
        if (Fordonstyper is null || Fordonstyper.Count == 0 ||
            Fordonstyper.Any(t => string.IsNullOrWhiteSpace(t.Namn) || t.Storlek < 1 || t.PrisPerTimme < 0) ||
            Fordonstyper.Select(t => t.Namn.ToUpperInvariant()).Distinct().Count() != Fordonstyper.Count)
            throw new InvalidOperationException("Ogiltiga fordonstyper i konfigurationen.");
        if (PlatsKapaciteter is null || PlatsKapaciteter.Any(p => p.Key < 1 || p.Key > AntalPlatser || p.Value < 1))
            throw new InvalidOperationException("Ogiltig platskapacitet.");
        if (!Fordonstyper.Any(t => t.Namn.Equals("Bil", StringComparison.OrdinalIgnoreCase)) ||
            !Fordonstyper.Any(t => t.Namn.Equals("MC", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Bil och MC måste finnas i konfigurationen.");
    }
}

public sealed class ParkeringsHus
{
    public Konfiguration Konfiguration { get; }
    public List<ParkeringsPlats> Platser { get; }
    public ParkeringsHus(Konfiguration config)
    {
        config.Validera();
        Konfiguration = config;
        Platser = Enumerable.Range(1, config.AntalPlatser)
            .Select(n => new ParkeringsPlats(n, config.PlatsKapaciteter.GetValueOrDefault(n, config.StandardKapacitet)))
            .ToList();
    }
    public int Storlek(string typ) => Konfiguration.Fordonstyper.First(t =>
        t.Namn.Equals(typ, StringComparison.OrdinalIgnoreCase)).Storlek;
    public (ParkeringsPlats Plats, Fordon Fordon)? Sok(string nummer)
    {
        foreach (var plats in Platser)
        {
            var fordon = plats.Fordon.FirstOrDefault(f => f.Registreringsnummer.Equals(nummer.Trim(), StringComparison.OrdinalIgnoreCase));
            if (fordon is not null) return (plats, fordon);
        }
        return null;
    }
    public void Parkera(Fordon fordon, int platsnummer)
    {
        if (Sok(fordon.Registreringsnummer) is not null)
            throw new InvalidOperationException("Fordonet finns redan i garaget.");
        var plats = HamtaPlats(platsnummer);
        if (!plats.KanParkera(fordon, Storlek)) throw new InvalidOperationException("Fordonet får inte plats här.");
        plats.Fordon.Add(fordon);
    }
    public void Flytta(string nummer, int nyPlats)
    {
        var befintlig = Sok(nummer) ?? throw new InvalidOperationException("Fordonet hittades inte.");
        var destination = HamtaPlats(nyPlats);
        if (destination.Nummer == befintlig.Plats.Nummer) return;
        if (!destination.KanParkera(befintlig.Fordon, Storlek)) throw new InvalidOperationException("Det finns inte plats.");
        befintlig.Plats.Fordon.Remove(befintlig.Fordon);
        destination.Fordon.Add(befintlig.Fordon);
    }
    public (Fordon Fordon, decimal Kostnad, TimeSpan Tid) CheckaUt(string nummer, DateTime nu)
    {
        var befintlig = Sok(nummer) ?? throw new InvalidOperationException("Fordonet hittades inte.");
        var tid = nu - befintlig.Fordon.Ankomsttid;
        var pris = BeraknaPris(befintlig.Fordon, nu);
        befintlig.Plats.Fordon.Remove(befintlig.Fordon);
        return (befintlig.Fordon, pris, tid < TimeSpan.Zero ? TimeSpan.Zero : tid);
    }
    public decimal BeraknaPris(Fordon fordon, DateTime nu)
    {
        var minuter = Math.Max(0, (nu - fordon.Ankomsttid).TotalMinutes);
        if (minuter <= Konfiguration.GratisMinuter) return 0m;
        var timpris = Konfiguration.Fordonstyper.First(t => t.Namn.Equals(fordon.Typ, StringComparison.OrdinalIgnoreCase)).PrisPerTimme;
        return (decimal)Math.Ceiling(minuter / 60.0) * timpris;
    }
    private ParkeringsPlats HamtaPlats(int n) =>
        Platser.FirstOrDefault(p => p.Nummer == n) ?? throw new ArgumentException("Ogiltigt platsnummer.");
}
