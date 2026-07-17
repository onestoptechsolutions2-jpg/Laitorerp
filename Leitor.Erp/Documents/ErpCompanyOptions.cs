namespace Leitor.Erp.Documents;

// Bound from the "Company" appsettings section - lets the letterhead shown on generated PDFs be
// filled in later via Coolify env vars (e.g. Company__AddressLine) without a code change/redeploy.
public class ErpCompanyOptions
{
    public string Name { get; set; } = "Leitor Investment Company Ltd";
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
