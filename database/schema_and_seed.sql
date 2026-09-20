-- ============================================================
-- Service Booking Management System
-- Schema + Seed Data (SQL Server)
-- Dùng script này nếu không chạy `dotnet ef migrations` được.
-- Tương đương với model trong ApplicationDbContext.cs / SeedData.cs
-- ============================================================

IF DB_ID('ServiceBookingDb') IS NULL
BEGIN
    CREATE DATABASE ServiceBookingDb;
END
GO

USE ServiceBookingDb;
GO

-- ---------- Users ----------
CREATE TABLE Users (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    FullName      NVARCHAR(150)  NOT NULL,
    Email         NVARCHAR(200)  NOT NULL,
    PasswordHash  NVARCHAR(300)  NOT NULL,
    Role          NVARCHAR(20)   NOT NULL,      -- 'Customer' | 'Admin'
    IsActive      BIT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

-- ---------- Services ----------
CREATE TABLE Services (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Name             NVARCHAR(200)   NOT NULL,
    Description      NVARCHAR(1000)  NULL,
    DurationMinutes  INT             NOT NULL CHECK (DurationMinutes > 0),
    Price            DECIMAL(18,2)   NOT NULL CHECK (Price >= 0),
    IsActive         BIT             NOT NULL DEFAULT 1
);
GO

-- ---------- Staffs ----------
CREATE TABLE Staffs (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    FullName  NVARCHAR(150)  NOT NULL,
    Email     NVARCHAR(200)  NOT NULL,
    IsActive  BIT            NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Staffs_Email UNIQUE (Email)
);
GO

-- ---------- WorkSchedules ----------
CREATE TABLE WorkSchedules (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    StaffId    INT      NOT NULL,
    WorkDate   DATE     NOT NULL,
    StartTime  TIME     NOT NULL,
    EndTime    TIME     NOT NULL,
    CONSTRAINT FK_WorkSchedules_Staffs FOREIGN KEY (StaffId) REFERENCES Staffs(Id) ON DELETE CASCADE,
    CONSTRAINT CK_WorkSchedules_TimeRange CHECK (StartTime < EndTime)
);
CREATE INDEX IX_WorkSchedules_StaffId_WorkDate ON WorkSchedules (StaffId, WorkDate);
GO

-- ---------- Bookings ----------
CREATE TABLE Bookings (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    BookingCode         NVARCHAR(20)   NOT NULL,
    CustomerId          INT            NOT NULL,
    ServiceId           INT            NOT NULL,
    StaffId             INT            NOT NULL,
    StartTime           DATETIME2      NOT NULL,
    EndTime             DATETIME2      NOT NULL,
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'Pending', -- Pending | Confirmed | Completed | Cancelled
    CustomerNote        NVARCHAR(500)  NULL,
    CancellationReason  NVARCHAR(500)  NULL,
    CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Bookings_BookingCode UNIQUE (BookingCode),
    CONSTRAINT FK_Bookings_Users FOREIGN KEY (CustomerId) REFERENCES Users(Id),
    CONSTRAINT FK_Bookings_Services FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    CONSTRAINT FK_Bookings_Staffs FOREIGN KEY (StaffId) REFERENCES Staffs(Id),
    CONSTRAINT CK_Bookings_TimeRange CHECK (StartTime < EndTime)
);
CREATE INDEX IX_Bookings_StaffId_StartTime_EndTime ON Bookings (StaffId, StartTime, EndTime);
CREATE INDEX IX_Bookings_Status ON Bookings (Status);
GO

-- ============================================================
-- SEED DATA
-- Mật khẩu: Admin@123 (admin) / Customer@123 (2 customer)
-- Hash BCrypt được tính sẵn, tương thích với BCrypt.Net-Next dùng trong backend
-- ============================================================

SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, IsActive, CreatedAt) VALUES
(1, N'System Admin',  'admin@bookingdemo.com',     '$2b$11$8eHBHt/uDocDWkhSuExsJeCCqHt0Zwi17kdIAxLV4kJG.Q1gcqgUK', 'Admin',    1, '2026-09-10T08:00:00'),
(2, N'Nguyen Van A',  'customer1@bookingdemo.com', '$2b$11$LdAU0Mx2RLKLlBT1ur3oK.du2VYk6.LKhdYV5Om4H64j4bdJncrWW', 'Customer', 1, '2026-09-10T08:00:00'),
(3, N'Tran Thi B',    'customer2@bookingdemo.com', '$2b$11$LdAU0Mx2RLKLlBT1ur3oK.du2VYk6.LKhdYV5Om4H64j4bdJncrWW', 'Customer', 1, '2026-09-10T08:00:00');
SET IDENTITY_INSERT Users OFF;
GO

SET IDENTITY_INSERT Staffs ON;
INSERT INTO Staffs (Id, FullName, Email, IsActive) VALUES
(1, N'Le Van Staff',   'staff1@bookingdemo.com', 1),
(2, N'Pham Thi Staff', 'staff2@bookingdemo.com', 1);
SET IDENTITY_INSERT Staffs OFF;
GO

SET IDENTITY_INSERT Services ON;
INSERT INTO Services (Id, Name, Description, DurationMinutes, Price, IsActive) VALUES
(1, N'Cắt tóc nam',          N'Cắt gọn gàng, tạo kiểu cơ bản',   30, 100000, 1),
(2, N'Uốn tóc',               N'Uốn nếp tự nhiên',                90, 350000, 1),
(3, N'Nhuộm tóc',             N'Nhuộm màu theo yêu cầu',          60, 300000, 1),
(4, N'Gội đầu dưỡng sinh',   N'Massage thư giãn da đầu',          45, 150000, 1),
(5, N'Spa da mặt',            N'Chăm sóc và làm sạch da mặt',     60, 400000, 1);
SET IDENTITY_INSERT Services OFF;
GO

SET IDENTITY_INSERT WorkSchedules ON;
INSERT INTO WorkSchedules (Id, StaffId, WorkDate, StartTime, EndTime) VALUES
(1,  1, '2026-09-15', '08:00', '17:00'),
(2,  2, '2026-09-15', '08:00', '17:00'),
(3,  1, '2026-09-16', '08:00', '17:00'),
(4,  2, '2026-09-16', '08:00', '17:00'),
(5,  1, '2026-09-17', '08:00', '17:00'),
(6,  2, '2026-09-17', '08:00', '17:00'),
(7,  1, '2026-09-18', '08:00', '17:00'),
(8,  2, '2026-09-18', '08:00', '17:00'),
(9,  1, '2026-09-19', '08:00', '17:00'),
(10, 2, '2026-09-19', '08:00', '17:00'),
(11, 1, '2026-09-20', '08:00', '17:00'),
(12, 2, '2026-09-20', '08:00', '17:00'),
(13, 1, '2026-09-21', '08:00', '17:00'),
(14, 2, '2026-09-21', '08:00', '17:00');
SET IDENTITY_INSERT WorkSchedules OFF;
GO

SET IDENTITY_INSERT Bookings ON;
INSERT INTO Bookings (Id, BookingCode, CustomerId, ServiceId, StaffId, StartTime, EndTime, Status, CustomerNote, CancellationReason, CreatedAt) VALUES
(1,  'BK000001', 2, 1, 1, '2026-09-15T09:00:00', '2026-09-15T09:30:00', 'Completed', N'Cắt gọn nhẹ', NULL, '2026-09-10T08:00:00'),
(2,  'BK000002', 2, 3, 1, '2026-09-15T10:00:00', '2026-09-15T11:00:00', 'Confirmed', NULL, NULL, '2026-09-10T08:00:00'),
(3,  'BK000003', 3, 2, 2, '2026-09-15T09:00:00', '2026-09-15T10:30:00', 'Completed', NULL, NULL, '2026-09-10T08:00:00'),
(4,  'BK000004', 3, 4, 2, '2026-09-16T08:00:00', '2026-09-16T08:45:00', 'Cancelled', NULL, N'Khách bận đột xuất', '2026-09-10T08:00:00'),
(5,  'BK000005', 2, 5, 1, '2026-09-16T13:00:00', '2026-09-16T14:00:00', 'Pending', NULL, NULL, '2026-09-10T08:00:00'),
(6,  'BK000006', 3, 1, 1, '2026-09-17T09:00:00', '2026-09-17T09:30:00', 'Confirmed', NULL, NULL, '2026-09-10T08:00:00'),
(7,  'BK000007', 2, 3, 2, '2026-09-17T14:00:00', '2026-09-17T15:00:00', 'Pending', NULL, NULL, '2026-09-10T08:00:00'),
(8,  'BK000008', 3, 2, 1, '2026-09-18T10:00:00', '2026-09-18T11:30:00', 'Cancelled', NULL, N'Đổi lịch khác', '2026-09-10T08:00:00'),
(9,  'BK000009', 2, 4, 2, '2026-09-18T15:00:00', '2026-09-18T15:45:00', 'Confirmed', NULL, NULL, '2026-09-10T08:00:00'),
(10, 'BK000010', 3, 5, 1, '2026-09-19T11:00:00', '2026-09-19T12:00:00', 'Pending', NULL, NULL, '2026-09-10T08:00:00');
SET IDENTITY_INSERT Bookings OFF;
GO
