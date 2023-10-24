using System;

namespace CAS.API.models.dto.generated
{
    public partial class RegionDto
    {
        public int Id { get; set; }
        public int JustinId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public uint ConcurrencyToken { get; set; }
    }
}