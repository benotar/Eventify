IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE TABLE [Artists] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Bio] nvarchar(2000) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Artists] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE TABLE [Venues] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address_City] nvarchar(100) NOT NULL,
        [Address_Country] nvarchar(100) NOT NULL,
        [Address_State] nvarchar(100) NULL,
        [Address_Street] nvarchar(200) NOT NULL,
        [Address_ZipCode] nvarchar(20) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Venues] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Artists_Name] ON [Artists] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Venues_Name] ON [Venues] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Venues_Address_City] ON [Venues] ([Address_City]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    CREATE INDEX [IX_Venues_Address_ZipCode] ON [Venues] ([Address_ZipCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919143817_InitialCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919143817_InitialCatalog', N'10.0.11');
END;

COMMIT;
GO

