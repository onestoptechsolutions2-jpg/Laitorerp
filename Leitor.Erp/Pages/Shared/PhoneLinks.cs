using System.Linq;

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

    public static string? WhatsApp(string? phone)
    {
        var international = ToInternationalDigits(phone);
        return international == null ? null : $"https://wa.me/{international}";
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
