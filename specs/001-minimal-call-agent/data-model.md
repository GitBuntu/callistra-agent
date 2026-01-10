# Data Model

**Feature**: 001-minimal-call-agent  
**Date**: January 10, 2026  
**Status**: ✅ Complete

## Entity Relationship Diagram

```
┌─────────────────────┐
│      Member         │
│─────────────────────│
│ Id (PK)             │
│ FirstName           │
│ LastName            │
│ PhoneNumber         │
│ Program             │
│ Status              │
│ CreatedAt           │
│ UpdatedAt           │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐
│   CallSession       │
│─────────────────────│
│ Id (PK)             │
│ MemberId (FK)       │
│ CallConnectionId    │
│ Status              │
│ StartTime           │
│ EndTime             │
│ CreatedAt           │
│ UpdatedAt           │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────┐
│   CallResponse      │
│─────────────────────│
│ Id (PK)             │
│ CallSessionId (FK)  │
│ QuestionNumber      │
│ QuestionText        │
│ ResponseValue       │
│ RespondedAt         │
└─────────────────────┘
```

---

## Entities

### Member

Represents a healthcare program enrollee who can receive outreach calls.

**Fields**:
- `Id` (int, PK, identity) - Unique identifier
- `FirstName` (nvarchar(100), not null) - Member's first name
- `LastName` (nvarchar(100), not null) - Member's last name
- `PhoneNumber` (nvarchar(20), not null) - E.164 format phone number (e.g., +18005551234)
- `Program` (nvarchar(100), not null) - Healthcare program name (e.g., "Diabetes Care", "Wellness")
- `Status` (nvarchar(50), not null) - Enrollment status (Active, Pending, Inactive)
- `CreatedAt` (datetime2, not null, default: GETUTCDATE()) - Record creation timestamp
- `UpdatedAt` (datetime2, not null, default: GETUTCDATE()) - Last update timestamp

**Indexes**:
- Unique index on `PhoneNumber` (prevent duplicate enrollments)
- Index on `Status` (filter active members)

**Validation Rules**:
- `PhoneNumber` must match E.164 format regex: `^\+[1-9]\d{1,14}$`
- `Status` must be one of: Active, Pending, Inactive
- `FirstName` and `LastName` cannot be empty strings

**Business Rules**:
- Members can only be called if `Status = 'Active'`
- `UpdatedAt` automatically updated on any field change

---

### CallSession

Represents a single outbound call attempt to a member.

**Fields**:
- `Id` (int, PK, identity) - Unique identifier
- `MemberId` (int, FK → Member.Id, not null) - Reference to member being called
- `CallConnectionId` (nvarchar(100), nullable) - Azure Communication Services call connection identifier (null until call initiated)
- `Status` (nvarchar(50), not null) - Current call status (see enum below)
- `StartTime` (datetime2, not null, default: GETUTCDATE()) - When call initiation was requested
- `EndTime` (datetime2, nullable) - When call ended (null if still in progress)
- `CreatedAt` (datetime2, not null, default: GETUTCDATE()) - Record creation timestamp
- `UpdatedAt` (datetime2, not null, default: GETUTCDATE()) - Last status update timestamp

**Status Enum Values**:
- `Initiated` - Call request sent to Azure Communication Services
- `Ringing` - Phone is ringing (CallConnecting event received)
- `Connected` - Member answered (CallConnected event received)
- `Completed` - All questions answered successfully
- `Disconnected` - Member hung up before completing all questions
- `Failed` - System error or call setup failure
- `NoAnswer` - 30-second timeout expired without answer
- `VoicemailMessage` - Voicemail detected, generic callback message played

**Indexes**:
- Index on `MemberId` (lookup all calls for a member)
- Index on `Status` (filter in-progress calls)
- Index on `CallConnectionId` (webhook event routing)
- Index on `StartTime DESC` (recent calls first)

**Validation Rules**:
- `Status` must be one of the enum values
- `EndTime` must be >= `StartTime` if not null
- `CallConnectionId` must be unique if not null

**Business Rules**:
- Only one `Initiated` or `Connected` call per member at a time (prevent duplicate calls)
- `EndTime` is set when status changes to Completed, Disconnected, Failed, NoAnswer, or VoicemailMessage
- Status transitions follow state machine:
  - `Initiated → Ringing → Connected → [Completed | Disconnected | VoicemailMessage]`
  - `Initiated → Failed` (call setup error)
  - `Ringing → NoAnswer` (30s timeout)
  - `Connected → VoicemailMessage` (no DTMF response to person-detection prompt)

---

### CallResponse

Represents a member's answer to a specific question during a call.

**Fields**:
- `Id` (int, PK, identity) - Unique identifier
- `CallSessionId` (int, FK → CallSession.Id, not null) - Reference to call session
- `QuestionNumber` (int, not null) - Question sequence number (1, 2, 3)
- `QuestionText` (nvarchar(500), not null) - Full question text as spoken to member
- `ResponseValue` (int, not null) - DTMF response (1 = yes, 2 = no)
- `RespondedAt` (datetime2, not null, default: GETUTCDATE()) - When response was captured

**Indexes**:
- Index on `CallSessionId` (lookup all responses for a call)
- Composite index on `(CallSessionId, QuestionNumber)` (ensure unique answers per question)

**Validation Rules**:
- `QuestionNumber` must be between 1 and 3
- `ResponseValue` must be 1 or 2
- `QuestionText` cannot be empty

**Business Rules**:
- Each `(CallSessionId, QuestionNumber)` pair must be unique (prevent duplicate responses)
- Responses are immutable once recorded (no updates allowed)
- If member hangs up mid-call, only partial responses are saved

---

## Database Schema (SQL)

### CREATE TABLE Statements

```sql
-- Members table
CREATE TABLE Members (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Program NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CHK_Member_Status CHECK (Status IN ('Active', 'Pending', 'Inactive')),
    CONSTRAINT UQ_Member_PhoneNumber UNIQUE (PhoneNumber)
);

CREATE INDEX IX_Members_Status ON Members(Status);

-- CallSessions table
CREATE TABLE CallSessions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT NOT NULL,
    CallConnectionId NVARCHAR(100) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Initiated',
    StartTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndTime DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_CallSession_Member FOREIGN KEY (MemberId) REFERENCES Members(Id),
    CONSTRAINT CHK_CallSession_Status CHECK (Status IN ('Initiated', 'Ringing', 'Connected', 'Completed', 'Disconnected', 'Failed', 'NoAnswer', 'VoicemailMessage')),
    CONSTRAINT CHK_CallSession_EndTime CHECK (EndTime IS NULL OR EndTime >= StartTime),
    CONSTRAINT UQ_CallSession_CallConnectionId UNIQUE (CallConnectionId)
);

CREATE INDEX IX_CallSessions_MemberId ON CallSessions(MemberId);
CREATE INDEX IX_CallSessions_Status ON CallSessions(Status);
CREATE INDEX IX_CallSessions_CallConnectionId ON CallSessions(CallConnectionId);
CREATE INDEX IX_CallSessions_StartTime ON CallSessions(StartTime DESC);

-- CallResponses table
CREATE TABLE CallResponses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CallSessionId INT NOT NULL,
    QuestionNumber INT NOT NULL,
    QuestionText NVARCHAR(500) NOT NULL,
    ResponseValue INT NOT NULL,
    RespondedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_CallResponse_CallSession FOREIGN KEY (CallSessionId) REFERENCES CallSessions(Id),
    CONSTRAINT CHK_CallResponse_QuestionNumber CHECK (QuestionNumber BETWEEN 1 AND 3),
    CONSTRAINT CHK_CallResponse_ResponseValue CHECK (ResponseValue IN (1, 2)),
    CONSTRAINT UQ_CallResponse_Question UNIQUE (CallSessionId, QuestionNumber)
);

CREATE INDEX IX_CallResponses_CallSessionId ON CallResponses(CallSessionId);
```

---

## Entity Framework Core Entities

### Member.cs

```csharp
public class Member
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<CallSession> CallSessions { get; set; } = new List<CallSession>();
}
```

### CallSession.cs

```csharp
public class CallSession
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? CallConnectionId { get; set; }
    public string Status { get; set; } = "Initiated";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Member Member { get; set; } = null!;
    public ICollection<CallResponse> CallResponses { get; set; } = new List<CallResponse>();
}
```

### CallResponse.cs

```csharp
public class CallResponse
{
    public int Id { get; set; }
    public int CallSessionId { get; set; }
    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int ResponseValue { get; set; }
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public CallSession CallSession { get; set; } = null!;
}
```

---

## DbContext Configuration

### CallAgentDbContext.cs

```csharp
public class CallAgentDbContext : DbContext
{
    public CallAgentDbContext(DbContextOptions<CallAgentDbContext> options) : base(options) { }
    
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<CallSession> CallSessions { get; set; } = null!;
    public DbSet<CallResponse> CallResponses { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Member configuration
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Program).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.Status);
        });
        
        // CallSession configuration
        modelBuilder.Entity<CallSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CallConnectionId).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.MemberId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CallConnectionId).IsUnique();
            entity.HasIndex(e => e.StartTime).IsDescending();
            
            entity.HasOne(e => e.Member)
                  .WithMany(m => m.CallSessions)
                  .HasForeignKey(e => e.MemberId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        
        // CallResponse configuration
        modelBuilder.Entity<CallResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionText).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.CallSessionId);
            entity.HasIndex(e => new { e.CallSessionId, e.QuestionNumber }).IsUnique();
            
            entity.HasOne(e => e.CallSession)
                  .WithMany(cs => cs.CallResponses)
                  .HasForeignKey(e => e.CallSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

---

## Sample Data (for testing)

```sql
-- Insert test members
INSERT INTO Members (FirstName, LastName, PhoneNumber, Program, Status)
VALUES 
    ('John', 'Doe', '+18005551234', 'Diabetes Care', 'Active'),
    ('Jane', 'Smith', '+18005555678', 'Wellness Program', 'Active'),
    ('Bob', 'Johnson', '+18005559012', 'Cardiac Care', 'Pending');
```

---

## Data Model Complete ✅

All entities defined with validation rules and relationships. Proceed to **Contract Generation**.
