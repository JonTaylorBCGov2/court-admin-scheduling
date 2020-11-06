using System;
using System.Collections.Generic;
using SS.Api.models.dto.generated;

namespace SS.Api.models.dto.generated
{
    public partial class DutyDto
    {
        public int Id { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public LocationDto Location { get; set; }
        public int LocationId { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public AssignmentDto Assignment { get; set; }
        public int? AssignmentId { get; set; }
        public ICollection<DutySlotDto> DutySlots { get; set; }
        public string Timezone { get; set; }
        public uint ConcurrencyToken { get; set; }
    }
}