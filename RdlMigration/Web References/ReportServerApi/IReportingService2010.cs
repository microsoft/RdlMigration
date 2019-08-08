namespace RdlMigration.ReportServerApi
{
    public interface IReportingService2010
    {
        DataSourceDefinition GetDataSourceContents(string DataSource);
        byte[] GetItemDefinition(string ItemPath);
        ItemReferenceData[] GetItemReferences(string ItemPath, string ReferenceItemType);
        string GetItemType(string ItemPath);
        CatalogItem[] ListChildren(string ItemPath, bool Recursive);
    }
}