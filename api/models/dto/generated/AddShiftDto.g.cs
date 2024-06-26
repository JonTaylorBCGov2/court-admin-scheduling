using System;

namespace CAS.API.models.dto.generated
{
    public partial class AddShiftDto
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public Guid CourtAdminId { get; set; }
        public int? AnticipatedAssignmentId { get; set; }
        public int LocationId { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public string Timezone { get; set; }
        public double OvertimeHours { get; set; }
        public string Comment { get; set; }
        public uint ConcurrencyToken { get; set; }
    }
}