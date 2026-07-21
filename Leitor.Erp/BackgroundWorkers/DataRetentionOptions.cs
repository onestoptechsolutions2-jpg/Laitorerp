namespace Leitor.Erp.BackgroundWorkers;

// Bound from the "DataRetention" appsettings section. 7 years is a reasonable default matching
// common financial-record retention norms, not a specific jurisdiction's mandated figure - the
// business/legal owner can override it in appsettings without a code change.
public class DataRetentionOptions
{
    public int Years { get; set; } = 7;
}
