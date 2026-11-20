using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using ExpiryTracker.Data;
using ExpiryTracker.Models;

namespace ExpiryTracker.Services
{
    /// <summary>The business rules: receiving stock, shelf order and the 14-day expiry check.</summary>
    public class StockService
    {
        private readonly StockRepository _repo = new StockRepository();

        public int AlertDays
        {
            get
            {
                int days;
                string setting = ConfigurationManager.AppSettings["AlertDaysBeforeExpiry"];
                return int.TryParse(setting, out days) && days > 0 ? days : 14;
            }
        }

        public List<Aisle> GetAisles() { return _repo.GetAisles(); }

        public Product FindProduct(string productCode) { return _repo.GetProductByCode(productCode); }

        public Product RegisterProduct(string productCode, string productName, int aisleId)
        {
            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name is required.");
            int id = _repo.AddProduct(productCode, productName.Trim(), aisleId);
            return new Product { ProductId = id, ProductCode = productCode, ProductName = productName.Trim(), AisleId = aisleId };
        }

        /// <summary>Step 1 and 2: stock arrives, is scanned and assigned to its aisle.</summary>
        public StockBatch ReceiveStock(Product product, DateTime expiryDate, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be at least 1.");
            if (expiryDate.Date < DateTime.Today)
                throw new ArgumentException("This stock has already expired and must not be shelved.");

            string shelfLocation = "Aisle " + product.AisleId.ToString("00", CultureInfo.InvariantCulture);
            int batchId = _repo.AddBatch(product.ProductId, expiryDate, quantity, shelfLocation);

            return new StockBatch
            {
                BatchId = batchId,
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                AisleId = product.AisleId,
                ExpiryDate = expiryDate.Date,
                Quantity = quantity,
                ShelfLocation = shelfLocation
            };
        }

        /// <summary>
        /// Packing order for one aisle. Earliest expiry comes first, so staff put
        /// the oldest stock at the FRONT of the shelf (FEFO).
        /// </summary>
        public List<StockBatch> GetPackingList(int aisleId) { return _repo.GetShelfOrder(aisleId); }

        /// <summary>
        /// Step 3: finds everything expiring within AlertDays, puts it on sale
        /// and returns the list so managers can be alerted.
        /// </summary>
        public List<StockBatch> RunExpiryCheck()
        {
            List<StockBatch> expiring = _repo.GetExpiringBatches(AlertDays);

            foreach (StockBatch batch in expiring)
            {
                if (batch.IsExpired) continue; // expired stock is removed, not discounted

                int percent = GetSalePercent(batch.DaysToExpiry);
                if (!batch.IsOnSale || (batch.SalePercent ?? 0) < percent)
                {
                    _repo.MarkOnSale(batch.BatchId, percent);
                    batch.IsOnSale = true;
                    batch.SalePercent = percent;
                }
            }
            return expiring;
        }

        /// <summary>
        /// Example markdown policy: the closer to expiry, the bigger the discount.
        /// These percentages are an assumption: change them to suit your store.
        /// </summary>
        public static int GetSalePercent(int daysLeft)
        {
            if (daysLeft <= 3) return 50;
            if (daysLeft <= 7) return 30;
            return 20;
        }

        public void MarkAlertSent(int batchId) { _repo.MarkAlertSent(batchId); }

        /// <summary>Use for sales and for removing expired stock.</summary>
        public bool ReduceStock(int batchId, int amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be at least 1.");
            return _repo.ReduceQuantity(batchId, amount);
        }
    }
}
