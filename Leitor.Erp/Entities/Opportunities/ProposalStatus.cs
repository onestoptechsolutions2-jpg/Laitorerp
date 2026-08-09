namespace Leitor.Erp.Entities.Opportunities;

public enum ProposalStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,

    // A previously live (even Accepted) proposal that a discovered dependency/constraint made
    // unsuitable - the original solution is preserved, not deleted (see TC-016 in the acceptance
    // test suite, the Zikis Odoo -> Jipos scenario). Set via ProposalAppService.SupersedeAsync.
    Superseded = 4
}
