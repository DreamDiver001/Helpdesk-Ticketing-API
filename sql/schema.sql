-- Helpdesk Ticketing System: SQL Server schema
-- Run in SSMS or Azure Data Studio.

CREATE DATABASE HelpdeskDb;
GO
USE HelpdeskDb;
GO

CREATE TABLE Customers (
    CustomerId   INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    Company      NVARCHAR(100) NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Agents (
    AgentId      INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    IsActive     BIT NOT NULL DEFAULT 1
);

CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE Tickets (
    TicketId     INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId   INT NOT NULL REFERENCES Customers(CustomerId),
    AgentId      INT NULL REFERENCES Agents(AgentId),
    CategoryId   INT NOT NULL REFERENCES Categories(CategoryId),
    Title        NVARCHAR(200) NOT NULL,
    Description  NVARCHAR(MAX) NOT NULL,
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Open'
                 CHECK (Status IN ('Open','InProgress','Resolved','Closed')),
    Priority     NVARCHAR(10) NOT NULL DEFAULT 'Medium'
                 CHECK (Priority IN ('Low','Medium','High','Critical')),
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ResolvedAt   DATETIME2 NULL
);

CREATE TABLE TicketComments (
    CommentId    INT IDENTITY(1,1) PRIMARY KEY,
    TicketId     INT NOT NULL REFERENCES Tickets(TicketId) ON DELETE CASCADE,
    AgentId      INT NULL REFERENCES Agents(AgentId),   -- NULL = customer comment
    Body         NVARCHAR(MAX) NOT NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX IX_Tickets_Status ON Tickets(Status);
CREATE INDEX IX_Tickets_CustomerId ON Tickets(CustomerId);
GO

-- Sample data
INSERT INTO Categories (Name) VALUES ('Login Issue'), ('Billing'), ('Bug Report'), ('Feature Request');
INSERT INTO Agents (Name, Email) VALUES ('Anna Kovacs', 'anna@helpdesk.local'), ('Peter Szabo', 'peter@helpdesk.local');
INSERT INTO Customers (Name, Email, Company) VALUES
    ('Laszlo Nagy', 'laszlo@acme.com', 'Acme Ltd'),
    ('Eva Toth', 'eva@globex.com', 'Globex');
INSERT INTO Tickets (CustomerId, AgentId, CategoryId, Title, Description, Priority) VALUES
    (1, 1, 1, 'Cannot log in', 'Password reset email never arrives.', 'High'),
    (2, NULL, 2, 'Invoice mismatch', 'October invoice shows wrong amount.', 'Medium');
GO

-- View: open tickets with customer and agent names
CREATE VIEW vw_OpenTickets AS
SELECT t.TicketId, t.Title, t.Status, t.Priority, t.CreatedAt,
       c.Name AS CustomerName, c.Company,
       ISNULL(a.Name, 'Unassigned') AS AgentName,
       cat.Name AS Category
FROM Tickets t
JOIN Customers c   ON c.CustomerId = t.CustomerId
JOIN Categories cat ON cat.CategoryId = t.CategoryId
LEFT JOIN Agents a ON a.AgentId = t.AgentId
WHERE t.Status IN ('Open','InProgress');
GO

-- Stored procedure: open tickets for one customer
CREATE PROCEDURE usp_GetOpenTicketsByCustomer
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.TicketId, t.Title, t.Status, t.Priority, t.CreatedAt,
           ISNULL(a.Name, 'Unassigned') AS AgentName
    FROM Tickets t
    LEFT JOIN Agents a ON a.AgentId = t.AgentId
    WHERE t.CustomerId = @CustomerId
      AND t.Status IN ('Open','InProgress')
    ORDER BY t.CreatedAt DESC;
END;
GO

-- Stored procedure: assign a ticket and move it to InProgress
CREATE PROCEDURE usp_AssignTicket
    @TicketId INT,
    @AgentId  INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Tickets
    SET AgentId = @AgentId, Status = 'InProgress'
    WHERE TicketId = @TicketId AND Status = 'Open';
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

-- Stored procedure: change status (sets ResolvedAt when resolved/closed)
CREATE PROCEDURE usp_UpdateTicketStatus
    @TicketId INT,
    @Status   NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Tickets
    SET Status = @Status,
        ResolvedAt = CASE WHEN @Status IN ('Resolved','Closed')
                          THEN ISNULL(ResolvedAt, SYSUTCDATETIME())
                          ELSE NULL END
    WHERE TicketId = @TicketId;
    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

-- Stored procedure: add a comment, returns the new id
CREATE PROCEDURE usp_AddComment
    @TicketId INT,
    @AgentId  INT = NULL,
    @Body     NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO TicketComments (TicketId, AgentId, Body)
    VALUES (@TicketId, @AgentId, @Body);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS CommentId;
END;
GO

-- Report: tickets per status and average resolution time in hours
CREATE VIEW vw_TicketStats AS
SELECT Status,
       COUNT(*) AS TicketCount,
       AVG(CASE WHEN ResolvedAt IS NULL THEN NULL
                ELSE DATEDIFF(MINUTE, CreatedAt, ResolvedAt) / 60.0 END) AS AvgResolutionHours
FROM Tickets
GROUP BY Status;
GO
