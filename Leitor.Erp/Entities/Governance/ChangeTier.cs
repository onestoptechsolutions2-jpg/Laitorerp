namespace Leitor.Erp.Entities.Governance;

// ITIL Change Enablement's own 3-tier model, adopted as-is rather than inventing a bespoke one -
// this is exactly the mechanism that keeps change control agile instead of a single heavyweight
// gate: only Normal changes wait on anyone.
public enum ChangeTier
{
    // Pre-authorized, low-risk, well-understood (e.g. a routine patch cycle already agreed with
    // the client) - never enters PendingApproval, goes straight to Approved.
    Standard = 0,

    // Needs sign-off before work starts - the only tier that actually uses PendingApproval.
    Normal = 1,

    // Work happens immediately (the incident causing it can't wait), but is flagged for mandatory
    // review after the fact - see ChangeRequest.PostImplementationReviewedDate.
    Emergency = 2
}
