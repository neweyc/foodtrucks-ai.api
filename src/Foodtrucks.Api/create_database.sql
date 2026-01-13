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
GO

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [TruckId] int NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [CustomerPhone] nvarchar(max) NOT NULL,
    [TrackingCode] nvarchar(max) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [VendorId] int NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Vendors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Website] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Vendors] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [MenuItemId] int NOT NULL,
    [ItemName] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [SelectedSize] nvarchar(max) NULL,
    [SelectedOptions] nvarchar(max) NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Trucks] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [PhotoUrl] nvarchar(max) NOT NULL,
    [VendorId] int NOT NULL,
    [CurrentLatitude] float NOT NULL,
    [CurrentLongitude] float NOT NULL,
    [IsActive] bit NOT NULL,
    [Schedule] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Trucks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trucks_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [TruckId] int NOT NULL,
    CONSTRAINT [PK_MenuCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuCategories_Trucks_TruckId] FOREIGN KEY ([TruckId]) REFERENCES [Trucks] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [MenuCategoryId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [PhotoUrl] nvarchar(max) NOT NULL,
    [IsAvailable] bit NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_MenuCategories_MenuCategoryId] FOREIGN KEY ([MenuCategoryId]) REFERENCES [MenuCategories] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuItemOption] (
    [Id] int NOT NULL IDENTITY,
    [MenuItemId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Section] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_MenuItemOption] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemOption_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuItemSize] (
    [Id] int NOT NULL IDENTITY,
    [MenuItemId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_MenuItemSize] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemSize_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_MenuCategories_TruckId] ON [MenuCategories] ([TruckId]);
GO

CREATE INDEX [IX_MenuItemOption_MenuItemId] ON [MenuItemOption] ([MenuItemId]);
GO

CREATE INDEX [IX_MenuItems_MenuCategoryId] ON [MenuItems] ([MenuCategoryId]);
GO

CREATE INDEX [IX_MenuItemSize_MenuItemId] ON [MenuItemSize] ([MenuItemId]);
GO

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
GO

CREATE INDEX [IX_Trucks_VendorId] ON [Trucks] ([VendorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260105003028_InitialSchema', N'8.0.8');
GO

COMMIT;
GO

