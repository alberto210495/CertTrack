namespace CertTrack;

public record DomainStats(string Cert, int Domain, int Done, int Total)
{
    public double Percent => Total == 0 ? 0 : 100.0 * Done / Total;
}

public static class StatsCalculator
{
    public static List<DomainStats> ByDomain(IEnumerable<Objective> objectives)
    {
        return objectives
            .GroupBy(o => (o.Cert, o.Domain))
            .OrderBy(g => g.Key.Cert)
            .ThenBy(g => g.Key.Domain)
            .Select(g => new DomainStats(
                g.Key.Cert,
                g.Key.Domain,
                g.Count(o => o.Status == Status.Done),
                g.Count()))
            .ToList();
    }
}
