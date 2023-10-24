﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CAS.DB.Configuration;
using CAS.DB.models.courtAdmin;

namespace CAS.DB.configuration
{
    public class CourtAdminActingRankConfiguration : BaseEntityConfiguration<CourtAdminActingRank>
    {
        public override void Configure(EntityTypeBuilder<CourtAdminActingRank> builder)
        {
            builder.HasIndex(b => new { b.StartDate, b.EndDate });

            base.Configure(builder);
        }
    }
}
