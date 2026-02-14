# Data Model: Human Voice Response Recognition (Feature 002)

**Date**: 2026-02-14 | **Feature**: Human Voice Response Recognition  
**Related Docs**: [spec.md](spec.md) | [plan.md](plan.md) | [domain-review.md](domain-review.md)

---

## Entity Relationship Diagram

```
┌─────────────────────────┐
│       Member (Existing) │
│─────────────────────────│
│ Id (PK)                 │
│ FirstName               │
│ LastName                │
│ PhoneNumber             │
│ Program                 │
│ Status                  │
│ CreatedAt, UpdatedAt    │
└──────────┬──────────────┘
           │
           │ 1:N
           │
┌──────────▼──────────────────────┐
│   CallSession (EXTENDED)        │
│─────────────────────────────────│
│ Id (PK)                         │
│ MemberId (FK)                   │
│ CallConnectionId (unique)       │
│ Status (enum)                   │
│ ResponseMode (NEW) ★            │ ← "dtmf-only" or "voice-only"
│ StartTime, EndTime              │
│ CreatedAt, UpdatedAt            │
│ ForeignKey: Member              │
└──────────┬──────────────────────┘
           │
           │
    ┌──────┴─────────┐
    │                │
    │ 1:N            │ 1:N
    │                │
┌───▼────────────────────┐   ┌──────────────────────────────┐
│  CallResponse (EXTEND) │   │ VoiceRecognitionResult (NEW)│
│────────────────────────│   │──────────────────────────────│
│ Id (PK)                │   │ Id (PK)                      │
│ CallSessionId (FK)     │   │ CallSessionId (FK)           │
│ QuestionNumber        │   │ QuestionNumber               │
│ QuestionText           │   │ AudioDurationMs              │
│ ResponseValue         │   │ TranscribedText              │
│ ResponseSource (NEW) ★ │   │ ConfidenceScore (0-1)        │
│ RespondedAt            │   │ Intent (yes/no/digit/skip)   │
└────────────────────────┘   │ ProcessedAt                  │
                              │ ForeignKey: CallSession      │
                              └──────────────────────────────┘
```

---

## Entity Definitions

### CallSession (EXTENDING - Existing Entity)

**New Schema** (changes marked with ★):

```csharp
public class CallSession
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Member being called
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Azure Communication Services call connection identifier
    /// </summary>
    public string? CallConnectionId { get; set; }

    /// <summary>
    /// Current call status
    /// </summary>
    public CallStatus Status { get; set; } = CallStatus.Initiated;

    /// <summary>
    /// ★ NEW: Campaign response mode (immutable at creation)
    /// Value: "dtmf-only" (DTMF keypad only) or "voice-only" (voice recognition only)
    /// Cannot be changed after campaign creation
    /// </summary>
    public string ResponseMode { get; set; } = "dtmf-only";

    /// <summary>
    /// When call initiation was requested
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When call ended (null if still in progress)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last status update timestamp (auto-updated by trigger)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Member Member { get; set; } = null!;
    public virtual ICollection<CallResponse> Responses { get; set; } = new List<CallResponse>();
    public virtual ICollection<VoiceRecognitionResult> VoiceResults { get; set; } = new List<VoiceRecognitionResult>();
}
```

**Database Table Schema** (SQL):

```sql
ALTER TABLE [dbo].[CallSessions]
ADD [ResponseMode] NVARCHAR(20) NOT NULL DEFAULT 'dtmf-only';

ALTER TABLE [dbo].[CallSessions]
ADD CONSTRAINT [CHK_CallSession_ResponseMode] 
CHECK ([ResponseMode] IN ('dtmf-only', 'voice-only'));
```

**Validation Rules**:
- `ResponseMode` must be 'dtmf-only' or 'voice-only' (enum-like check constraint)
- `ResponseMode` is immutable after campaign creation (set once, never updated)
- All other existing CallSession rules unchanged

---

### CallResponse (EXTENDING - Existing Entity)

**New Schema** (changes marked with ★):

```csharp
public class CallResponse
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to CallSession
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Question sequence number (1-3 for healthcare questions)
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Full question text as asked to member
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Response value: 1=yes, 2=no (for DTMF) or intent text (for voice)
    /// Can store int or string depending on ResponseSource
    /// </summary>
    public string ResponseValue { get; set; } = string.Empty;

    /// <summary>
    /// ★ NEW: Source of response (DTMF or voice recognition)
    /// Value: "dtmf" (keypad tone) or "voice" (transcribed speech)
    /// Used to determine how to interpret ResponseValue
    /// </summary>
    public string ResponseSource { get; set; } = "dtmf";

    /// <summary>
    /// When response was captured
    /// </summary>
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual CallSession CallSession { get; set; } = null!;
}
```

**Database Table Schema** (SQL):

```sql
ALTER TABLE [dbo].[CallResponses]
ADD [ResponseSource] NVARCHAR(20) NOT NULL DEFAULT 'dtmf';

ALTER TABLE [dbo].[CallResponses]
ADD CONSTRAINT [CHK_CallResponse_ResponseSource] 
CHECK ([ResponseSource] IN ('dtmf', 'voice'));

-- Composite index for efficient lookup by call + source
CREATE NONCLUSTERED INDEX [IX_CallResponses_CallSession_Source]
    ON [dbo].[CallResponses]([CallSessionId] ASC, [ResponseSource] ASC);
```

**Validation Rules**:
- `ResponseSource` must be 'dtmf' or 'voice'
- `ResponseValue` remains flexible to support both int (DTMF) and string (voice intent)
- All other existing validation rules unchanged

---

### VoiceRecognitionResult (NEW Entity)

**Description**: Stores metadata from Azure Speech Services voice recognition for audit trail and confidence gating.

```csharp
public class VoiceRecognitionResult
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to CallSession
    /// </summary>
    public int CallSessionId { get; set; }

    /// <summary>
    /// Question number (1-3) being answered with voice
    /// </summary>
    public int QuestionNumber { get; set; }

    /// <summary>
    /// Audio clip duration in milliseconds
    /// </summary>
    public decimal AudioDurationMs { get; set; }

    /// <summary>
    /// Raw transcription from Azure Speech Services
    /// Example: "yes", "no", "one", "number one", etc.
    /// </summary>
    public string TranscribedText { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// Azure Speech Services returns: 0.0 (no confidence) to 1.0 (perfect confidence)
    /// MVP threshold: >0.75 (75%)
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Interpreted intent from transcription
    /// Values: "yes", "no", "1", "2", "skip"
    /// Populated by VoiceResponseInterpreter after transcription
    /// </summary>
    public string Intent { get; set; } = string.Empty;

    /// <summary>
    /// When this voice result was processed by Azure Speech Services
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual CallSession CallSession { get; set; } = null!;
}
```

**Database Table Schema** (SQL):

```sql
CREATE TABLE [dbo].[VoiceRecognitionResults]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [CallSessionId] INT NOT NULL,
    [QuestionNumber] INT NOT NULL,
    [AudioDurationMs] DECIMAL(10,3) NOT NULL DEFAULT 0,
    [TranscribedText] NVARCHAR(500) NOT NULL DEFAULT '',
    [ConfidenceScore] DECIMAL(3,2) NOT NULL DEFAULT 0,
    [Intent] NVARCHAR(50) NOT NULL DEFAULT '',
    [ProcessedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_VoiceRecognitionResults] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VoiceRecognitionResult_CallSession] FOREIGN KEY ([CallSessionId]) 
        REFERENCES [dbo].[CallSessions]([Id]),
    CONSTRAINT [CHK_VoiceRecognitionResult_Confidence] 
        CHECK ([ConfidenceScore] >= 0.0 AND [ConfidenceScore] <= 1.0),
    CONSTRAINT [CHK_VoiceRecognitionResult_Intent] 
        CHECK ([Intent] IN ('yes', 'no', '1', '2', 'skip'))
);

-- Index for looking up all voice results for a call
CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_CallSessionId]
    ON [dbo].[VoiceRecognitionResults]([CallSessionId] ASC);

-- Index for filtering by confidence score (performance gating)
CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_Confidence]
    ON [dbo].[VoiceRecognitionResults]([ConfidenceScore] DESC);

-- Composite index for call + question lookup
CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_Question]
    ON [dbo].[VoiceRecognitionResults]([CallSessionId] ASC, [QuestionNumber] ASC);
```

**Validation Rules**:
- `ConfidenceScore` must be between 0.0 and 1.0 (percentage as decimal)
- `Intent` must be one of: 'yes', 'no', '1', '2', 'skip'
- `QuestionNumber` should match corresponding CallResponse (referential constraint via FK)
- `TranscribedText` stores raw text for audit trail

---

## EF Core Configuration

### CallistraAgentDbContext (EXTENDING)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Existing configuration for Member, CallSession (without ResponseMode), 
    // CallResponse (without ResponseSource)...

    // EXTEND: CallSession configuration with ResponseMode
    modelBuilder.Entity<CallSession>(entity =>
    {
        // Existing: ToTable, HasKey, Property configurations...
        
        // NEW: Configure ResponseMode property
        entity.Property(e => e.ResponseMode)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("dtmf-only");

        // NEW: Add check constraint via migration or fluent API comment
        // Note: Check constraints applied via SQL migration for database-level enforcement
    });

    // EXTEND: CallResponse configuration with ResponseSource
    modelBuilder.Entity<CallResponse>(entity =>
    {
        // Existing: ToTable, HasKey, Property configurations...
        
        // NEW: Configure ResponseSource property
        entity.Property(e => e.ResponseSource)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("dtmf");

        // NEW: Add composite index for efficient filtering
        entity.HasIndex(e => new { e.CallSessionId, e.ResponseSource })
            .HasDatabaseName("IX_CallResponse_CallSession_Source");
    });

    // NEW: Configure VoiceRecognitionResult entity
    modelBuilder.Entity<VoiceRecognitionResult>(entity =>
    {
        entity.ToTable("VoiceRecognitionResults", tb => tb.HasTrigger("UpdateTimestamp"));
        
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.CallSessionId).IsRequired();
        entity.Property(e => e.QuestionNumber).IsRequired();
        entity.Property(e => e.AudioDurationMs).HasPrecision(10, 3);
        entity.Property(e => e.TranscribedText).HasMaxLength(500).IsRequired();
        entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2).IsRequired();
        entity.Property(e => e.Intent).HasMaxLength(50).IsRequired();
        entity.Property(e => e.ProcessedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        entity.HasOne(e => e.CallSession)
            .WithMany(cs => cs.VoiceResults)
            .HasForeignKey(e => e.CallSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(e => e.CallSessionId)
            .HasDatabaseName("IX_VoiceRecognitionResults_CallSessionId");
        
        entity.HasIndex(e => e.ConfidenceScore)
            .IsDescending()
            .HasDatabaseName("IX_VoiceRecognitionResults_Confidence");

        entity.HasIndex(e => new { e.CallSessionId, e.QuestionNumber })
            .HasDatabaseName("IX_VoiceRecognitionResults_Question");
    });
}

// NEW: Register DbSet
public DbSet<VoiceRecognitionResult> VoiceRecognitionResults => Set<VoiceRecognitionResult>();
```

---

## Database Migration Script

**Filename**: `Migrations/002_AddVoiceSupport.sql`

```sql
-- =============================================================================
-- Migration: 002_AddVoiceSupport
-- Purpose: Add voice recognition support to CallistraAgent database
-- Date: 2026-02-14
-- =============================================================================

USE CallistraAgent;
GO

-- =============================================================================
-- Step 1: Extend CallSessions table
-- =============================================================================

PRINT 'Adding ResponseMode column to CallSessions table...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CallSessions' AND COLUMN_NAME = 'ResponseMode')
BEGIN
    ALTER TABLE [dbo].[CallSessions]
    ADD [ResponseMode] NVARCHAR(20) NOT NULL DEFAULT 'dtmf-only';
    
    PRINT 'Column [ResponseMode] added successfully.';
END
ELSE
BEGIN
    PRINT 'Column [ResponseMode] already exists. Skipping.';
END
GO

-- Add check constraint if not exists
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_CallSession_ResponseMode')
BEGIN
    ALTER TABLE [dbo].[CallSessions]
    ADD CONSTRAINT [CHK_CallSession_ResponseMode] 
    CHECK ([ResponseMode] IN ('dtmf-only', 'voice-only'));
    
    PRINT 'Check constraint [CHK_CallSession_ResponseMode] added successfully.';
END
GO

-- =============================================================================
-- Step 2: Extend CallResponses table
-- =============================================================================

PRINT 'Adding ResponseSource column to CallResponses table...';

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CallResponses' AND COLUMN_NAME = 'ResponseSource')
BEGIN
    ALTER TABLE [dbo].[CallResponses]
    ADD [ResponseSource] NVARCHAR(20) NOT NULL DEFAULT 'dtmf';
    
    PRINT 'Column [ResponseSource] added successfully.';
END
ELSE
BEGIN
    PRINT 'Column [ResponseSource] already exists. Skipping.';
END
GO

-- Add check constraint if not exists
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_CallResponse_ResponseSource')
BEGIN
    ALTER TABLE [dbo].[CallResponses]
    ADD CONSTRAINT [CHK_CallResponse_ResponseSource] 
    CHECK ([ResponseSource] IN ('dtmf', 'voice'));
    
    PRINT 'Check constraint [CHK_CallResponse_ResponseSource] added successfully.';
END
GO

-- Create composite index for ResponseSource filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CallResponses_CallSession_Source')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CallResponses_CallSession_Source]
        ON [dbo].[CallResponses]([CallSessionId] ASC, [ResponseSource] ASC);
    
    PRINT 'Index [IX_CallResponses_CallSession_Source] created successfully.';
END
GO

-- =============================================================================
-- Step 3: Create VoiceRecognitionResults table
-- =============================================================================

PRINT 'Creating VoiceRecognitionResults table...';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'VoiceRecognitionResults' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
    CREATE TABLE [dbo].[VoiceRecognitionResults]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CallSessionId] INT NOT NULL,
        [QuestionNumber] INT NOT NULL,
        [AudioDurationMs] DECIMAL(10,3) NOT NULL DEFAULT 0,
        [TranscribedText] NVARCHAR(500) NOT NULL DEFAULT '',
        [ConfidenceScore] DECIMAL(3,2) NOT NULL DEFAULT 0,
        [Intent] NVARCHAR(50) NOT NULL DEFAULT '',
        [ProcessedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [PK_VoiceRecognitionResults] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_VoiceRecognitionResult_CallSession] FOREIGN KEY ([CallSessionId]) 
            REFERENCES [dbo].[CallSessions]([Id]),
        CONSTRAINT [CHK_VoiceRecognitionResult_Confidence] 
            CHECK ([ConfidenceScore] >= 0.0 AND [ConfidenceScore] <= 1.0),
        CONSTRAINT [CHK_VoiceRecognitionResult_Intent] 
            CHECK ([Intent] IN ('yes', 'no', '1', '2', 'skip'))
    );

    PRINT 'Table [dbo].[VoiceRecognitionResults] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[VoiceRecognitionResults] already exists. Skipping.';
END
GO

-- =============================================================================
-- Step 4: Create indexes on VoiceRecognitionResults
-- =============================================================================

PRINT 'Creating indexes on VoiceRecognitionResults table...';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VoiceRecognitionResults_CallSessionId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_CallSessionId]
        ON [dbo].[VoiceRecognitionResults]([CallSessionId] ASC);
    
    PRINT 'Index [IX_VoiceRecognitionResults_CallSessionId] created successfully.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VoiceRecognitionResults_Confidence')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_Confidence]
        ON [dbo].[VoiceRecognitionResults]([ConfidenceScore] DESC);
    
    PRINT 'Index [IX_VoiceRecognitionResults_Confidence] created successfully.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VoiceRecognitionResults_Question')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VoiceRecognitionResults_Question]
        ON [dbo].[VoiceRecognitionResults]([CallSessionId] ASC, [QuestionNumber] ASC);
    
    PRINT 'Index [IX_VoiceRecognitionResults_Question] created successfully.';
END
GO

-- =============================================================================
-- Verification
-- =============================================================================

PRINT '';
PRINT '=============================================================================';
PRINT 'Migration 002_AddVoiceSupport Complete!';
PRINT '=============================================================================';
PRINT '';

PRINT 'Verifying schema changes:';
SELECT
    t.name AS TableName,
    COUNT(c.column_id) AS ColumnCount
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
    AND t.name IN ('CallSessions', 'CallResponses', 'VoiceRecognitionResults')
GROUP BY t.name, t.object_id
ORDER BY t.name;

PRINT '';
PRINT 'New columns:';
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('CallSessions', 'CallResponses', 'VoiceRecognitionResults')
    AND COLUMN_NAME IN ('ResponseMode', 'ResponseSource', 'Intent', 'ConfidenceScore')
ORDER BY TABLE_NAME, COLUMN_NAME;

GO
```

---

## Validation Rules & Constraints

### CallSession

| Rule | Level | Enforcement | Impact |
|------|-------|-------------|--------|
| ResponseMode ∈ {dtmf-only, voice-only} | Database | CHECK constraint | Invalid modes rejected |
| ResponseMode immutable | Application | Manual check in service | Updated via warning in logs |
| One active call per member | Database | Business logic | Prevents duplicate calls |

### CallResponse

| Rule | Level | Enforcement | Impact |
|------|-------|-------------|--------|
| ResponseSource ∈ {dtmf, voice} | Database | CHECK constraint | Invalid sources rejected |
| QuestionNumber ∈ [1-3] | Database | CHECK constraint (existing) | Invalid questions rejected |
| ResponseValue matches ResponseSource | Application | Validation in CallService | Type safety during save |
| Unique (CallSessionId, QuestionNumber) | Database | Unique constraint (existing) | One answer per question |

### VoiceRecognitionResult

| Rule | Level | Enforcement | Impact |
|------|-------|-------------|--------|
| ConfidenceScore ∈ [0.0, 1.0] | Database | CHECK constraint | Out-of-range scores rejected |
| Intent ∈ {yes, no, 1, 2, skip} | Database | CHECK constraint | Invalid intents rejected |
| QuestionNumber ∈ [1-3] | Application | Validation in service | Domain consistency |

---

## Backward Compatibility

✅ **All changes are backward-compatible**:

1. **CallSession.ResponseMode**: New column with DEFAULT 'dtmf-only'
   - Existing DTMF campaigns automatically default to DTMF-only mode
   - No data migration required

2. **CallResponse.ResponseSource**: New column with DEFAULT 'dtmf'
   - Existing DTMF responses automatically marked as 'dtmf' source
   - Queries filtering by source work correctly

3. **VoiceRecognitionResults**: New table (not referenced by existing code)
   - Additive only; no impact on existing queries
   - Optional; voice responses can be logged without this table

**Migration Strategy**: Run 002_AddVoiceSupport.sql after deploying code that uses these new columns/tables.

---

## Next Steps

- **Phase 1 (Design)**: Complete ✅
- **Phase 2 (Contracts)**: Generate OpenAPI specs for voice recognition endpoints
- **Phase 3 (Quickstart)**: Document database setup and migration procedures
- **Phase 3+ (Tasks)**: Break into implementation tasks per user story

