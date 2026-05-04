USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'InventoryOrderDB')
BEGIN
    CREATE DATABASE InventoryOrderDB;
END
GO

USE InventoryOrderDB;
GO

-- =============================
-- Products table
-- =============================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
BEGIN
    CREATE TABLE Products (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(150) NOT NULL,
        SKU             NVARCHAR(50)  NOT NULL,
        Price           DECIMAL(10,2) NOT NULL,
        QuantityInStock INT           NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Products_SKU UNIQUE (SKU)
    );
END
GO

-- =============================
-- Orders table
-- =============================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE Orders (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        CustomerName NVARCHAR(150)  NOT NULL,
        OrderDate    DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        TotalAmount  DECIMAL(10,2)  NOT NULL DEFAULT 0
    );
END
GO

-- =============================
-- OrderItems table
-- =============================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderItems' AND xtype='U')
BEGIN
    CREATE TABLE OrderItems (
        Id        INT IDENTITY(1,1) PRIMARY KEY,
        OrderId   INT           NOT NULL,
        ProductId INT           NOT NULL,
        Quantity  INT           NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_OrderItems_Orders 
            FOREIGN KEY (OrderId) 
            REFERENCES Orders(Id) 
            ON DELETE CASCADE,

        CONSTRAINT FK_OrderItems_Products 
            FOREIGN KEY (ProductId) 
            REFERENCES Products(Id) 
            ON DELETE NO ACTION
    );

    CREATE INDEX IX_OrderItems_OrderId   ON OrderItems(OrderId);
    CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
END
GO

-- =============================
-- Seed Sample Products
-- =============================
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products (Name, SKU, Price, QuantityInStock) VALUES
        ('Apple iPhone 15 Pro 128GB',   'APPL-IP15P-128',  999.00,  50),
        ('Samsung Galaxy S24 Ultra',    'SAMS-S24U-256',   1199.00, 30),
        ('Dell XPS 15 Laptop',          'DELL-XPS15-2024', 1899.00, 15),
        ('Sony WH-1000XM5 Headphones',  'SONY-WH1000XM5',  349.00,  75),
        ('Apple AirPods Pro 2nd Gen',   'APPL-APP2',       249.00,   8),
        ('Logitech MX Master 3S Mouse', 'LOGI-MXM3S',       99.99, 100),
        ('Mechanical Keyboard K80',     'KB-MECH-K80',      79.99,  40),
        ('27" 4K Monitor UltraSharp',   'MON-4K-27US',     599.00,  12),
        ('USB-C Hub 7-in-1',            'HUB-USBC-7IN1',    45.99, 200),
        ('Webcam HD 1080p Pro',         'WEB-1080P-PRO',    89.99,  35);

    PRINT 'Sample products seeded successfully.';
END
GO

PRINT 'Database setup complete!';
GO