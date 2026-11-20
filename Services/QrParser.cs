using System;
using System.Globalization;
using ExpiryTracker.Models;

namespace ExpiryTracker.Services
{
    /// <summary>
    /// Turns the text read from a QR code into a ScanResult.
    /// Handheld QR scanners act like a keyboard: they "type" the code and press Enter,
    /// so we can simply read it with Console.ReadLine().
    ///
    /// Supported payloads:
    ///   MILK-1L                  -> product code only (expiry typed in by the receiver)
    ///   MILK-1L|2026-12-31       -> product code and expiry date (yyyy-MM-dd)
    /// </summary>
    public static class QrParser
    {
        public static ScanResult Parse(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("The scan was empty.");

            string[] parts = payload.Trim().Split('|');
            var result = new ScanResult { ProductCode = parts[0].Trim().ToUpperInvariant() };

            if (result.ProductCode.Length == 0)
                throw new ArgumentException("The QR code has no product code.");

            if (parts.Length > 1)
            {
                DateTime expiry;
                if (!DateTime.TryParseExact(parts[1].Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out expiry))
                {
                    throw new ArgumentException("The expiry date in the QR code must look like 2026-12-31.");
                }
                result.ExpiryDate = expiry;
            }
            return result;
        }
    }
}
