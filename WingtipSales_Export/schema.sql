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
  [Zipcode]           NVARCHAR(MAX)  NULL,  -- must match WingtipSales exactly (Power Query is case-sensitive)
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
