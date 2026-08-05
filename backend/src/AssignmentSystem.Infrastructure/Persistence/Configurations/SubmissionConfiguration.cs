// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        // Only the lower bound is expressible here. The upper bound is Assignment.MaxMarks,
        // which lives on a different table, and a PostgreSQL CHECK cannot reach across
        // rows — so the service layer owns that half and returns 422. See docs/ERD.md.
        builder.ToTable("Submissions", t =>
            t.HasCheckConstraint("CK_Submissions_Marks_NonNegative", "\"Marks\" IS NULL OR \"Marks\" >= 0"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AnswerText)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(s => s.AttachmentUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.SubmittedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(s => s.GradedAt)
            .HasColumnType("timestamptz");

        builder.Property(s => s.Feedback)
            .HasMaxLength(5000);

        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two foreign keys point at Users from this table, so both ends are stated
        // explicitly — otherwise EF cannot tell which navigation belongs to which key.
        builder.HasOne(s => s.Student)
            .WithMany(u => u.Submissions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.GradedByTeacher)
            .WithMany()
            .HasForeignKey(s => s.GradedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // fp:902f9ccf28febc57
        //
        // Business rule 6, enforced by the database rather than by the service alone.
        // A check-then-insert in application code leaves a window where two concurrent
        // submissions both see "none exists" and both insert; this index closes it.
        builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_Submissions_AssignmentId_StudentId");

        builder.HasIndex(s => s.AssignmentId)
            .HasDatabaseName("IX_Submissions_AssignmentId");

        builder.HasIndex(s => s.StudentId)
            .HasDatabaseName("IX_Submissions_StudentId");

        builder.HasIndex(s => s.GradedByTeacherId)
            .HasDatabaseName("IX_Submissions_GradedByTeacherId");
    }
}
