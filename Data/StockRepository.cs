using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ExpiryTracker.Models;

namespace ExpiryTracker.Data
{
    /// <summary>
    /// All SQL lives here. Every query uses parameters (@name) so user input
    /// can never be run as SQL (this prevents SQL injection).
    /// </summary>
    public class StockRepository
    {
        private const string BatchSelect = @"
            SELECT b.BatchId, b.ProductId, p.ProductCode, p.ProductName,
                   p.AisleId, a.AisleName, b.ExpiryDate, b.Quantity,
                   b.ShelfLocation, b.IsOnSale, b.SalePercent, b.AlertSentDate
            FROM StockBatches b
            JOIN Products p ON p.ProductId = b.ProductId
            JOIN Aisles   a ON a.AisleId   = p.AisleId ";

        public List<Aisle> GetAisles()
        {
            var list = new List<Aisle>();
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand("SELECT AisleId, AisleName FROM Aisles ORDER BY AisleId", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new Aisle { AisleId = reader.GetInt32(0), AisleName = reader.GetString(1) });
                }
            }
            return list;
        }

        public Product GetProductByCode(string productCode)
        {
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand(
                "SELECT ProductId, ProductCode, ProductName, AisleId FROM Products WHERE ProductCode = @code", conn))
            {
                cmd.Parameters.AddWithValue("@code", productCode);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new Product
                    {
                        ProductId = reader.GetInt32(0),
                        ProductCode = reader.GetString(1),
                        ProductName = reader.GetString(2),
                        AisleId = reader.GetInt32(3)
                    };
                }
            }
        }

        public int AddProduct(string productCode, string productName, int aisleId)
        {
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand(@"
                INSERT INTO Products (ProductCode, ProductName, AisleId)
                VALUES (@code, @name, @aisle);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                cmd.Parameters.AddWithValue("@code", productCode);
                cmd.Parameters.AddWithValue("@name", productName);
                cmd.Parameters.AddWithValue("@aisle", aisleId);
                return (int)cmd.ExecuteScalar();
            }
        }

        public int AddBatch(int productId, DateTime expiryDate, int quantity, string shelfLocation)
        {
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand(@"
                INSERT INTO StockBatches (ProductId, ExpiryDate, Quantity, ShelfLocation)
                VALUES (@pid, @expiry, @qty, @shelf);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.Parameters.AddWithValue("@expiry", expiryDate.Date);
                cmd.Parameters.AddWithValue("@qty", quantity);
                cmd.Parameters.AddWithValue("@shelf", shelfLocation);
                return (int)cmd.ExecuteScalar();
            }
        }

        /// <summary>Stock in one aisle, earliest expiry first (FEFO: first expired, first out).</summary>
        public List<StockBatch> GetShelfOrder(int aisleId)
        {
            return QueryBatches(
                BatchSelect + "WHERE p.AisleId = @aisle AND b.Quantity > 0 ORDER BY b.ExpiryDate, p.ProductName",
                cmd => cmd.Parameters.AddWithValue("@aisle", aisleId));
        }

        /// <summary>All active batches expiring on or before today + daysAhead (includes already expired).</summary>
        public List<StockBatch> GetExpiringBatches(int daysAhead)
        {
            DateTime cutoff = DateTime.Today.AddDays(daysAhead);
            return QueryBatches(
                BatchSelect + "WHERE b.Quantity > 0 AND b.ExpiryDate <= @cutoff ORDER BY b.ExpiryDate, a.AisleId",
                cmd => cmd.Parameters.AddWithValue("@cutoff", cutoff));
        }

        public void MarkOnSale(int batchId, int salePercent)
        {
            Execute("UPDATE StockBatches SET IsOnSale = 1, SalePercent = @pct WHERE BatchId = @id",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@pct", salePercent);
                    cmd.Parameters.AddWithValue("@id", batchId);
                });
        }

        public void MarkAlertSent(int batchId)
        {
            Execute("UPDATE StockBatches SET AlertSentDate = @today WHERE BatchId = @id",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@today", DateTime.Today);
                    cmd.Parameters.AddWithValue("@id", batchId);
                });
        }

        /// <summary>Reduces quantity (sold or removed). Returns false if the batch has too little stock.</summary>
        public bool ReduceQuantity(int batchId, int amount)
        {
            int rows = Execute(
                "UPDATE StockBatches SET Quantity = Quantity - @amt WHERE BatchId = @id AND Quantity >= @amt",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.Parameters.AddWithValue("@id", batchId);
                });
            return rows > 0;
        }

        // ---------- helpers ----------

        private int Execute(string sql, Action<SqlCommand> addParameters)
        {
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                addParameters(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        private List<StockBatch> QueryBatches(string sql, Action<SqlCommand> addParameters)
        {
            var list = new List<StockBatch>();
            using (var conn = Database.OpenConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                addParameters(cmd);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new StockBatch
                        {
                            BatchId = r.GetInt32(0),
                            ProductId = r.GetInt32(1),
                            ProductCode = r.GetString(2),
                            ProductName = r.GetString(3),
                            AisleId = r.GetInt32(4),
                            AisleName = r.GetString(5),
                            ExpiryDate = r.GetDateTime(6),
                            Quantity = r.GetInt32(7),
                            ShelfLocation = r.GetString(8),
                            IsOnSale = r.GetBoolean(9),
                            SalePercent = r.IsDBNull(10) ? (int?)null : r.GetInt32(10),
                            AlertSentDate = r.IsDBNull(11) ? (DateTime?)null : r.GetDateTime(11)
                        });
                    }
                }
            }
            return list;
        }
    }
}
