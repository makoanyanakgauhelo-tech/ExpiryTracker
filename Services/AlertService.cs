using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;
using ExpiryTracker.Models;

namespace ExpiryTracker.Services
{
    /// <summary>Builds the manager alert and (optionally) emails it.</summary>
    public class AlertService
    {
        private readonly StockService _stock;

        public AlertService(StockService stock) { _stock = stock; }

        public string BuildReport(List<StockBatch> batches)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EXPIRY ALERT " + DateTime.Today.ToString("yyyy-MM-dd"));
            sb.AppendLine("These products expire within " + _stock.AlertDays + " days and must be sold first:");
            sb.AppendLine();

            foreach (StockBatch b in batches)
            {
                string status = b.IsExpired
                    ? "EXPIRED - REMOVE FROM SHELF"
                    : b.DaysToExpiry + " day(s) left, " + b.SalePercent + "% off";

                sb.AppendLine(string.Format("Batch {0} | {1} | {2} | Qty {3} | Expires {4:yyyy-MM-dd} | {5}",
                    b.BatchId, b.ShelfLocation, b.ProductName, b.Quantity, b.ExpiryDate, status));
            }
            return sb.ToString();
        }

        /// <summary>Emails the report once per batch per day. Does nothing if ManagerEmail is empty.</summary>
        public bool TrySendEmail(List<StockBatch> batches)
        {
            string to = ConfigurationManager.AppSettings["ManagerEmail"];
            if (string.IsNullOrWhiteSpace(to)) return false;

            var unsent = batches.FindAll(b => !b.AlertSentDate.HasValue || b.AlertSentDate.Value.Date < DateTime.Today);
            if (unsent.Count == 0) return false;

            try
            {
                string host = ConfigurationManager.AppSettings["SmtpHost"];
                int port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
                string user = ConfigurationManager.AppSettings["SmtpUser"];
                string password = ConfigurationManager.AppSettings["SmtpPassword"];

                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(user, password);
                    client.Send(new MailMessage(user, to, "Stock expiry alert", BuildReport(unsent)));
                }

                foreach (StockBatch b in unsent) _stock.MarkAlertSent(b.BatchId);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email could not be sent: " + ex.Message);
                return false;
            }
        }
    }
}
