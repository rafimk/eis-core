namespace EIS.Api.Application.Constants;

public class EISEventTypes
{
    public static class ItemManagement
    {
        public static readonly string ITEM_CREATED = "ITEM_CREATED";
        public static readonly string ITEM_UPDATED = "ITEM_UPDATED";
        public static readonly string ITEM_DELETE = "ITEM_DELETE";
    }

    public static class MDM
    {
        public static readonly string MDM_CREATED = "MDM_CREATED";
        public static readonly string MDM_UPDATED = "ITEM_UPDATED";
        public static readonly string MDM_DELETE = "ITEM_DELETE";
    }
}