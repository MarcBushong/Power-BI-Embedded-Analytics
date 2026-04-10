# ============================================================
#  MigrateWingtipSales.ps1
#  Copies WingtipSales schema + data from source to target
#  Requirements: sqlcmd and bcp must be on PATH (SQL Server tools)
# ============================================================

# ---------- SOURCE (read-only, do not change) ---------------
$srcServer   = "devcamp.database.windows.net"
$srcDb       = "WingtipSales"
$srcUser     = "CptStudent"
$srcPassword = "pass@word1"

# ---------- TARGET (fill in your details) -------------------
$tgtServer   = "azsql-pbiembedded.database.windows.net"   # your Azure SQL server
$tgtDb       = "WingtipSales"                              # database to create/use on target
$tgtUser     = "adminAccount"                                          # target SQL login
$tgtPassword = "MySecureCode967!?"                                          # target SQL password

# ---------- Working folder for bcp flat files ---------------
$exportDir = "$PSScriptRoot\WingtipSales_Export"
New-Item -ItemType Directory -Force -Path $exportDir | Out-Null

# ============================================================
# STEP 1 – Create the database on the target (skip if exists)
# ============================================================
Write-Host "`n[1/4] Creating target database '$tgtDb' if it does not exist..." -ForegroundColor Cyan
sqlcmd -S $tgtServer -U $tgtUser -P $tgtPassword -Q "
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$tgtDb')
    CREATE DATABASE [$tgtDb];"

# ============================================================
# STEP 2 – Create schema on target
# ============================================================
Write-Host "`n[2/4] Creating schema on target..." -ForegroundColor Cyan
$schema = @"
IF OBJECT_ID('dbo.InvoiceDetails','U') IS NOT NULL DROP TABLE dbo.InvoiceDetails;
IF OBJECT_ID('dbo.Invoices','U')       IS NOT NULL DROP TABLE dbo.Invoices;
IF OBJECT_ID('dbo.Customers','U')      IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.Products','U')       IS NOT NULL DROP TABLE dbo.Products;

CREATE TABLE [dbo].[Customers] (
  [CustomerId]        INT            IDENTITY(1,1) NOT NULL,
  [FirstName]         NVARCHAR(MAX)  NOT NULL,
  [LastName]          NVARCHAR(MAX)  NOT NULL,
  [Company]           NVARCHAR(MAX)  NULL,
  [EmailAddress]      NVARCHAR(MAX)  NULL,
  [WorkPhone]         NVARCHAR(MAX)  NULL,
  [HomePhone]         NVARCHAR(MAX)  NOT NULL,
  [Address]           NVARCHAR(MAX)  NULL,
  [City]              NVARCHAR(MAX)  NULL,
  [State]             NVARCHAR(MAX)  NULL,
  [Zipcode]           NVARCHAR(MAX)  NULL,
  [Gender]            NVARCHAR(1)    NULL,
  [BirthDate]         DATETIME       NOT NULL,
  [FirstPurchaseDate] DATETIME       NULL,
  [LastPurchaseDate]  DATETIME       NULL,
  CONSTRAINT PK_Customers PRIMARY KEY ([CustomerId])
);

CREATE TABLE [dbo].[Products] (
  [ProductId]       INT             IDENTITY(1,1) NOT NULL,
  [ProductCode]     NVARCHAR(MAX)   NOT NULL,
  [Title]           NVARCHAR(MAX)   NOT NULL,
  [Description]     NVARCHAR(MAX)   NULL,
  [ProductCategory] NVARCHAR(MAX)   NULL,
  [UnitCost]        DECIMAL(9,2)    NULL,
  [ListPrice]       DECIMAL(9,2)    NOT NULL,
  [Color]           NVARCHAR(MAX)   NULL,
  [MinimumAge]      INT             NULL,
  [MaximumAge]      INT             NULL,
  [ProductImageUrl] NVARCHAR(MAX)   NULL,
  [ProductImage]    IMAGE           NULL,
  CONSTRAINT PK_Products PRIMARY KEY ([ProductId])
);

CREATE TABLE [dbo].[Invoices] (
  [InvoiceId]     INT             IDENTITY(1,1) NOT NULL,
  [InvoiceDate]   DATETIME        NOT NULL,
  [InvoiceAmount] DECIMAL(9,2)    NOT NULL,
  [InvoiceType]   NVARCHAR(MAX)   NOT NULL,
  [CustomerId]    INT             NOT NULL,
  CONSTRAINT PK_Invoices PRIMARY KEY ([InvoiceId]),
  CONSTRAINT FK_Invoices_Customers FOREIGN KEY ([CustomerId]) REFERENCES dbo.Customers([CustomerId])
);

CREATE TABLE [dbo].[InvoiceDetails] (
  [Id]           INT          IDENTITY(1,1) NOT NULL,
  [Quantity]     INT          NOT NULL,
  [SalesAmount]  DECIMAL(9,2) NOT NULL,
  [InvoiceId]    INT          NOT NULL,
  [ProductId]    INT          NOT NULL,
  CONSTRAINT PK_InvoiceDetails PRIMARY KEY ([Id]),
  CONSTRAINT FK_InvoiceDetails_Invoices  FOREIGN KEY ([InvoiceId]) REFERENCES dbo.Invoices([InvoiceId]),
  CONSTRAINT FK_InvoiceDetails_Products  FOREIGN KEY ([ProductId]) REFERENCES dbo.Products([ProductId])
);
"@

$schemaFile = "$exportDir\schema.sql"
$schema | Out-File -FilePath $schemaFile -Encoding utf8
sqlcmd -S $tgtServer -d $tgtDb -U $tgtUser -P $tgtPassword -i $schemaFile

# ============================================================
# STEP 3 – Export data from source using bcp
# ============================================================
Write-Host "`n[3/4] Exporting data from source (this may take a minute)..." -ForegroundColor Cyan

$tables = @("Customers", "Products", "Invoices", "InvoiceDetails")
foreach ($tbl in $tables) {
    Write-Host "  Exporting $tbl..."
    bcp "dbo.$tbl" out "$exportDir\$tbl.dat" -S $srcServer -d $srcDb -U $srcUser -P $srcPassword -n -q
}

# ============================================================
# STEP 4 – Import data into target using bcp
#          IDENTITY_INSERT + FK ordering respected
# ============================================================
Write-Host "`n[4/4] Importing data into target..." -ForegroundColor Cyan

$importOrder = @("Customers", "Products", "Invoices", "InvoiceDetails")
foreach ($tbl in $importOrder) {
    Write-Host "  Importing $tbl..."
    sqlcmd -S $tgtServer -d $tgtDb -U $tgtUser -P $tgtPassword -Q "SET IDENTITY_INSERT dbo.$tbl ON"
    bcp "dbo.$tbl" in "$exportDir\$tbl.dat" -S $tgtServer -d $tgtDb -U $tgtUser -P $tgtPassword -n -q -E
    sqlcmd -S $tgtServer -d $tgtDb -U $tgtUser -P $tgtPassword -Q "SET IDENTITY_INSERT dbo.$tbl OFF"
}

# ============================================================
# VERIFY
# ============================================================
Write-Host "`nVerifying row counts on target..." -ForegroundColor Cyan
sqlcmd -S $tgtServer -d $tgtDb -U $tgtUser -P $tgtPassword -W -Q "
SELECT 'Customers'     AS [Table], COUNT(*) AS [Rows] FROM dbo.Customers     UNION ALL
SELECT 'Products',                  COUNT(*)           FROM dbo.Products      UNION ALL
SELECT 'Invoices',                  COUNT(*)           FROM dbo.Invoices      UNION ALL
SELECT 'InvoiceDetails',            COUNT(*)           FROM dbo.InvoiceDetails"

Write-Host "`nDone. Export files saved to: $exportDir" -ForegroundColor Green
