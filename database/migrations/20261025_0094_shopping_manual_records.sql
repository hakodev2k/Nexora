SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
IF EXISTS(SELECT 1 FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
 WHERE s.name=N'shopping' AND t.name IN(N'Seller',N'PurchaseOrder',N'OrderLine',N'Warranty')
 AND NOT EXISTS(SELECT 1 FROM sys.extended_properties p WHERE p.major_id=t.object_id AND p.minor_id=0
 AND p.name=N'Nexora.Shopping.Contract' AND CONVERT(nvarchar(100),p.value)=N'manual-records-v1'))
 THROW 51940,'Unexpected Shopping schema requires explicit reconciliation.',1;
GO
IF OBJECT_ID(N'shopping.Seller',N'U') IS NULL
BEGIN
 CREATE TABLE shopping.Seller(
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ShoppingSeller PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL,
  Title nvarchar(200) NOT NULL,
  Url nvarchar(2048) NULL,
  MergedIntoId uniqueidentifier NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingSeller_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingSeller_Updated DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ShoppingSeller_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ShoppingSeller_Resource FOREIGN KEY(OwnerId,Id) REFERENCES platform.Resource(OwnerId,Id),
  CONSTRAINT FK_ShoppingSeller_Merge FOREIGN KEY(OwnerId,MergedIntoId) REFERENCES shopping.Seller(OwnerId,Id),
  CONSTRAINT CK_ShoppingSeller_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 1 AND 200)
 );
 CREATE INDEX IX_ShoppingSeller_Browse ON shopping.Seller(OwnerId,Title,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Shopping.Contract',@value=N'manual-records-v1',
  @level0type=N'SCHEMA',@level0name=N'shopping',@level1type=N'TABLE',@level1name=N'Seller';
END;
GO
IF OBJECT_ID(N'shopping.PurchaseOrder',N'U') IS NULL
BEGIN
 CREATE TABLE shopping.PurchaseOrder(
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ShoppingOrder PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL,
  SellerId uniqueidentifier NULL,
  SellerTitle nvarchar(200) NULL,
  OrderNumber nvarchar(200) NULL,
  PurchasedOn date NOT NULL,
  Currency char(3) COLLATE Latin1_General_100_BIN2 NOT NULL,
  Status varchar(64) NOT NULL CONSTRAINT DF_ShoppingOrder_State DEFAULT 'Draft',
  ShippingAmount decimal(28,8) NOT NULL,
  TaxAmount decimal(28,8) NOT NULL,
  DiscountAmount decimal(28,8) NOT NULL,
  TotalAmount decimal(28,8) NOT NULL,
  Notes nvarchar(max) NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingOrder_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingOrder_Updated DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ShoppingOrder_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ShoppingOrder_Resource FOREIGN KEY(OwnerId,Id) REFERENCES platform.Resource(OwnerId,Id),
  CONSTRAINT FK_ShoppingOrder_Seller FOREIGN KEY(OwnerId,SellerId) REFERENCES shopping.Seller(OwnerId,Id),
  CONSTRAINT CK_ShoppingOrder_State CHECK(Status IN('Draft','Ordered','Shipped','Delivered','Canceled','Returned')),
  CONSTRAINT CK_ShoppingOrder_Currency CHECK(Currency NOT LIKE '%[^A-Z]%' AND LEN(Currency)=3),
  CONSTRAINT CK_ShoppingOrder_Amounts CHECK(ShippingAmount>=0 AND TaxAmount>=0 AND DiscountAmount>=0 AND TotalAmount>=0),
  CONSTRAINT CK_ShoppingOrder_SellerLabel CHECK((SellerId IS NULL AND SellerTitle IS NULL) OR (SellerId IS NOT NULL AND SellerTitle IS NOT NULL))
 );
 CREATE INDEX IX_ShoppingOrder_Browse ON shopping.PurchaseOrder(OwnerId,UpdatedAt DESC,Id DESC);
 CREATE INDEX IX_ShoppingOrder_Reference ON shopping.PurchaseOrder(OwnerId,OrderNumber,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Shopping.Contract',@value=N'manual-records-v1',
  @level0type=N'SCHEMA',@level0name=N'shopping',@level1type=N'TABLE',@level1name=N'PurchaseOrder';
END;
GO
IF OBJECT_ID(N'shopping.OrderLine',N'U') IS NULL
BEGIN
 CREATE TABLE shopping.OrderLine(
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ShoppingOrderLine PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL,
  OrderId uniqueidentifier NOT NULL,
  Position int NOT NULL,
  Title nvarchar(200) NOT NULL,
  Quantity decimal(28,8) NOT NULL,
  UnitAmount decimal(28,8) NOT NULL,
  Variant nvarchar(300) NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingOrderLine_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingOrderLine_Updated DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ShoppingOrderLine_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ShoppingOrderLine_Order FOREIGN KEY(OwnerId,OrderId) REFERENCES shopping.PurchaseOrder(OwnerId,Id),
  CONSTRAINT CK_ShoppingOrderLine_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 1 AND 200),
  CONSTRAINT CK_ShoppingOrderLine_Amounts CHECK(Quantity>0 AND UnitAmount>=0),
  CONSTRAINT CK_ShoppingOrderLine_Position CHECK(Position BETWEEN 0 AND 99)
 );
 CREATE INDEX IX_ShoppingOrderLine_Order ON shopping.OrderLine(OwnerId,OrderId,Position,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Shopping.Contract',@value=N'manual-records-v1',
  @level0type=N'SCHEMA',@level0name=N'shopping',@level1type=N'TABLE',@level1name=N'OrderLine';
END;
GO
IF OBJECT_ID(N'shopping.Warranty',N'U') IS NULL
BEGIN
 CREATE TABLE shopping.Warranty(
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ShoppingWarranty PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL,
  OrderLineId uniqueidentifier NOT NULL,
  Title nvarchar(200) NOT NULL,
  CoverageKind varchar(64) NOT NULL,
  StartsOn date NULL,
  EndsOn date NULL,
  Provider nvarchar(200) NULL,
  Terms nvarchar(max) NULL,
  ClaimContact nvarchar(1000) NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingWarranty_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ShoppingWarranty_Updated DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ShoppingWarranty_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ShoppingWarranty_Resource FOREIGN KEY(OwnerId,Id) REFERENCES platform.Resource(OwnerId,Id),
  CONSTRAINT FK_ShoppingWarranty_Line FOREIGN KEY(OwnerId,OrderLineId) REFERENCES shopping.OrderLine(OwnerId,Id),
  CONSTRAINT CK_ShoppingWarranty_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 1 AND 200),
  CONSTRAINT CK_ShoppingWarranty_Coverage CHECK((CoverageKind='Fixed' AND StartsOn IS NOT NULL AND EndsOn IS NOT NULL AND EndsOn>=StartsOn)
   OR (CoverageKind IN('Lifetime','Unknown') AND StartsOn IS NULL AND EndsOn IS NULL))
 );
 CREATE INDEX IX_ShoppingWarranty_Browse ON shopping.Warranty(OwnerId,EndsOn,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Shopping.Contract',@value=N'manual-records-v1',
  @level0type=N'SCHEMA',@level0name=N'shopping',@level1type=N'TABLE',@level1name=N'Warranty';
END;
GO
IF EXISTS(SELECT 1 FROM platform.ResourceType rt JOIN platform.Module m ON m.Id=rt.ModuleId
 WHERE m.Code='FX31' AND rt.Code IN('Seller','PurchaseOrder','Warranty') AND rt.ContractVersion<>'manual-records-v1')
 THROW 51941,'Unexpected Shopping resource contract requires reconciliation.',1;
INSERT platform.ResourceType(Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
SELECT NEWID(),m.Id,s.Code,'manual-records-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
FROM platform.Module m CROSS JOIN (VALUES(N'Seller'),(N'PurchaseOrder'),(N'Warranty'))s(Code)
WHERE m.Code='FX31' AND NOT EXISTS(SELECT 1 FROM platform.ResourceType rt WITH(UPDLOCK,HOLDLOCK) WHERE rt.ModuleId=m.Id AND rt.Code=s.Code);
INSERT platform.Permission(ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES(N'shopping.seller.read'),(N'shopping.seller.create'),(N'shopping.seller.update'),
 (N'shopping.order.read'),(N'shopping.order.create'),(N'shopping.order.update'),
 (N'shopping.warranty.read'),(N'shopping.warranty.create'),(N'shopping.warranty.update'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM platform.Permission p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=s.ActionKey);
GO
COMMIT TRANSACTION;
GO
