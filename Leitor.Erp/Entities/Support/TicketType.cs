namespace Leitor.Erp.Entities.Support;

public enum TicketType
{
    General = 0,
    Technical = 1,
    Billing = 2,
    Complaint = 3,

    // Managed-IT-services cybersecurity line: a breach/malware/phishing/unauthorized-access
    // incident, distinct from a routine Technical ticket - see Ticket.IsSecurityBreach/ContainedDate.
    SecurityIncident = 4
}
