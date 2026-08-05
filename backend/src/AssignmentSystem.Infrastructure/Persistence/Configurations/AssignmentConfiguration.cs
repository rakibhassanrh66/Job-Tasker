// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments", t =>
            t.HasCheckConstraint("CK_Assignments_MaxMarks_Positive", "\"MaxMarks\" > 0"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(a => a.Deadline)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(a => a.MaxMarks)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.AllowLateSubmission)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.AllowUpdateBeforeDeadline)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasOne(a => a.ClassCourse)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.ClassCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByTeacher)
            .WithMany(u => u.CreatedAssignments)
            .HasForeignKey(a => a.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // The student-facing query is always "Published assignments for the classes I am
        // enrolled in" (business rules 1 and 2), so index that pair together.
        builder.HasIndex(a => new { a.Status, a.ClassCourseId })
            .HasDatabaseName("IX_Assignments_Status_ClassCourseId");

        builder.HasIndex(a => a.SubjectId)
            .HasDatabaseName("IX_Assignments_SubjectId");

        builder.HasIndex(a => a.CreatedByTeacherId)
            .HasDatabaseName("IX_Assignments_CreatedByTeacherId");
    }
}
