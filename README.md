# Stock Expiry Tracker

A C# (.NET Framework 4.8) console application with a SQL Server database that tracks
product expiry dates for a retail store.

## What it does
1. **Receive stock**: scan a product QR code, enter the expiry date and quantity.
2. **Shelf order**: stock is assigned to one of 14 aisle categories and a packing list shows
   the earliest expiry first (FEFO: first expired, first out).
3. **Expiry alerts**: every batch expiring within 14 days is flagged for managers
   (on screen, and by email if configured) and automatically put on sale with a tiered discount.

## Tech
C#, .NET Framework 4.8, ADO.NET (`System.Data.SqlClient`), SQL Server, parameterised queries.

## Project structure
- `Database/schema.sql`: tables (Aisles, Products, StockBatches) and sample data
- `Models/`: Aisle, Product, StockBatch, ScanResult
- `Data/`: database connection and all SQL (repository pattern)
- `Services/`: QR parsing, business rules (StockService), alerts (AlertService)
- `Program.cs`: the console menu

## How to run
1. Run `Database/schema.sql` in SQL Server Management Studio.
2. Open `ExpiryTracker.csproj` in Visual Studio 2022.
3. Check the connection string in `App.config`.
4. Press F5.

## QR code format
`PRODUCTCODE` or `PRODUCTCODE|yyyy-MM-dd` (for example `MILK-1L|2026-12-31`).
Handheld scanners type the code like a keyboard, so no extra scanner library is needed.

## Discount policy (configurable in `StockService.GetSalePercent`)
3 days or less: 50% | 7 days or less: 30% | 14 days or less: 20%

## Possible improvements
Windows Forms or web interface, user logins for managers, sales history reports,
GS1 barcode support, a scheduled background alert, unit tests.
