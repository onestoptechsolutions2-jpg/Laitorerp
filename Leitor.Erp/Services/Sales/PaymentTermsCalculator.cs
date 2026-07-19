using System;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Sales;

// Mirrors DocumentNumbering's role as a small static helper - turns a PaymentTerms choice into a
// suggested due date. Only a default: DueDate stays directly editable afterwards on the invoice,
// same as before this existed.
public static class PaymentTermsCalculator
{
    public static DateTime DueDate(DateTime issueDate, PaymentTerms terms) => terms switch
    {
        PaymentTerms.Net15 => issueDate.AddDays(15),
        PaymentTerms.Net30 => issueDate.AddDays(30),
        PaymentTerms.Net45 => issueDate.AddDays(45),
        PaymentTerms.Net60 => issueDate.AddDays(60),
        _ => issueDate
    };
}
