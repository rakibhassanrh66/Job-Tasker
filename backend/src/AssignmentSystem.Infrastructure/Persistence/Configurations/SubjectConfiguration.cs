// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Restrict, not Cascade: deleting a class that still has subjects should be
        // refused by the database and surfaced as a 409, not silently take rows with it.
        builder.HasOne(s => s.ClassCourse)
            .WithMany(c => c.Subjects)
            .HasForeignKey(s => s.ClassCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ClassCourseId)
            .HasDatabaseName("IX_Subjects_ClassCourseId");
    }
}
