using System;
using CAS.API.models.dto.generated;

namespace CAS.API.models.dto.generated
{
    public partial class CourtAdminAwayLocationDto
    {
        public LocationDto Location { get; set; }
        public int? LocationId { get; set; }
        public int Id { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string ExpiryReason { get; set; }
        public Guid CourtAdminId { get; set; }
        public string Comment { get; set; }
        public string Timezone { get; set; }
        public uint ConcurrencyToken { get; set; }
    }
}