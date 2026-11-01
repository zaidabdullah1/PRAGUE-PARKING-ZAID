using Microsoft.VisualStudio.TestTools.UnitTesting;
using PragueParking.Core;
namespace PragueParking.Tests;
[TestClass]
public class ParkeringsTester
{
    [TestMethod]
    public void TvaMcFarPlatsPaEnStandardruta()
    {
        var hus = new ParkeringsHus(new Konfiguration());
        hus.Parkera(new MC("MC1"), 5);
        hus.Parkera(new MC("MC2"), 5);
        Assert.AreEqual(2, hus.Platser[4].Fordon.Count);
        Assert.ThrowsException<InvalidOperationException>(() => hus.Parkera(new MC("MC3"), 5));
    }
    [TestMethod]
    public void AvgiftBeraknasPerPaborjadTimmeEfterGratisTid()
    {
        var hus = new ParkeringsHus(new Konfiguration());
        var start = new DateTime(2026, 1, 1, 12, 0, 0);
        var bil = new Bil("ABC123", start);
        Assert.AreEqual(0m, hus.BeraknaPris(bil, start.AddMinutes(10)));
        Assert.AreEqual(20m, hus.BeraknaPris(bil, start.AddMinutes(11)));
        Assert.AreEqual(40m, hus.BeraknaPris(bil, start.AddMinutes(61)));
    }
}
