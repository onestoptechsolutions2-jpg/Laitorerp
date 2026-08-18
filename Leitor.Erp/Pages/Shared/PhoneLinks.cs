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
