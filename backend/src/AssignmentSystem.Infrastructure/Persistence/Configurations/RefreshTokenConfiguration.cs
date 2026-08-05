// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(r => r.RevokedAt)
            .HasColumnType("timestamptz");

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Cascade here, unlike elsewhere: refresh tokens are worthless without their
        // user, and there is no audit reason to keep them behind.
        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Refresh presents a token and we look it up by hash, so this is the hot path.
        builder.HasIndex(r => r.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
