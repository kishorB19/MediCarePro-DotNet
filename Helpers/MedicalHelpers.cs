using System.Text.RegularExpressions;

namespace HMS.Helpers
{
    public static class MedicalHelpers
    {
        public static string FormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            var clean = Regex.Replace(phone, @"[^\d]", "");
            if (clean.Length == 10)
            {
                return $"({clean.Substring(0, 3)}) {clean.Substring(3, 3)}-{clean.Substring(6)}";
            }
            return phone;
        }

        public static int CalculateAge(DateTime? dob)
        {
            if (!dob.HasValue) return 0;
            var today = DateTime.Today;
            var age = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-age)) age--;
            return age;
        }

        public static string GetSeverityLevel(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return "Normal";
            var text = reason.ToLower();
            if (text.Contains("chest pain") || text.Contains("severe") || text.Contains("difficulty breathing") || text.Contains("accident"))
            {
                return "Urgent";
            }
            if (text.Contains("fever") || text.Contains("pain") || text.Contains("cough"))
            {
                return "Moderate";
            }
            return "Routine";
        }

        public static decimal ApplyDiscount(decimal fee, string? coupon)
        {
            if (string.IsNullOrWhiteSpace(coupon)) return fee;
            var code = coupon.ToUpper().Trim();
            if (code == "HEALTH10")
            {
                return fee * 0.90m;
            }
            if (code == "HEALTH20")
            {
                return fee * 0.80m;
            }
            return fee;
        }

        public static string FormatCurrency(decimal amount)
        {
            return $"Rs. {amount:N2}";
        }
    }
}
