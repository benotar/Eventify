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
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [DeviceCodes] (
        [UserCode] nvarchar(200) NOT NULL,
        [DeviceCode] nvarchar(200) NOT NULL,
        [SubjectId] nvarchar(200) NULL,
        [SessionId] nvarchar(100) NULL,
        [ClientId] nvarchar(200) NOT NULL,
        [Description] nvarchar(200) NULL,
        [CreationTime] datetime2 NOT NULL,
        [Expiration] datetime2 NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DeviceCodes] PRIMARY KEY ([UserCode])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [Keys] (
        [Id] nvarchar(450) NOT NULL,
        [Version] int NOT NULL,
        [Created] datetime2 NOT NULL,
        [Use] nvarchar(450) NULL,
        [Algorithm] nvarchar(100) NOT NULL,
        [IsX509Certificate] bit NOT NULL,
        [DataProtected] bit NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Keys] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [PersistedGrants] (
        [Id] bigint NOT NULL IDENTITY,
        [Key] nvarchar(200) NULL,
        [Type] nvarchar(50) NOT NULL,
        [SubjectId] nvarchar(200) NULL,
        [SessionId] nvarchar(100) NULL,
        [ClientId] nvarchar(200) NOT NULL,
        [Description] nvarchar(200) NULL,
        [CreationTime] datetime2 NOT NULL,
        [Expiration] datetime2 NULL,
        [ConsumedTime] datetime2 NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PersistedGrants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [PushedAuthorizationRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [ReferenceValueHash] nvarchar(64) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Parameters] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PushedAuthorizationRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [SamlLogoutSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [LogoutId] nvarchar(200) NOT NULL,
        [SerializedSession] nvarchar(max) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_SamlLogoutSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [SamlSigninStates] (
        [Id] bigint NOT NULL IDENTITY,
        [StateId] uniqueidentifier NOT NULL,
        [SerializedState] nvarchar(max) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [ServiceProviderEntityId] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_SamlSigninStates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [ServerSideSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [Scheme] nvarchar(100) NOT NULL,
        [SubjectId] nvarchar(100) NOT NULL,
        [SessionId] nvarchar(100) NULL,
        [DisplayName] nvarchar(100) NULL,
        [Created] datetime2 NOT NULL,
        [Renewed] datetime2 NOT NULL,
        [Expires] datetime2 NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ServerSideSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE TABLE [SamlLogoutSessionRequestIndices] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestId] nvarchar(200) NOT NULL,
        [SamlLogoutSessionId] bigint NOT NULL,
        CONSTRAINT [PK_SamlLogoutSessionRequestIndices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SamlLogoutSessionRequestIndices_SamlLogoutSessions_SamlLogoutSessionId] FOREIGN KEY ([SamlLogoutSessionId]) REFERENCES [SamlLogoutSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeviceCodes_DeviceCode] ON [DeviceCodes] ([DeviceCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_DeviceCodes_Expiration] ON [DeviceCodes] ([Expiration]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_Keys_Use] ON [Keys] ([Use]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_PersistedGrants_ConsumedTime] ON [PersistedGrants] ([ConsumedTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_PersistedGrants_Expiration] ON [PersistedGrants] ([Expiration]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PersistedGrants_Key] ON [PersistedGrants] ([Key]) WHERE [Key] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_PersistedGrants_SubjectId_ClientId_Type] ON [PersistedGrants] ([SubjectId], [ClientId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_PersistedGrants_SubjectId_SessionId_Type] ON [PersistedGrants] ([SubjectId], [SessionId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_PushedAuthorizationRequests_ExpiresAtUtc] ON [PushedAuthorizationRequests] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PushedAuthorizationRequests_ReferenceValueHash] ON [PushedAuthorizationRequests] ([ReferenceValueHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SamlLogoutSessionRequestIndices_RequestId] ON [SamlLogoutSessionRequestIndices] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_SamlLogoutSessionRequestIndices_SamlLogoutSessionId] ON [SamlLogoutSessionRequestIndices] ([SamlLogoutSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_SamlLogoutSessions_ExpiresAtUtc] ON [SamlLogoutSessions] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SamlLogoutSessions_LogoutId] ON [SamlLogoutSessions] ([LogoutId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_SamlSigninStates_ExpiresAtUtc] ON [SamlSigninStates] ([ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SamlSigninStates_StateId] ON [SamlSigninStates] ([StateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_ServerSideSessions_DisplayName] ON [ServerSideSessions] ([DisplayName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_ServerSideSessions_Expires] ON [ServerSideSessions] ([Expires]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServerSideSessions_Key] ON [ServerSideSessions] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_ServerSideSessions_SessionId] ON [ServerSideSessions] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    CREATE INDEX [IX_ServerSideSessions_SubjectId] ON [ServerSideSessions] ([SubjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919114912_InitialDuendeOperationalSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919114912_InitialDuendeOperationalSchema', N'10.0.11');
END;

COMMIT;
GO

