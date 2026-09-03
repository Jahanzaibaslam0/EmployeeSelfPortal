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

    public const string InvalidEmailMessage = "Enter a valid email address (e.g. name@company.com).";
    public const string InvalidPhoneMessage = "Enter a valid phone number (7–15 digits; +, spaces, and dashes allowed).";

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
