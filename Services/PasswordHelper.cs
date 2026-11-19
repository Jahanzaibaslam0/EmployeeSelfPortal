using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.Services
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;
        public const int MinLength = 10;
        public const int MaxLength = 128;

        private static readonly string[] CommonPasswords =
        {
            "password", "password1", "password123", "admin", "admin123", "admin@123",
            "changeme", "changeme@123", "welcome", "welcome123", "hrms", "hrms@123",
            "qwerty", "12345678", "1234567890", "letmein", "passw0rd"
        };

        public static string PolicySummary =>
            "At least " + MinLength + " characters, with uppercase, lowercase, a number, and a symbol. Do not use your username or a common password.";

        public static string HashPassword(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            var hash = DeriveKey(password, salt);
            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            var parts = storedHash.Split('.');
            if (parts.Length != 2) return false;
            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
                expected = Convert.FromBase64String(parts[1]);
            }
            catch { return false; }

            var actual = DeriveKey(password, salt);
            if (actual.Length != expected.Length) return false;
            var diff = 0;
            for (var i = 0; i < actual.Length; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        public static bool TryValidatePolicy(string password, string username, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            {
                error = "Password must be at least " + MinLength + " characters.";
                return false;
            }
            if (password.Length > MaxLength)
            {
                error = "Password is too long.";
                return false;
            }
            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower)
                || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            {
                error = PolicySummary;
                return false;
            }
            if (!string.IsNullOrWhiteSpace(username)
                && password.IndexOf(username.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
            {
                error = "Password must not contain your username.";
                return false;
            }
            var lower = password.ToLowerInvariant();
            if (CommonPasswords.Any(p => lower == p))
            {
                error = "Choose a less common password.";
                return false;
            }
            return true;
        }

        public static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length) return false;
            var diff = 0;
            for (var i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(HashSize);
        }
    }
}
