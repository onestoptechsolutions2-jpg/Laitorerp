using System;
using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Services.Dtos.Customers;

namespace Leitor.Erp.Documents;

// Builds the [Placeholder] -> value substitution table for a CustomerContract + its Customer, and
// applies it to a ContractTemplateSection's BodyText. Kept separate from ContractPdfDocument so the
// token table itself (BuildTokens) can be unit-tested without any QuestPDF/PDF-generation
// machinery involved - see Leitor.Erp.Tests/ContractTemplateTests.cs. Takes Dtos, not the raw
// entities - CustomerContractDto/CustomerDto already carry every field this needs.
public static class ContractTemplateRenderer
{
    private const decimal VatRate = 0.16m; // Kenyan standard VAT rate - matches the source MSA sample.

    public static Dictionary<string, string> BuildTokens(
        CustomerContractDto contract,
        CustomerDto customer,
        ErpCompanyOptions company,
        string? companySignatoryName,
        int? defaultTermMonths)
    {
        var annualFee = contract.Value ?? 0m;

        return new Dictionary<string, string>
        {
            ["[ClientName]"] = customer.Name,
            ["[ClientAddress]"] = JoinAddress(customer.AddressLine, customer.City, customer.State, customer.PostalCode, customer.Country),
            ["[StartDate]"] = contract.StartDate.ToString("d MMMM yyyy"),
            ["[EndDate]"] = contract.EndDate?.ToString("d MMMM yyyy") ?? string.Empty,
            ["[AgreementDate]"] = FormatAgreementDate(contract.StartDate),
            ["[ContractNumber]"] = contract.ContractNumber,
            ["[Purpose]"] = contract.Title,
            ["[TermMonths]"] = ResolveTermMonths(contract, defaultTermMonths),
            ["[AnnualFee]"] = annualFee.ToString("N2"),
            ["[VatAmount]"] = (annualFee * VatRate).ToString("N2"),
            ["[TotalAmount]"] = (annualFee * (1 + VatRate)).ToString("N2"),
            ["[CompanyName]"] = company.Name,
            ["[CompanyAddress]"] = JoinAddress(company.AddressLine, company.City, company.State, company.PostalCode, company.Country),
            ["[CompanySignatoryName]"] = string.IsNullOrWhiteSpace(companySignatoryName) ? "________________________" : companySignatoryName,
            ["[ClientSignatoryName]"] = string.IsNullOrWhiteSpace(contract.ClientSignatoryName) ? "________________________" : contract.ClientSignatoryName
        };
    }

    public static string Render(string text, IReadOnlyDictionary<string, string> tokens)
    {
        return tokens.Aggregate(text, (current, token) => current.Replace(token.Key, token.Value));
    }

    private static string ResolveTermMonths(CustomerContractDto contract, int? defaultTermMonths)
    {
        if (contract.EndDate.HasValue)
        {
            var months = ((contract.EndDate.Value.Year - contract.StartDate.Year) * 12) + contract.EndDate.Value.Month - contract.StartDate.Month;
            return Math.Max(months, 0).ToString();
        }

        return defaultTermMonths?.ToString() ?? string.Empty;
    }

    private static string JoinAddress(string? addressLine, string? city, string? state, string? postalCode, string? country)
    {
        var cityStatePostal = string.Join(", ", new[] { city, state, postalCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var parts = new[] { addressLine, cityStatePostal, country }.Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(", ", parts);
    }

    // "this 1st day of August, 2026" - the sample MSA's own intro-paragraph phrasing.
    private static string FormatAgreementDate(DateTime date)
    {
        return $"{Ordinal(date.Day)} day of {date:MMMM}, {date:yyyy}";
    }

    private static string Ordinal(int day)
    {
        if (day is >= 11 and <= 13)
        {
            return $"{day}th";
        }

        return (day % 10) switch
        {
            1 => $"{day}st",
            2 => $"{day}nd",
            3 => $"{day}rd",
            _ => $"{day}th"
        };
    }
}
