// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasOne(e => e.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ClassCourse)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.ClassCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // A student cannot be enrolled in the same class twice.
        builder.HasIndex(e => new { e.StudentId, e.ClassCourseId })
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_StudentId_ClassCourseId");

        builder.HasIndex(e => e.ClassCourseId)
            .HasDatabaseName("IX_Enrollments_ClassCourseId");
    }
}
