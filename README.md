# Prague Parking 2.0 – OOP

C#/.NET 8-lösning med fyra projekt:

- **PragueParking.Core** – OOP-modeller och parkeringslogik (`Fordon`, `Bil`, `MC`, `ParkeringsPlats`, `ParkeringsHus`).
- **PragueParking.Data** – separat klassbibliotek för JSON, konfiguration och prislista (`MinaFiler`).
- **PragueParking.App** – konsolgränssnitt med **Spectre.Console**.
- **PragueParking.Tests** – två MSTest-enhetstester.

## Starta

Öppna `PragueParking.sln` i Visual Studio 2022 (med .NET 8 SDK), välj **PragueParking.App** som startprojekt och kör. Alternativt:

```bash
dotnet restore PragueParking.sln
dotnet run --project PragueParking.App
dotnet test PragueParking.sln
```

Installationen av paket sker normalt via NuGet vid `dotnet restore` (internet behövs första gången).

## Funktioner

- Parkera, hämta ut, sök, flytta.
- Karta med grön (tom), gul (delvis), röd (full) och tydliga totalsiffror.
- Parkeringstid och avgift på kvitto.
- Automatisk sparning till `parkering.json` efter ändringar och vid avslut.
- Inläsning vid start; datafilerna ligger i programmets **output-katalog** (`bin/Debug/net8.0` vid vanlig utvecklingskörning).
- `config.json`: antal platser, fordonsstorlekar och kapaciteter per ruta.
- `priser.txt`: enkla `namn=belopp`-rader, kommentarer efter `#`, omläsning i meny.

### Hur kapacitet fungerar

En standardruta har kapacitet **2**: bil använder **2**, MC använder **1**. Det går alltså in en bil eller två MC. Ändra `PlatsKapaciteter` för vissa större platser (plats 1 och 2 har exempelvis kapacitet 4).

**Utbyggnad:** Konfigurationen kan beskriva fler fordonstyper, men för att faktiskt parkera t.ex. buss och cykel måste nya subklasser till `Fordon` samt stöd i meny och JSON-inläsning läggas till. Nuvarande krav säger endast Bil och MC.

### Debitering

Gratis om parkeringstiden är **högst 10 minuter**. Därefter debiteras hela **påbörjade timmar från ankomst**: 11–60 minuter = 1 timme, 61–120 = 2 timmar, osv. Detta är en tolkning av uppgiftens prisregler; kontrollera med läraren om gratistiden i stället ska dras av innan timavrundning.

## Viktigt före inlämning

1. Kör `dotnet restore`, `dotnet build` och `dotnet test` lokalt, prova menyerna och kontrollera att sparad data återkommer efter omstart.
2. Skriv din **egen, sanningsenliga loggbok** (mall i `LOGGBOK_MALL.md`).
3. Lägg upp **hela lösningen** i ett publikt GitHub-repo och lämna in länken till repot.

**Observera:** Projektet är framtaget utan tillgänglig .NET-kompilator i skapandemiljön och är därför inte lokalt kompilerat eller testkört ännu.
