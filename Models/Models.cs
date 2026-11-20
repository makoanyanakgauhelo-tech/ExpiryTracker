using System;

namespace ExpiryTracker.Models
{
    public class Aisle
    {
        public int AisleId { get; set; }
        public string AisleName { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int AisleId { get; set; }
    }

    /// <summary>One delivery batch of a product, joined with product and aisle info.</summary>
    public class StockBatch
    {
        public int BatchId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int AisleId { get; set; }
        public string AisleName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public string ShelfLocation { get; set; }
        public bool IsOnSale { get; set; }
        public int? SalePercent { get; set; }
        public DateTime? AlertSentDate { get; set; }

        public int DaysToExpiry
        {
            get { return (ExpiryDate.Date - DateTime.Today).Days; }
        }

        public bool IsExpired
        {
            get { return DaysToExpiry < 0; }
        }
    }

    /// <summary>The result of scanning a QR code.</summary>
    public class ScanResult
    {
        public string ProductCode { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
