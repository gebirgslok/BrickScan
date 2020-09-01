using System;

namespace BrickScan.Library.Dataset.Model
{
    public class DatasetImage : DatasetEntity
    {
        public string Url { get; set; } = null!;

        public EntityStatus Status { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
