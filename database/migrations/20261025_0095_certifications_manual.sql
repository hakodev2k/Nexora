SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
IF OBJECT_ID(N'learning.Certification',N'U') IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'learning.Certification') AND minor_id=0
 AND name=N'Nexora.Certification.Contract' AND CONVERT(nvarchar(100),value)=N'certifications-manual-v1')
 THROW 51950,'Unexpected existing Certification schema requires explicit reconciliation.',1;
GO
IF OBJECT_ID(N'learning.Certification',N'U') IS NULL
BEGIN
 CREATE TABLE [learning].[Certification](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_Certification PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL, Issuer nvarchar(200) NOT NULL,
  IssuedOn date NULL, ExpiresOn date NULL, VerificationUrl nvarchar(2048) NULL,
  Status varchar(64) NOT NULL CONSTRAINT DF_Certification_Status DEFAULT 'Active',
  PreArchiveState varchar(64) NULL,
  VersionNumber bigint NOT NULL CONSTRAINT DF_Certification_Version DEFAULT 1,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_Certification_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_Certification_Updated DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_Certification_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_Certification_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT CK_Certification_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 1 AND 200),
  CONSTRAINT CK_Certification_Issuer CHECK(LEN(LTRIM(RTRIM(Issuer))) BETWEEN 1 AND 200),
  CONSTRAINT CK_Certification_Dates CHECK(IssuedOn IS NULL OR ExpiresOn IS NULL OR ExpiresOn>=IssuedOn),
  CONSTRAINT CK_Certification_Version CHECK(VersionNumber>=1),
  CONSTRAINT CK_Certification_State CHECK((Status='Active' AND PreArchiveState IS NULL) OR (Status='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState='Active'))
 );
 CREATE INDEX IX_Certification_Expiry ON [learning].[Certification](OwnerId,ExpiresOn,Id) INCLUDE(Status,Title,Issuer);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Certification.Contract',@value=N'certifications-manual-v1',
  @level0type=N'SCHEMA',@level0name=N'learning',@level1type=N'TABLE',@level1name=N'Certification';
END;
GO
IF OBJECT_ID(N'learning.CertificationVersion',N'U') IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'learning.CertificationVersion') AND minor_id=0
 AND name=N'Nexora.Certification.Contract' AND CONVERT(nvarchar(100),value)=N'certifications-manual-v1')
 THROW 51951,'Unexpected existing CertificationVersion schema requires explicit reconciliation.',1;
GO
IF OBJECT_ID(N'learning.CertificationVersion',N'U') IS NULL
BEGIN
 CREATE TABLE [learning].[CertificationVersion](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_CertificationVersion PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL, CertificationId uniqueidentifier NOT NULL,
  VersionNumber bigint NOT NULL, Operation varchar(32) NOT NULL,
  SafeSnapshotJson nvarchar(max) NOT NULL, Reason nvarchar(2000) NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_CertificationVersion_Created DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  CONSTRAINT UQ_CertificationVersion_Number UNIQUE(OwnerId,CertificationId,VersionNumber),
  CONSTRAINT FK_CertificationVersion_Parent FOREIGN KEY(OwnerId,CertificationId) REFERENCES [learning].[Certification](OwnerId,Id),
  CONSTRAINT CK_CertificationVersion_Snapshot CHECK(ISJSON(SafeSnapshotJson)=1),
  CONSTRAINT CK_CertificationVersion_Number CHECK(VersionNumber>=1),
  CONSTRAINT CK_CertificationVersion_Operation CHECK(Operation IN('Created','Updated','Renewed','Archived','Unarchived'))
 );
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Certification.Contract',@value=N'certifications-manual-v1',
  @level0type=N'SCHEMA',@level0name=N'learning',@level1type=N'TABLE',@level1name=N'CertificationVersion';
END;
GO
IF EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX40')
 AND Code='Certification' AND ContractVersion<>'certifications-manual-v1')
 THROW 51952,'Unexpected Certification resource contract requires explicit reconciliation.',1;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX40') AND Code='Certification')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Certification','certifications-manual-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX40';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES(N'learning.certification.read'),(N'learning.certification.create'),(N'learning.certification.update'),
 (N'learning.certification.renew'),(N'learning.certification.archive'),(N'learning.certification.unarchive')) s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=s.ActionKey);
GO
COMMIT TRANSACTION;
GO
