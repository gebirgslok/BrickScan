using System;
using System.Collections.Generic;

namespace BrickScan.Library.Dataset.Model
{
    public class DatasetClass : DatasetEntity
    {
        public EntityStatus Status { get; set; }

        public string CreatedBy { get; set; } = null!;

        public DateTime CreatedOn { get; set; }

        public List<DatasetImage> TrainingImages { get; set; } = new List<DatasetImage>();

        public List<DatasetImage> DisplayImages { get; set; } = new List<DatasetImage>();

        public List<DatasetItem> DatasetItems { get; set; } = new List<DatasetItem>();
    }
}
