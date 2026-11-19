using System;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.Services
{
    /// <summary>RFC 6238 TOTP (HMAC-SHA1, 30s, 6 digits). Works fully offline.</summary>
    public static class TotpHelper
    {
        public const int StepSeconds = 30;
        public const int Digits = 6;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string GenerateSecret()
        {
            var bytes = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base32Encode(bytes);
        }

        public static string FormatSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret)) return "";
            var s = secret.Replace(" ", "").ToUpperInvariant();
            var sb = new StringBuilder();
            for (var i = 0; i < s.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public static string BuildOtpAuthUri(string issuer, string accountName, string secret)
        {
            issuer = string.IsNullOrWhiteSpace(issuer) ? "HRMS" : issuer.Trim();
            accountName = string.IsNullOrWhiteSpace(accountName) ? "user" : accountName.Trim();
            var label = Uri.EscapeDataString(issuer + ":" + accountName);
            return "otpauth://totp/" + label
                + "?secret=" + (secret ?? "").Replace(" ", "").ToUpperInvariant()
                + "&issuer=" + Uri.EscapeDataString(issuer)
                + "&period=" + StepSeconds
                + "&digits=" + Digits
                + "&algorithm=SHA1";
        }

        public static bool Verify(string secret, string code, int window = 1)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim().Replace(" ", "");
            if (code.Length != Digits) return false;
            byte[] key;
            try { key = Base32Decode(secret); }
            catch { return false; }
            if (key == null || key.Length == 0) return false;

            var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;
            for (var i = -window; i <= window; i++)
            {
                if (PasswordHelper.ConstantTimeEquals(Compute(key, timestep + i), code))
                    return true;
            }
            return false;
        }

        public static string Compute(byte[] key, long counter)
        {
            var counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
            using (var hmac = new HMACSHA1(key))
            {
                var hash = hmac.ComputeHash(counterBytes);
                var offset = hash[hash.Length - 1] & 0x0F;
                var binary = ((hash[offset] & 0x7f) << 24)
                    | ((hash[offset + 1] & 0xff) << 16)
                    | ((hash[offset + 2] & 0xff) << 8)
                    | (hash[offset + 3] & 0xff);
                return (binary % 1000000).ToString("D6");
            }
        }

        public static string Base32Encode(byte[] data)
        {
            if (data == null || data.Length == 0) return "";
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            var buffer = 0;
            var bitsLeft = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    bitsLeft -= 5;
                    sb.Append(Base32Alphabet[(buffer >> bitsLeft) & 31]);
                }
            }
            if (bitsLeft > 0)
                sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
            return sb.ToString();
        }

        public static byte[] Base32Decode(string input)
        {
            var s = (input ?? "").Replace(" ", "").Replace("=", "").ToUpperInvariant();
            var buffer = 0;
            var bitsLeft = 0;
            var bytes = new byte[s.Length * 5 / 8];
            var index = 0;
            foreach (var c in s)
            {
                var val = Base32Alphabet.IndexOf(c);
                if (val < 0) throw new FormatException("Invalid Base32 character.");
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    bytes[index++] = (byte)((buffer >> bitsLeft) & 0xFF);
                }
            }
            if (index != bytes.Length)
            {
                var trimmed = new byte[index];
                Array.Copy(bytes, trimmed, index);
                return trimmed;
            }
            return bytes;
        }
    }
}
