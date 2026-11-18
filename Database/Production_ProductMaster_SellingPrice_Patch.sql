-- Product Master: optional Default Selling Price.
-- Safe to re-run. Backup production before applying.
-- Fixes: Invalid column name 'SellingPrice' when creating/updating products.

IF COL_LENGTH(N'dbo.tblProduct', N'SellingPrice') IS NULL
    ALTER TABLE dbo.tblProduct ADD SellingPrice DECIMAL(18,4) NULL;
GO
