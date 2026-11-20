-- ExpiryTracker database schema (SQL Server)


IF DB_ID('ExpiryTrackerDb') IS NULL CREATE DATABASE ExpiryTrackerDb;
GO
USE ExpiryTrackerDb;
GO

-- 1) The 14 aisle categories on the sales floor.

CREATE TABLE Aisles (
    AisleId   INT           NOT NULL PRIMARY KEY,
    AisleName NVARCHAR(60)  NOT NULL
);

-- 2) One row per product. ProductCode is what the QR code contains.
CREATE TABLE Products (
    ProductId   INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode NVARCHAR(50)  NOT NULL UNIQUE,
    ProductName NVARCHAR(100) NOT NULL,
    AisleId     INT           NOT NULL REFERENCES Aisles(AisleId)
);

-- 3) One row per delivery batch. The same product can have several batches
--    with different expiry dates.
CREATE TABLE StockBatches (
    BatchId       INT IDENTITY(1,1) PRIMARY KEY,
    ProductId     INT           NOT NULL REFERENCES Products(ProductId),
    ExpiryDate    DATE          NOT NULL,
    Quantity      INT           NOT NULL CHECK (Quantity >= 0),
    ShelfLocation NVARCHAR(30)  NOT NULL,
    ReceivedDate  DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    IsOnSale      BIT           NOT NULL DEFAULT 0,
    SalePercent   INT           NULL,
    AlertSentDate DATE          NULL
);

CREATE INDEX IX_StockBatches_ExpiryDate ON StockBatches(ExpiryDate);

INSERT INTO Aisles (AisleId, AisleName) VALUES
(1,'Fresh Produce'),(2,'Bakery'),(3,'Dairy & Eggs'),(4,'Meat & Poultry'),
(5,'Deli & Ready Meals'),(6,'Frozen Foods'),(7,'Beverages'),(8,'Snacks & Confectionery'),
(9,'Canned & Packaged Foods'),(10,'Baking & Cooking Ingredients'),(11,'Breakfast & Cereals'),
(12,'Health & Beauty'),(13,'Household & Cleaning'),(14,'Baby & Pet Care');
GO

-- Sample products (QR payload = ProductCode)
INSERT INTO Products (ProductCode, ProductName, AisleId) VALUES
('MILK-1L',  'Full Cream Milk 1L',   3),
('BREAD-WH', 'White Bread 700g',     2),
('YOG-STR',  'Strawberry Yoghurt',   3),
('CHKN-1KG', 'Chicken Portions 1kg', 4);
GO
