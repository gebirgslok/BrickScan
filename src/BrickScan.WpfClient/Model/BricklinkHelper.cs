namespace BrickScan.WpfClient.Model
{
    internal static class BricklinkHelper
    {
        private const string BRICKLINK_PART_COLOR_URL_TEMPLATE =
            "https://www.bricklink.com/v2/catalog/catalogitem.page?P={0}#T=S&C={1}";

        private const string BRICKLINK_PART_URL_TEMPLATE =
            "https://www.bricklink.com/v2/catalog/catalogitem.page?P={0}#T=S";
        public static string GeneratePartUrl(string partNo, int? colorId = null)
        {
            if (!colorId.HasValue)
            {
                return string.Format(BRICKLINK_PART_URL_TEMPLATE, partNo);
            }

            var url = string.Format(BRICKLINK_PART_COLOR_URL_TEMPLATE, partNo, colorId.Value);
            return url;
        }
    }
}
