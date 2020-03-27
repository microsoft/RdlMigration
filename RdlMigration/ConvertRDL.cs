// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using RdlMigration.ReportServerApi;
using static RdlMigration.ElementNameConstants;

namespace RdlMigration
{
    /// <summary>
    /// The class that actually does the conversion.
    /// Methods of reading local files are also included in this file.
    /// </summary>
    public class ConvertRDL
    {
        public string rootFolder;
        public RdlFileIO rdlFileIO;

        // Report name to file count mapping -
        // Avoid uploading the same report twice and blocks other reports with colliding file names
        // Can be used in future work to support renaming of duplicate files
        public ConcurrentDictionary<string, int> reportNameMap = new ConcurrentDictionary<string, int>();

        // Queue of all files to be uploaded
        public List<string> reportPaths = new List<string>();

        /// <summary>
        /// Takes a folder of reports, convert them and upload them to PBI workspace.
        /// </summary>
        /// <param name="urlEndPoint">the end point of report server.</param>
        /// <param name="inputPath">The Path of input Folder.</param>
        /// <param name="workspaceName">The name of requesting workspace.</param>
        /// <param name="clientID">The clientID of the App registered with permissions of Reading/Writing dataset, report and Reading Workspaces.</param>
        public void ConvertFolder(string urlEndPoint, string inputPath, string workspaceName, string clientID)
        {
            Trace("Starting the log-in window");
            PowerBIClientWrapper powerBIClient = new PowerBIClientWrapper(workspaceName, clientID);
            Trace("Log-in successfully, retrieving the reports...");

            rdlFileIO = new RdlFileIO(urlEndPoint);

            Trace($"Starting conversion and uploading the reports {DateTime.UtcNow.ToString()}");

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            if (!rdlFileIO.IsFolder(inputPath))
            {
                rootFolder = Path.GetDirectoryName(inputPath).Replace("\\", "/");
                var reportName = Path.GetFileName(inputPath);
                reportNameMap.TryAdd(reportName, 1);
                reportPaths.Add(inputPath);
            }
            else
            {
                rootFolder = inputPath;
                var rootReports = rdlFileIO.GetReportsInFolder(inputPath);
                Console.WriteLine($"Found {rootReports.Length} reports to convert");
                foreach (string reportPath in reportPaths)
                {
                    var reportName = Path.GetFileName(reportPath);
                    reportNameMap.TryAdd(reportName, 1);
                }
                reportPaths.AddRange(rootReports);
            }
            foreach (string reportPath in reportPaths.ToList())
            {
                ConvertAndUploadReport(
                    powerBIClient,
                    rdlFileIO,
                    reportPath);
            }
        }

        private void ConvertAndUploadReport(PowerBIClientWrapper powerBIClient, RdlFileIO rdlFileIO, string reportPath)
        {
            var reportName = Path.GetFileName(reportPath);
            var report = rdlFileIO.DownloadRdl(reportPath);
            SaveAndCopyStream(reportName, report, $"output\\{reportName}_original.rdl");

            if (powerBIClient.ExistReport(reportName))
            {
                Trace($"CONFLICT : {reportName}  A report with the same name already exists in the workspace");
            }
            else
            {
                try
                {
                    XElement[] dataSets = rdlFileIO.GetDataSets(reportPath, out Dictionary<string, XElement> referenceDataSetMap);
                    DataSource[] dataSources = rdlFileIO.GetDataSources(reportPath);

                    var convertedFile = ConvertFile(report, dataSources, referenceDataSetMap);
                    SaveAndCopyStream(reportName, convertedFile, $"output\\{reportName}_convert.rdl");

                    powerBIClient.UploadRDL(reportName + ReportFileExtension, convertedFile);

                    Trace($"SUCCESS : {reportName}  The file is successfully uploaded");
                }
                catch (HttpOperationException httpException)
                {
                    string errorMessage;
                    string requestId = String.Empty;
                    if (httpException?.Response?.Headers.ContainsKey("RequestId") == true)
                    {
                        requestId = httpException.Response.Headers["RequestId"].First();
                    }

                    if (httpException.Response.Headers.TryGetValue("X-PowerBI-Error-Details", out IEnumerable<string> returnedJsonStr))
                    {
                        if (returnedJsonStr.Count() != 1)
                        {
                            Trace($"FAILED TO UPLOAD : {reportName} RequestId:{requestId} {httpException.Message}");
                        }
                        else
                        {
                            string jsonString = returnedJsonStr.First();
                            var returnedJsonDetail = JObject.Parse(jsonString);
                            errorMessage = returnedJsonDetail["error"]["pbi.error"]["details"][2]["detail"]["value"].Value<string>();
                            Trace($"FAILED TO UPLOAD :  {reportName} RequestId:{requestId} {errorMessage}");
                        }
                    }
                    else
                    {
                        Trace($"FAILED TO UPLOAD : {reportName} RequestId:{requestId} {httpException.Message}");
                    }
                }
                catch (Exception e)
                {
                    Trace($"FAILED : {reportName} {e.Message}");
                }
            }
        }

        private void SaveAndCopyStream(string reportName, Stream stream, string filePath)
        {
            using (var logStream = new MemoryStream())
            {
                stream.CopyTo(logStream);
                stream.Seek(0, SeekOrigin.Begin);
                logStream.Seek(0, SeekOrigin.Begin);

                using (var sr = new StreamReader(logStream))
                {
                    var rdl = sr.ReadToEnd();
                    try
                    {
                        File.WriteAllText(filePath, rdl);
                    }
                    catch (Exception e)
                    {
                        Trace($"FAILED : {reportName} {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// main method that runs soap api, take a rdl in report server and change it.
        /// </summary>
        /// <param name="urlEndPoint">The report Server url.</param>
        /// <param name="inputReportPath">The path of input report file.</param>
        /// <param name="outputPath"> The path of output file.</param>
        /// <param name="downloadPath">The path to store original rdl and rds file.</param>
        /// <returns>the output file path.</returns>
        public string RunSoap(string urlEndPoint, string inputReportPath, string outputPath = "./", string downloadPath = "./testFiles/")
        {
            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(downloadPath);

            string reportName = Path.GetFileNameWithoutExtension(inputReportPath);
            string outputFileName = Path.Combine(outputPath, reportName + ReportFileExtension);

            RdlFileIO rdlFileIO = new RdlFileIO(urlEndPoint);

            var rdlFilePath = rdlFileIO.DownloadRdl(inputReportPath, downloadPath);

            XElement[] dataSets = rdlFileIO.GetDataSets(inputReportPath, out Dictionary<string, XElement> referenceDataSetMap);
            DataSource[] dataSources = rdlFileIO.GetDataSources(inputReportPath);

            var dataSourceFilePath = Path.Combine(downloadPath, reportName + DataSourceFileExtension);
            var dataSetDirectoryPath = downloadPath + reportName + "_DataSets";

            rdlFileIO.WriteDataSetContent(dataSets, dataSetDirectoryPath);
            rdlFileIO.WriteDataSourceContent(dataSources, dataSourceFilePath);

            ConvertFile(rdlFilePath, dataSources, referenceDataSetMap, outputPath);

            Console.WriteLine($"Conversion from  {inputReportPath}  to  {outputFileName}  Completed.");
            return outputFileName;
        }

        /// <summary>
        /// the method that actually does the conversion, take the rdl file, its datasource/dataSet and convert
        /// to a new file with shared datasource.
        /// </summary>
        /// <param name="rdlFile">The rdl File Stream.</param>
        /// <param name="dataSources"> array of datasources used for conversion.</param>
        /// <param name="dataSetDict">Dict of Name-XElement Pair of dataSets used for conversion.</param>
        /// <returns>  the output file stream.</returns>
        public Stream ConvertFile(Stream rdlFile, DataSource[] dataSources, Dictionary<string, XElement> dataSetDict)
        {
            XDocument doc = XDocument.Load(rdlFile);

            Stream outputFile = new MemoryStream();
            ConvertFileWithDataSet(doc, dataSetDict);
            ConvertFileWithDataSource(doc, dataSources);
            reportPaths.AddRange(DiscoverSubreports(doc));
            doc.Save(outputFile);
            outputFile.Position = 0;

            return outputFile;
        }

        /// <summary>
        /// the method that actually does the conversion, take the rdl file, its datasource/dataSet and convert
        /// to a new file with shared datasource.
        /// </summary>
        /// <param name="rdlFilePath">The rdl File Stream.</param>
        /// <param name="dataSources">array of datasources used for conversion.</param>
        /// <param name="dataSetDict">Dict of Name-XElement Pair of dataSets used for conversion.</param>
        /// <param name="outputPath">Output File Path.</param>
        /// <returns>the path of file saved in local disk.</returns>
        public string ConvertFile(string rdlFilePath, DataSource[] dataSources, Dictionary<string, XElement> dataSetDict, string outputPath)
        {
            XDocument doc = XDocument.Load(rdlFilePath);

            string outputFileName = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(rdlFilePath) + ReportFileExtension);

            ConvertFileWithDataSet(doc, dataSetDict);
            ConvertFileWithDataSource(doc, dataSources);
            reportPaths.AddRange(DiscoverSubreports(doc));
            doc.Save(outputFileName);

            return outputFileName;
        }

        /// <summary>
        /// the method that does the conversion with dataSources, take the rdl file, its datasources and convert
        /// to a new file with Embedded datasource.
        /// </summary>
        /// <param name="doc"> The XDocument of local rdl File.</param>
        /// <param name="dataSources">array of datasources used for conversion.</param>
        public void ConvertFileWithDataSource(XDocument doc, DataSource[] dataSources)
        {
            // Set the root element of our analzing to "DataSource", cuz that is the only thing we are modify here
            var dataSourceRootElement = doc.Descendants(doc.Root.Name.Namespace + DataSourceConstants.DataSources);
            if (dataSourceRootElement.Count() == 1)
            {
                var dataSourcesElements = dataSourceRootElement.First().Elements();

                for (int i = dataSourcesElements.Count() - 1; i >= 0; i--)
                {
                    XElement dataSourceElement = dataSourcesElements.ElementAt(i);
                    if (dataSourceElement.Element(dataSourceElement.Name.Namespace + DataSourceConstants.DataSourceReference) != null)
                    {
                        dataSourceElement.Remove();
                    }
                }
            }

            foreach (var dataSource in dataSources)
            {
                DataSourceDefinition currentDataSource = (DataSourceDefinition)dataSource.Item;
                AddDataSource(doc, currentDataSource, dataSource.Name);
            }
        }

        /// <summary>
        /// take the rdl file, its dataSet and convert to a new file with Embedded dataSet.
        /// </summary>
        /// <param name="doc">The XDocument of rdl File.</param>
        /// <param name="dataSetMaps">Dict of Name-XElement Pair of dataSets used for conversion.</param>
        public void ConvertFileWithDataSet(XDocument doc, Dictionary<string, XElement> dataSetMaps)
        {
            var dataSetRootElememt = doc.Descendants(doc.Root.Name.Namespace + DataSetConstants.DataSets).FirstOrDefault();
            if (dataSetRootElememt != null)
            {
                XNamespace currNamespace = doc.Root.Name.Namespace;

                var dataSetElements = dataSetRootElememt.Elements();

                for (int i = dataSetElements.Count() - 1; i >= 0; i--)
                {
                    XElement dataSetElement = dataSetElements.ElementAt(i);
                    var sharedDataSetReferenceName = dataSetElement.Name.Namespace + DataSetConstants.SharedDataSetReference;
                    if (dataSetElement.Descendants(sharedDataSetReferenceName).Count() == 1)
                    {
                        string dataSetName = dataSetElement.Descendants(sharedDataSetReferenceName).First().Value;
                        if (dataSetMaps.TryGetValue(dataSetName, out XElement currDataSetNode))
                        {
                            currDataSetNode = new XElement(currDataSetNode);    // change passing by reference to by value, thus modification would not effect original dataSet
                            ChangeNameSpaceHelper(currNamespace, currDataSetNode);

                            var referenceNodes = currDataSetNode.Descendants(currDataSetNode.Name.Namespace + DataSetConstants.DataSourceReference);
                            if (referenceNodes.Count() != 0)
                            {
                                string dataSetSourceName = RdlFileIO.SerializeDataSourceName(referenceNodes.ElementAt(0).Value);
                                referenceNodes.ElementAt(0).ReplaceWith(new XElement(currDataSetNode.Name.Namespace + DataSetConstants.DataSourceName, dataSetSourceName));
                            }

                            currDataSetNode.Attribute("Name").SetValue(dataSetElement.Attribute("Name").Value);

                            AlignParameters(dataSetElement, currDataSetNode);

                            dataSetElement.Descendants(currNamespace + DataSetConstants.SharedDataSet).First().ReplaceWith(currDataSetNode.Descendants(currNamespace + DataSetConstants.Query).First());

                            if (dataSetElement.Descendants(currNamespace + DataSetConstants.Fields).Count() == 1 && currDataSetNode.Descendants(currNamespace + DataSetConstants.Fields).Count() == 1)
                            {
                                AlignCalculatedFields(dataSetElement.Descendants(currNamespace + DataSetConstants.Fields).First(), currDataSetNode.Descendants(currNamespace + DataSetConstants.Fields).First());
                            }

                            AlignFilters(dataSetElement, currDataSetNode);
                        }
                        else
                        {
                            throw new Exception($"Can't find corresponding Data Set {dataSetName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads a datasource file and take it into DataSource object.
        /// </summary>
        /// <param name="filePath">The local rds File path.</param>
        /// <returns> an array of DataSource Object.</returns>
        public DataSource[] ReadDataSourceFile(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var dataSources = doc.Element(DataSourceConstants.DataSources) == null ? new XElement[0] : doc.Element(DataSourceConstants.DataSources).Elements();
            DataSource[] retDataSourceArray = new DataSource[dataSources.Count()];
            int i = 0;
            foreach (XElement childNode in dataSources)
            {
                DataSource temp = new DataSource
                {
                    Name = childNode.Attribute("Name").Value
                };
                DataSourceDefinition currDataSourceDefinition = new DataSourceDefinition
                {
                    Extension = childNode.Element(DataSourceConstants.Extension).Value,
                    ConnectString = childNode.Element(DataSourceConstants.ConnectString).Value,
                    UseOriginalConnectString = childNode.Element(DataSourceConstants.UseOriginalConnectString).Value == "True",
                    OriginalConnectStringExpressionBased = childNode.Element(DataSourceConstants.OriginalConnectStringExpressionBased).Value == "True"
                };

                Enum.TryParse(childNode.Element(DataSourceConstants.CredentialRetrieval).Value, true, out CredentialRetrievalEnum credentialRetrieval);
                currDataSourceDefinition.CredentialRetrieval = credentialRetrieval;

                currDataSourceDefinition.Enabled = childNode.Element(DataSourceConstants.Enabled).Value == "True";

                temp.Item = currDataSourceDefinition;

                retDataSourceArray[i++] = temp;
            }

            return retDataSourceArray;
        }

        /// <summary>
        ///  Reads a datasource file and take it into DataSource object.
        /// </summary>
        /// <param name="dirPath">The local rds File path.</param>
        /// <returns> The Dictonary of Data Set Name and Data Set XElement.</returns>
        public Dictionary<string, XElement> ReadDataSet(string dirPath)
        {
            Dictionary<string, XElement> retMap = new Dictionary<string, XElement>();
            string[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(dirPath);
            }
            catch (Exception)
            {
                return retMap;
            }

            foreach (string filename in filePaths)
            {
                var currDataSetNode = ReadDataSetFile(filename);
                retMap.Add(currDataSetNode.Attribute("Name").Value, currDataSetNode);
            }

            return retMap;
        }

        private XElement ReadDataSetFile(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var dataSets = (XElement)doc.Root.FirstNode;
            return dataSets;
        }

        private void AlignCalculatedFields(XElement reportFieldElement, XElement dataSetFieldElements)
        {
            Dictionary<string, XElement> dataSetFieldDict = dataSetFieldElements.Elements().ToDictionary(x => x.Attribute("Name").Value, x => x);

            foreach (var reportField in reportFieldElement.Elements())
            {
                // Only taking action if the original field does not have "Value"
                if (reportField.Descendants(reportField.Name.Namespace + DataSetConstants.DataField).Count() == 1
                    && reportField.Descendants(reportField.Name.Namespace + "Value").Count() == 0)
                {
                    var reportDataFieldNode = reportField.Descendants(reportField.Name.Namespace + DataSetConstants.DataField).First();
                    string dataSetFieldName = reportDataFieldNode.Value;
                    if (dataSetFieldDict.TryGetValue(dataSetFieldName, out XElement currDataSetField))
                    {
                        var dataSetFieldValueNode = currDataSetField.Descendants(currDataSetField.Name.Namespace + "Value");
                        var dataSetDataFieldNode = currDataSetField.Descendants(currDataSetField.Name.Namespace + DataSetConstants.DataField);
                        if (dataSetFieldValueNode.Count() == 1)
                        {
                            ReplaceXElement(reportDataFieldNode, dataSetFieldValueNode.First());
                        }
                        else if (dataSetDataFieldNode.Count() == 1)
                        {
                            ReplaceXElement(reportDataFieldNode, dataSetDataFieldNode.First());
                        }

                        // If they have the same name then we don't have to worry about complex naming issues and can safely ignore current datasetField
                        if (currDataSetField.Attribute("Name").Value == reportField.Attribute("Name").Value)
                        {
                            dataSetFieldDict.Remove(dataSetFieldName);
                        }
                    }
                }
            }

            if (dataSetFieldDict.Count() != 0)
            {
                foreach (var element in dataSetFieldDict)
                {
                    element.Value.Name = reportFieldElement.Name.Namespace + element.Value.Name.LocalName;
                    reportFieldElement.Add(element.Value);
                }
            }
        }

        private void AlignParameters(XElement reportDataSetElement, XElement currDataSetNode)
        {
            XNamespace currNamespace = reportDataSetElement.Name.Namespace;
            var queryParameterEnum = reportDataSetElement.Descendants(currNamespace + DataSetConstants.QueryParameters);
            var dataSetParamEnum = currDataSetNode.Descendants(currNamespace + DataSetConstants.DataSetParameters);

            XElement queryParameters = queryParameterEnum.Count() == 0 ? new XElement(currNamespace + DataSetConstants.QueryParameters) : queryParameterEnum.First();
            Dictionary<string, XElement> parameterMap = queryParameters.Elements().ToDictionary(x => x.Attribute("Name").Value, x => x);

            if (dataSetParamEnum.Count() != 0)
            {
                var datasetParameters = dataSetParamEnum.First();
                foreach (var datasetParam in datasetParameters.Elements())
                {
                    var parameterName = datasetParam.Attribute("Name").Value;
                    if (!parameterMap.TryGetValue(parameterName, out XElement output))
                    {
                        var defaultValue = datasetParam.Element(currNamespace + "DefaultValue");
                        queryParameters.Add(CreateNewQueryParameter(currNamespace, parameterName, defaultValue == null ? "" : defaultValue.Value));
                    }
                }

                datasetParameters.Remove();
            }

            if (!queryParameters.IsEmpty)
            {
                currDataSetNode.Descendants(currNamespace + DataSetConstants.Query).First().Add(queryParameters);
            }
        }

        private XElement CreateNewQueryParameter(XNamespace currNamespace, string name, string value)
        {
            XElement retElement = new XElement(currNamespace + DataSetConstants.QueryParameter);
            retElement.SetAttributeValue("Name", name);
            retElement.Add(new XElement(currNamespace + "Value", value));
            return retElement;
        }

        private void AlignFilters(XElement reportDataSetElement, XElement currDataSetNode)
        {
            var currDataSetFilterElements = currDataSetNode.Descendants(currDataSetNode.Name.Namespace + DataSetConstants.Filters);
            var reportDataSetFilterElements = reportDataSetElement.Descendants(reportDataSetElement.Name.Namespace + DataSetConstants.Filters);
            if (currDataSetFilterElements.Count() != 1)
            {
                if (currDataSetFilterElements.Count() == 0)
                {
                    return;
                }
                else
                {
                    throw new Exception("Illegal FIle - " + currDataSetNode.Name.LocalName + " has more than 1 Filters Element found");
                }
            }

            var dataSetFilters = currDataSetFilterElements.First().Elements().ToArray();
            XElement reportDataSetFilters;

            if (reportDataSetFilterElements.Count() == 0)
            {
                reportDataSetFilters = new XElement(reportDataSetElement.Name.Namespace + DataSetConstants.Filters);
                reportDataSetElement.Add(reportDataSetFilters);
            }
            else if (reportDataSetFilterElements.Count() == 1)
            {
                reportDataSetFilters = reportDataSetFilterElements.First();
            }
            else
            {
                throw new Exception("Illegal FIle - " + reportDataSetElement.Name.LocalName + " has more than 1 Filters ELement found");
            }

            for (int i = dataSetFilters.Count() - 1; i >= 0; i--)
            {
                reportDataSetFilters.AddFirst(dataSetFilters[i]);
            }
        }

        private void AddDataSource(XDocument doc, DataSourceDefinition currentDataSource, string dataSourceName)
        {
            XElement dataSourceBaseElement;
            if (doc.Descendants(doc.Root.Name.Namespace + DataSourceConstants.DataSources).Count() == 0)
            {
                dataSourceBaseElement = new XElement(doc.Root.Name.Namespace + DataSourceConstants.DataSources);
                doc.Root.Add(dataSourceBaseElement);
            }
            else
            {
                dataSourceBaseElement = doc.Descendants(doc.Root.Name.Namespace + DataSourceConstants.DataSources).First();
            }

            var currNode = CreateDataSourceNode(doc, currentDataSource, dataSourceName);

            dataSourceBaseElement.Add(currNode);
        }

        private XElement CreateDataSourceNode(XDocument doc, DataSourceDefinition currentDataSource, string dataSourceName)
        {
            // get all the information we need
            var dataSourceConnectString = currentDataSource.ConnectString;

            var dataSourceDataProvider = (currentDataSource.Extension == DataSourceConstants.SQL && IsSQLAzure(dataSourceConnectString)) ? DataSourceConstants.SQLAzure : currentDataSource.Extension;

            var dataSourceSecurityType = currentDataSource.CredentialRetrieval;

            XElement currNode = new XElement(doc.Root.Name.Namespace + DataSourceConstants.DataSource);
            currNode.Add(new XAttribute("Name", dataSourceName));       // Name sometime changes because of out-of-order DataSources

            var connectionPropertiesNode = CreateNode(doc, DataSourceConstants.ConnectionProperties, string.Empty);
            currNode.Add(connectionPropertiesNode);
            connectionPropertiesNode.Add(CreateNode(doc, DataSourceConstants.DataProvider, dataSourceDataProvider));
            connectionPropertiesNode.Add(CreateNode(doc, DataSourceConstants.ConnectString, dataSourceConnectString));
            switch (dataSourceSecurityType)
            {
                case CredentialRetrievalEnum.Integrated:
                    connectionPropertiesNode.Add(CreateNode(doc, DataSourceConstants.IntegratedSecurity, "true"));
                    currNode.Add(CreateNode_rd(doc, "SecurityType", "Integrated"));
                    break;
                case CredentialRetrievalEnum.Store:
                    currNode.Add(CreateNode_rd(doc, "SecurityType", "DataBase"));
                    break;
                case CredentialRetrievalEnum.Prompt:
                    connectionPropertiesNode.Add(CreateNode(doc, DataSourceConstants.Prompt, "Specify a user name and password for data source " + dataSourceName));
                    currNode.Add(CreateNode_rd(doc, "SecurityType", "DataBase"));
                    break;
                case CredentialRetrievalEnum.None:
                    currNode.Add(CreateNode_rd(doc, "SecurityType", "None"));
                    break;

                default:
                    break;
            }

            return currNode;
        }

        /// <summary>
        /// This method takes in an XDocument, looks for valid subreports within, and attempts 
        /// to upload these subreports.
        /// </summary>
        /// <param name="doc"> The XDocument of local rdl File.</param>
        public List<string> DiscoverSubreports(XDocument doc)
        {
            var subreports = doc.Descendants(doc.Root.Name.Namespace + "Subreport");
            var subreportPaths = new List<string>();

            foreach (XElement subreport in subreports)
            {
                var subreportName = subreport.Elements().First().Value;
                var subreportPath = Path.Combine(rootFolder, subreportName);
                subreportPath = Path.GetFullPath(subreportPath).Replace("\\", "/");
                subreportPath = subreportPath.Substring(subreportPath.IndexOf('/'));   // file path from server root
                subreportName = Path.GetFileName(subreportPath);                       // clean file name with no folder path
                
                if (!rdlFileIO.IsReport(subreportPath))
                {
                    Trace($"SUBREPORT FAIL : {subreportPath} does not exist or is not a report");
                    continue;
                }

                if (reportNameMap.TryAdd(subreportName, 1))
                {
                    subreportPaths.Add(subreportPath);
                    Trace($"SUBREPORT : Attempting to upload subreport from {subreportPath}");
                }
                else
                {
                    Trace($"CONFLICT : a file with name \"{subreportName}\" has already been uploaded ");
                }
            }

            return subreportPaths;
        }

        private bool IsSQLAzure(string connectString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                ConnectionString = connectString
            };
            string source = builder.DataSource;

            return SQLAzureSuffixes.Any(sqlAzureSuffix => source.EndsWith(sqlAzureSuffix, StringComparison.CurrentCultureIgnoreCase));
        }

        private void ChangeNameSpaceHelper(XNamespace ns, XElement root)
        {
            XNamespace rd = ReportDesignerNameSpace;
            if (root == null || root.Name.LocalName == DataSetConstants.QueryDefinition || root.Name.Namespace == rd)
            {
                return;
            }

            root.Name = ns.GetName(root.Name.LocalName);
            if (root.Elements() != null)
            {
                foreach (XElement child in root.Elements())
                {
                    ChangeNameSpaceHelper(ns, child);
                }
            }
        }

        // Create a XML node with normal namespace
        private XElement CreateNode(XDocument root, string elementName, string innerText)
        {
            XNamespace xmlNameSpace = root.Root.Attribute("xmlns").Value;
            XElement retNode = new XElement(xmlNameSpace + elementName)
            {
                Value = innerText
            };
            return retNode;
        }

        // Create a XML node with rd namespace
        private XElement CreateNode_rd(XDocument root, string elementName, string innerText)
        {
            XNamespace reportDesignerNameSpace = ReportDesignerNameSpace;
            XElement retNode = new XElement(reportDesignerNameSpace + elementName)
            {
                Value = innerText
            };
            return retNode;
        }

        private void ReplaceXElement(XElement originalElement, XElement newElement)
        {
            newElement.Name = originalElement.Name.Namespace + newElement.Name.LocalName;
            originalElement.ReplaceWith(newElement);
        }

        private void Trace(string message)
        {
            try
            {
                Console.WriteLine(message);
                File.AppendAllText(ConversionLogFileName, message);
                File.AppendAllText(ConversionLogFileName, Environment.NewLine);
            }
            catch (IOException)
            {
                // ignore failure to trace
            }
        }
    }
}