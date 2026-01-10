using Microsoft.EntityFrameworkCore;
using CallistraAgent.Functions.Models;

namespace CallistraAgent.Functions.Data;

/// <summary>
/// Entity Framework Core database context for Callistra Agent
/// </summary>
public class CallistraAgentDbContext : DbContext
{
    public CallistraAgentDbContext(DbContextOptions<CallistraAgentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Members dataset
    /// </summary>
    public DbSet<Member> Members => Set<Member>();

    /// <summary>
    /// Call sessions dataset
    /// </summary>
    public DbSet<CallSession> CallSessions => Set<CallSession>();

    /// <summary>
    /// Call responses dataset
    /// </summary>
    public DbSet<CallResponse> CallResponses => Set<CallResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Member entity
        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("Members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Program).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Unique constraint on phone number
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasDatabaseName("UQ_Member_PhoneNumber");

            // Index for status filtering
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Members_Status");
        });

        // Configure CallSession entity
        modelBuilder.Entity<CallSession>(entity =>
        {
            entity.ToTable("CallSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MemberId).IsRequired();
            entity.Property(e => e.CallConnectionId).HasMaxLength(100);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue(CallStatus.Initiated)
                .HasConversion<string>();
            entity.Property(e => e.StartTime).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key to Member
            entity.HasOne(e => e.Member)
                .WithMany(m => m.CallSessions)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint on CallConnectionId
            entity.HasIndex(e => e.CallConnectionId)
                .IsUnique()
                .HasDatabaseName("UQ_CallSession_CallConnectionId")
                .HasFilter("[CallConnectionId] IS NOT NULL");

            // Indexes
            entity.HasIndex(e => e.MemberId).HasDatabaseName("IX_CallSessions_MemberId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_CallSessions_Status");
            entity.HasIndex(e => e.CallConnectionId).HasDatabaseName("IX_CallSessions_CallConnectionId");
            entity.HasIndex(e => e.StartTime).IsDescending().HasDatabaseName("IX_CallSessions_StartTime");
        });

        // Configure CallResponse entity
        modelBuilder.Entity<CallResponse>(entity =>
        {
            entity.ToTable("CallResponses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CallSessionId).IsRequired();
            entity.Property(e => e.QuestionNumber).IsRequired();
            entity.Property(e => e.QuestionText).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ResponseValue).IsRequired();
            entity.Property(e => e.RespondedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Foreign key to CallSession
            entity.HasOne(e => e.CallSession)
                .WithMany(cs => cs.Responses)
                .HasForeignKey(e => e.CallSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for call session lookup
            entity.HasIndex(e => e.CallSessionId).HasDatabaseName("IX_CallResponses_CallSessionId");

            // Unique composite index for one answer per question per call
            entity.HasIndex(e => new { e.CallSessionId, e.QuestionNumber })
                .IsUnique()
                .HasDatabaseName("IX_CallResponses_CallSession_Question");
        });
    }
}
