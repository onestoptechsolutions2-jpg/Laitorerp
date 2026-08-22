using System;
using System.Linq;
using System.Net;

namespace Leitor.Erp.Pages.Shared;

// Click-to-call/SMS/WhatsApp deep links wherever a phone number is already displayed - no
// telephony provider, no API keys, just tel:/sms:/wa.me links the device's own apps handle.
// Laitor is a Kenyan business, so the WhatsApp link assumes a Kenyan local number (leading 0)
// when no country code is already present - the only heuristic available since phone fields are
// free text with no enforced format anywhere in this app.
public static class PhoneLinks
{
    private const string KenyaCountryCode = "254";

    public static string? Tel(string? phone)
    {
        var digits = DigitsWithLeadingPlus(phone);
        return digits == null ? null : $"tel:{digits}";
    }

    public static string? Sms(string? phone)
    {
        var digits = DigitsWithLeadingPlus(phone);
        return digits == null ? null : $"sms:{digits}";
    }

    // E.164 for the httpSMS API (see Services/Sms/HttpSmsClient.cs) - reuses the same
    // Kenyan-local-number heuristic as WhatsApp() instead of a second phone-parsing
    // implementation, since a bulk SMS recipient list is drawn from the same free-text
    // Lead/Customer phone fields WhatsApp links already work from.
    public static string? ToE164(string? phone)
    {
        var international = ToInternationalDigits(phone);
        return international == null ? null : $"+{international}";
    }

    // The optional message is used wherever a document (quote/order/PO/invoice) gets shared - the
    // customer's app opens with the text already typed in, editable before send, same "generate
    // then let a human review before it goes out" rule every other outbound message in this app
    // follows (see ProposalAppService's own Email/WhatsApp actions).
    public static string? WhatsApp(string? phone, string? message = null)
    {
        var international = ToInternationalDigits(phone);
        if (international == null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(message)
            ? $"https://wa.me/{international}"
            : $"https://wa.me/{international}?text={WebUtility.UrlEncode(message)}";
    }

    // "Hello John," reads as a real message from a person; "Hello John Wanjiru Kamau (Densification
    // Apartments)," reads as a mail-merge - every outbound WhatsApp/email greeting in this app
    // interpolates a customer/contact name and should use this, not the raw Name field. Falls back
    // to the full (trimmed) name if it has no obvious separator to split on, so a single-word or
    // company-style name still renders sensibly rather than as an empty string.
    public static string FirstName(string? fullName)
    {
        var trimmed = fullName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        var firstWord = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrEmpty(firstWord) ? trimmed : firstWord;
    }

    private static string? DigitsWithLeadingPlus(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var hasPlus = phone.TrimStart().StartsWith("+");
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        return hasPlus ? "+" + digits : digits;
    }

    private static string? ToInternationalDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        if (digits.StartsWith(KenyaCountryCode))
        {
            return digits;
        }

        if (digits.StartsWith("0"))
        {
            return KenyaCountryCode + digits.Substring(1);
        }

        return digits;
    }
}
