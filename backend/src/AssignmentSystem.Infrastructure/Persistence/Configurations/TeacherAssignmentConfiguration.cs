// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasOne(t => t.Teacher)
            .WithMany(u => u.TeacherAssignments)
            .HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Subject)
            .WithMany(s => s.TeacherAssignments)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ClassCourse)
            .WithMany(c => c.TeacherAssignments)
            .HasForeignKey(t => t.ClassCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TeacherId, t.SubjectId, t.ClassCourseId })
            .IsUnique()
            .HasDatabaseName("IX_TeacherAssignments_Teacher_Subject_Class");

        // Business rule 3 looks up "does this teacher teach this subject in this class?"
        // on every assignment create, which is exactly this key order.
        builder.HasIndex(t => t.SubjectId)
            .HasDatabaseName("IX_TeacherAssignments_SubjectId");

        builder.HasIndex(t => t.ClassCourseId)
            .HasDatabaseName("IX_TeacherAssignments_ClassCourseId");
    }
}
