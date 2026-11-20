using System;
using System.Collections.Generic;
using System.Globalization;
using ExpiryTracker.Models;
using ExpiryTracker.Services;

namespace ExpiryTracker
{
    internal class Program
    {
        private static readonly StockService Stock = new StockService();
        private static readonly AlertService Alerts = new AlertService(Stock);

        private static void Main()
        {
            Console.WriteLine("=== Stock Expiry Tracker ===");

            while (true)
            {
                try
                {
                    // The expiry check runs every time the menu is shown, so managers
                    // always see the latest alerts the moment they open the system.
                    ShowAlertBanner();

                    Console.WriteLine();
                    Console.WriteLine("1. Receive stock (scan QR code)");
                    Console.WriteLine("2. Show packing list for an aisle (earliest expiry first)");
                    Console.WriteLine("3. View expiry alerts and sale items");
                    Console.WriteLine("4. Record a sale / remove stock");
                    Console.WriteLine("5. Exit");
                    Console.Write("Choose an option: ");

                    switch (Console.ReadLine())
                    {
                        case "1": ReceiveStock(); break;
                        case "2": ShowPackingList(); break;
                        case "3": ShowAlerts(); break;
                        case "4": RemoveStock(); break;
                        case "5": return;
                        default: Console.WriteLine("Please choose 1 to 5."); break;
                    }
                }
                catch (Exception ex)
                {
                    // One bad input or a database problem must not crash the program.
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private static void ShowAlertBanner()
        {
            List<StockBatch> expiring = Stock.RunExpiryCheck();
            Console.WriteLine();
            if (expiring.Count == 0)
            {
                Console.WriteLine("No stock expiring within " + Stock.AlertDays + " days.");
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("*** ALERT: " + expiring.Count + " batch(es) expire within " + Stock.AlertDays + " days. See option 3. ***");
            Console.ResetColor();
            if (Alerts.TrySendEmail(expiring)) Console.WriteLine("(Alert email sent to manager.)");
        }

        private static void ReceiveStock()
        {
            Console.Write("Scan QR code: ");
            ScanResult scan = QrParser.Parse(Console.ReadLine());

            Product product = Stock.FindProduct(scan.ProductCode);
            if (product == null)
            {
                Console.WriteLine("New product '" + scan.ProductCode + "'. Let's register it.");
                Console.Write("Product name: ");
                string name = Console.ReadLine();
                int aisleId = AskAisle();
                product = Stock.RegisterProduct(scan.ProductCode, name, aisleId);
            }

            DateTime expiry = scan.ExpiryDate ?? AskDate("Expiry date (yyyy-MM-dd): ");
            int quantity = AskNumber("Quantity received: ");

            StockBatch batch = Stock.ReceiveStock(product, expiry, quantity);
            Console.WriteLine(string.Format("Received {0} x {1}. Pack in {2}, expiry {3:yyyy-MM-dd}. Batch ID {4}.",
                batch.Quantity, batch.ProductName, batch.ShelfLocation, batch.ExpiryDate, batch.BatchId));
        }

        private static void ShowPackingList()
        {
            int aisleId = AskAisle();
            List<StockBatch> list = Stock.GetPackingList(aisleId);
            if (list.Count == 0) { Console.WriteLine("Nothing in this aisle."); return; }

            Console.WriteLine("Pack so the first line is at the FRONT of the shelf:");
            int position = 1;
            foreach (StockBatch b in list)
            {
                Console.WriteLine(string.Format("{0,2}. {1,-28} Qty {2,-4} Expires {3:yyyy-MM-dd} ({4} days)  Batch {5}",
                    position++, b.ProductName, b.Quantity, b.ExpiryDate, b.DaysToExpiry, b.BatchId));
            }
        }

        private static void ShowAlerts()
        {
            List<StockBatch> expiring = Stock.RunExpiryCheck();
            Console.WriteLine(expiring.Count == 0 ? "Nothing to report." : Alerts.BuildReport(expiring));
        }

        private static void RemoveStock()
        {
            int batchId = AskNumber("Batch ID: ");
            int amount = AskNumber("Quantity sold or removed: ");
            Console.WriteLine(Stock.ReduceStock(batchId, amount)
                ? "Stock updated."
                : "That batch does not have enough stock (or does not exist).");
        }

        // ---------- input helpers: keep asking until the input is valid ----------

        private static int AskAisle()
        {
            foreach (Aisle a in Stock.GetAisles())
                Console.WriteLine(string.Format("  {0,2} - {1}", a.AisleId, a.AisleName));

            while (true)
            {
                int id = AskNumber("Aisle number (1-14): ");
                if (id >= 1 && id <= 14) return id;
                Console.WriteLine("Please enter a number from 1 to 14.");
            }
        }

        private static int AskNumber(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                int value;
                if (int.TryParse(Console.ReadLine(), out value) && value > 0) return value;
                Console.WriteLine("Please enter a whole number greater than 0.");
            }
        }

        private static DateTime AskDate(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                DateTime date;
                if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return date;
                Console.WriteLine("Use the format yyyy-MM-dd, for example 2026-12-31.");
            }
        }
    }
}
