// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

namespace RdlMigration
{
    public class ElementNameConstants
    {
        public const string ReportDesignerNameSpace = "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";
        public const string ConversionLogFileName = "./ConversionLog.txt";
        public const string ReportFileExtension = ".rdl";
        public const string DataSetFileExtension = ".rsd";
        public const string DataSourceFileExtension = ".rds";

        public static readonly string[] SQLAzureSuffixes = { ".database.windows.net", ".database.chinacloudapi.cn", ".database.cloudapi.de", ".database.usgovcloudapi.net" };

        /// <summary>
        /// Constants related to Soap API
        /// </summary>
        public class SoapApiConstants
        {
            public const string SOAPApiExtension = "/ReportService2010.asmx?wsdl";
            public const string Report = "Report";
            public const string Folder = "Folder";
        }
        /// <summary>
        /// Constants related to Data Source
        /// </summary>
        public class DataSourceConstants
        {
            public const string DataSources = "DataSources";
            public const string DataSource = "DataSource";

            public const string DataSourceReference = "DataSourceReference";
            public const string ConnectionProperties = "ConnectionProperties";
            public const string DataProvider = "DataProvider";
            public const string ConnectString = "ConnectString";
            public const string SQLAzure = "SQLAzure";
            public const string SQL = "SQL";
            public const string IntegratedSecurity = "IntegratedSecurity";
            public const string DataSourceID = "DataSourceID";

            public const string DataSourceDefinition = "DataSourceDefinition";
            public const string Extension = "Extension";
            public const string UseOriginalConnectString = "UseOriginalConnectString";
            public const string OriginalConnectStringExpressionBased = "OriginalConnectStringExpressionBased";
            public const string CredentialRetrieval = "CredentialRetrieval";
            public const string WindowsCredentials = "WindowsCredentials";
            public const string ImpersonateUser = "ImpersonateUser";
            public const string ImpersonateUserSpecified = "ImpersonateUserSpecified";
            public const string Prompt = "Prompt";
            public const string Enabled = "Enabled";
            public const string EnabledSpecified = "EnabledSpecified";
        }

        /// <summary>
        /// Constants related to Data Set
        /// </summary>
        public class DataSetConstants
        {
            public const string DataSets = "DataSets";
            public const string DataSet = "DataSet";
            public const string SharedDataSet = "SharedDataSet";
            public const string SharedDataSetReference = "SharedDataSetReference";

            public const string Query = "Query";
            public const string DataSourceReference = "DataSourceReference";
            public const string DataSourceName = "DataSourceName";
            public const string DataSetParameters = "DataSetParameters";
            public const string DataSetParameter = "DataSetParameter";
            public const string QueryDefinition = "QueryDefinition";

            public const string QueryParameters = "QueryParameters";
            public const string QueryParameter = "QueryParameter";

            public const string Fields = "Fields";
            public const string DataField = "DataField";

            public const string Filters = "Filters";
            public const string Filter = "Filter";
        }

        /// <summary>
        /// Constants related to Power BI Api
        /// </summary>
        public class PowerBIWrapperConstants
        {
            public const string MyWorkspace = "My Workspace";
            public const string ResourceUrl = "https://analysis.windows.net/powerbi/api";
            public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";
            public const string AuthorityUri = "https://login.microsoftonline.com/common";
            public const string PowerBiApiUri = "https://api.powerbi.com";
        }
    }
}
