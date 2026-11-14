namespace CertTrack.Core;

public static class CertName
{
    // Returns the spelling already in use for a cert, so "a+" and "A+" stay one cert.
    // A cert that is not in use yet is returned as typed.
    public static string Resolve(IEnumerable<string> certsInUse, string cert)
    {
        return certsInUse.FirstOrDefault(c => c.Equals(cert, StringComparison.OrdinalIgnoreCase)) ?? cert;
    }
}
