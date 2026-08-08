namespace Leitor.Erp.Pages.Shared;

// Presentational only - a visual guide through the Order lifecycle Create.cshtml/Detail.cshtml
// already implement (header-first, then lines, then confirm/finalize). Doesn't gate access to
// anything below the stepper; every existing section stays reachable regardless of current step,
// same "restyle, don't rearchitect" scope as the rest of this stepper.
public enum OrderWizardStep
{
    Customer,
    Items,
    Payment,
    Review
}
