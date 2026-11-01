using PragueParking.Core;
using PragueParking.Data;
using Spectre.Console;

var filer = new MinaFiler(AppContext.BaseDirectory);
ParkeringsHus hus;
try
{
    var konfiguration = filer.LasKonfiguration();
    filer.LasPriser(konfiguration);
    hus = filer.LasParkering(konfiguration);
}
catch (Exception e)
{
    AnsiConsole.MarkupLine($"[red]Kunde inte starta:[/] {Markup.Escape(e.Message)}");
    return;
}

while (true)
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Prague Parking").Color(Color.Blue));
    VisaKarta();
    var val = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Välj funktion")
        .AddChoices("Parkera fordon", "Hämta ut fordon", "Sök fordon", "Flytta fordon",
                    "Visa prislista", "Läs om prislista", "Spara nu", "Avsluta"));
    if (val == "Avsluta") { Forsok(() => filer.Spara(hus)); break; }
    switch (val)
    {
        case "Parkera fordon": Forsok(Parkera); break;
        case "Hämta ut fordon": Forsok(CheckaUt); break;
        case "Sök fordon": Forsok(Sok); break;
        case "Flytta fordon": Forsok(Flytta); break;
        case "Visa prislista": VisaPriser(); break;
        case "Läs om prislista": Forsok(() => { filer.LasPriser(hus.Konfiguration); AnsiConsole.MarkupLine("[green]Priser uppdaterade![/]"); }); break;
        case "Spara nu": Forsok(() => { filer.Spara(hus); AnsiConsole.MarkupLine("[green]Sparat![/]"); }); break;
    }
    AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för menyn...[/]");
    Console.ReadKey(true);
}

void Parkera()
{
    var typ = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Fordonstyp").AddChoices("Bil", "MC"));
    var nummer = FragaNummer();
    Fordon fordon = typ == "Bil" ? new Bil(nummer) : new MC(nummer);
    var mojliga = hus.Platser.Where(p => p.KanParkera(fordon, hus.Storlek)).Select(p => p.Nummer).ToList();
    if (mojliga.Count == 0) { AnsiConsole.MarkupLine("[red]Garaget är fullt för denna fordonstyp.[/]"); return; }
    AnsiConsole.MarkupLine($"Lediga alternativ (första 20): {string.Join(", ", mojliga.Take(20))}");
    var plats = AnsiConsole.Ask<int>("Ange platsnummer:", mojliga[0]);
    hus.Parkera(fordon, plats);
    filer.Spara(hus);
    AnsiConsole.MarkupLine($"[green]{Markup.Escape(typ)} {Markup.Escape(fordon.Registreringsnummer)} parkerad på plats {plats}.[/]");
}
void CheckaUt()
{
    var nummer = FragaNummer();
    var hittad = hus.Sok(nummer);
    if (hittad is null) { AnsiConsole.MarkupLine("[yellow]Fordonet hittades inte.[/]"); return; }
    var resultat = hus.CheckaUt(nummer, DateTime.Now);
    filer.Spara(hus);
    AnsiConsole.Write(new Panel($"Fordon: {resultat.Fordon.Registreringsnummer}\n" +
        $"Tid: {resultat.Tid.Days} dagar, {resultat.Tid.Hours} timmar, {resultat.Tid.Minutes} minuter\n" +
        $"Att betala: {resultat.Kostnad:0.##} CZK").Header("Kvitto"));
}
void Sok()
{
    var resultat = hus.Sok(FragaNummer());
    if (resultat is null) { AnsiConsole.MarkupLine("[yellow]Hittades inte.[/]"); return; }
    AnsiConsole.MarkupLine($"[green]{Markup.Escape(resultat.Value.Fordon.Typ)} på plats {resultat.Value.Plats.Nummer}[/]");
    AnsiConsole.WriteLine($"Ankomsttid: {resultat.Value.Fordon.Ankomsttid:yyyy-MM-dd HH:mm}");
}
void Flytta()
{
    var nummer = FragaNummer();
    var ny = AnsiConsole.Ask<int>("Flytta till plats:");
    hus.Flytta(nummer, ny);
    filer.Spara(hus);
    AnsiConsole.MarkupLine("[green]Fordonet har flyttats.[/]");
}
void VisaPriser()
{
    var tabell = new Table().AddColumn("Fordon").AddColumn("CZK / påbörjad timme");
    foreach (var typ in hus.Konfiguration.Fordonstyper)
        tabell.AddRow(Markup.Escape(typ.Namn), typ.PrisPerTimme.ToString("0.##"));
    AnsiConsole.Write(tabell);
    AnsiConsole.MarkupLine($"[green]Första {hus.Konfiguration.GratisMinuter} minuterna gratis.[/]");
}
void VisaKarta()
{
    var tabell = new Table().Border(TableBorder.Rounded).Title("Parkeringskarta");
    const int kolumner = 10;
    for (int i = 0; i < kolumner; i++) tabell.AddColumn(new TableColumn(" ").Centered());
    foreach (var grupp in hus.Platser.Chunk(kolumner))
    {
        var celler = grupp.Select(p =>
        {
            var anvand = p.AnvandKapacitet(hus.Storlek);
            string farg = anvand == 0 ? "green" : anvand >= p.Kapacitet ? "red" : "yellow";
            return $"[{farg}]{p.Nummer:000}[/]";
        }).ToList();
        while (celler.Count < kolumner) celler.Add(" ");
        tabell.AddRow(celler.ToArray());
    }
    AnsiConsole.Write(tabell);
    var lediga = hus.Platser.Count(p => p.Fordon.Count == 0);
    var delvis = hus.Platser.Count(p => p.Fordon.Count > 0 && p.AnvandKapacitet(hus.Storlek) < p.Kapacitet);
    var fulla = hus.Platser.Count - lediga - delvis;
    AnsiConsole.MarkupLine($"[green]Lediga: {lediga}[/]  [yellow]Delvis: {delvis}[/]  [red]Fulla: {fulla}[/]   Fordon: {hus.Platser.Sum(p => p.Fordon.Count)}");
    AnsiConsole.MarkupLine("[grey]Grön = tom plats, gul = delvis upptagen, röd = full.[/]");
}
string FragaNummer() => AnsiConsole.Ask<string>("Registreringsnummer:").Trim();
void Forsok(Action funktion)
{
    try { funktion(); }
    catch (Exception e) { AnsiConsole.MarkupLine($"[red]Fel:[/] {Markup.Escape(e.Message)}"); }
}
