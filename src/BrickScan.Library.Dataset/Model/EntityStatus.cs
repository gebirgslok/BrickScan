namespace BrickScan.Library.Dataset.Model
{
    public enum EntityStatus : byte
    {
        Classified = 0,
        Unclassified = 1,
        MarkedForDeletion = 2,
        MarkedForMerge = 3,
        Inherited = 4
    }
}