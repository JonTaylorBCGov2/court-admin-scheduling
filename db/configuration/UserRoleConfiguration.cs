﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CAS.DB.Configuration;
using CAS.DB.models.auth;

namespace CAS.DB.configuration
{
    public class UserRoleConfiguration : BaseEntityConfiguration<UserRole>
    {
        public override void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.Property(b => b.Id).HasIdentityOptions(startValue: 5000);

            builder.HasIndex(lc => new { lc.RoleId, lc.UserId }).IsUnique();

            builder.HasOne(m => m.User).WithMany(m => m.UserRoles).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(m => m.Role).WithMany(m => m.UserRoles).HasForeignKey(m => m.RoleId).OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}
