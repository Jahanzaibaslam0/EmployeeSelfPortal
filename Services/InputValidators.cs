using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HRMS.Services
{
public static class InputValidators
{
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneCharsRegex = new Regex(@"^[\d\s\-\+\(\)\.]+$", RegexOptions.Compiled);
    private static readonly Regex NationalIdRegex = new Regex(@"^\d{5}-\d{7}-\d$", RegexOptions.Compiled);

    public const string InvalidEmailMessage = "Enter a valid email address (e.g. name@company.com).";
    public const string InvalidPhoneMessage = "Enter a valid phone number (7–15 digits; +, spaces, and dashes allowed).";
    public const string InvalidNationalIdMessage =
        "National ID Number must be in the format 42101-1234567-8 (5 digits - 7 digits - 1 digit).";

    public static bool IsValidEmail(string value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return !required;

        var email = value.Trim();
        if (email.Length > 150)
            return false;

        return EmailRegex.IsMatch(email);
    }

    public static bool TryValidateEmail(string value, out string error, bool required = false, string fieldLabel = "Email")
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                error = $"{fieldLabel} is required.";
                return false;
            }
            return true;
        }

        if (!IsValidEmail(value))
        {
            error = $"{fieldLabel}: {InvalidEmailMessage}";
            return false;
        }

        return true;
    }

    public static bool IsValidPhone(string value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return !required;

        var phone = value.Trim();
        if (phone.Length > 50)
            return false;

        if (!PhoneCharsRegex.IsMatch(phone))
            return false;

        var digits = phone.Count(char.IsDigit);
        return digits >= 7 && digits <= 15;
    }

    public static bool TryValidatePhone(string value, out string error, bool required = false, string fieldLabel = "Phone")
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                error = $"{fieldLabel} is required.";
                return false;
            }
            return true;
        }

        if (!IsValidPhone(value))
        {
            error = $"{fieldLabel}: {InvalidPhoneMessage}";
            return false;
        }

        return true;
    }

    public static bool IsValidNationalId(string value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return !required;
        return NationalIdRegex.IsMatch(value.Trim());
    }

    public static bool TryValidateNationalId(string value, out string error, bool required = false, string fieldLabel = "National ID Number")
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                error = fieldLabel + " is required.";
                return false;
            }
            return true;
        }

        if (!IsValidNationalId(value))
        {
            error = InvalidNationalIdMessage;
            return false;
        }

        return true;
    }

    public static bool IsEmailContactType(string contactType)
        => contactType?.Equals("PersonalEmail", StringComparison.OrdinalIgnoreCase) == true
           || contactType?.Equals("OfficialEmail", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsPhoneContactType(string contactType)
        => contactType?.Equals("PersonalMobile", StringComparison.OrdinalIgnoreCase) == true
           || contactType?.Equals("OfficialMobile", StringComparison.OrdinalIgnoreCase) == true
           || contactType?.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase) == true
           || contactType?.Equals("Emergency", StringComparison.OrdinalIgnoreCase) == true;

    public static bool TryValidateEmployeeContact(string contactType, string contactValue, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(contactValue))
            return true;

        var type = contactType?.Trim() ?? "";
        if (IsEmailContactType(type))
            return TryValidateEmail(contactValue, out error, required: false, fieldLabel: type);

        if (IsPhoneContactType(type))
            return TryValidatePhone(contactValue, out error, required: false, fieldLabel: type);

        return true;
    }

    public static bool TryValidateContactMasterFields(
        string email,
        string phone,
        string mobile,
        string whatsapp,
        string fax,
        out string error)
    {
        error = null;
        if (!TryValidateEmail(email, out error, required: false, "Email")) return false;
        if (!TryValidatePhone(phone, out error, required: false, "Phone")) return false;
        if (!TryValidatePhone(mobile, out error, required: false, "Mobile")) return false;
        if (!TryValidatePhone(whatsapp, out error, required: false, "WhatsApp")) return false;
        if (!TryValidatePhone(fax, out error, required: false, "Fax")) return false;
        return TryValidateContactMasterNoRepeat(email, phone, mobile, whatsapp, fax, out error);
    }

    public static string NormalizeContactPhone(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static string NormalizeContactEmail(string value)
        => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();

    public static bool TryValidateContactMasterNoRepeat(
        string email,
        string phone,
        string mobile,
        string whatsapp,
        string fax,
        out string error)
    {
        error = null;
        var phoneFields = new[]
        {
            ("Phone", phone),
            ("Mobile", mobile),
            ("WhatsApp", whatsapp),
            ("Fax", fax)
        };

        var seenPhones = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in phoneFields)
        {
            var key = NormalizeContactPhone(pair.Item2);
            if (key.Length == 0) continue;
            if (seenPhones.TryGetValue(key, out var otherLabel))
            {
                error = $"{pair.Item1} cannot be the same as {otherLabel}. Phone, Mobile, Fax, and WhatsApp must be unique. Please recorrect the value.";
                return false;
            }
            seenPhones[key] = pair.Item1;
        }

        return true;
    }

    public static bool TryValidateContactList(IEnumerable<(string Type, string Value)> contacts, out string error)
    {
        error = null;
        foreach (var item in contacts)
        {
            if (!TryValidateEmployeeContact(item.Type, item.Value, out error))
                return false;
        }
        return true;
    }
}
}
