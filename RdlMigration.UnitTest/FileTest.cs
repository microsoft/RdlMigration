// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RdlMigration.ReportServerApi;
using static RdlMigration.ElementNameConstants;

namespace RdlMigration.UnitTest
{
    [TestClass]
    public class FilesTest
    {
        public string outputPath = "../../outputFiles/";
        public string downloadPath = "../../testFiles/";

        [TestMethod]
        public void TestSimpleEnterDataSharedDataSource()
        {
            TestFile("test_enterdata_simple");
        }
        [TestMethod]
        public void TestSimpleSharedDataSource()
        {
            TestFile("staff_test_Sh_EmSet");
        }
        [TestMethod]
        public void TestSimpleMixedDataSource()
        {
            TestFile("test_1xSh1xEmSameDB_EmSet");
        }
        [TestMethod]
        public void Test3xSharedDataSourceWithDifferentAuth()
        {
            TestFile("test_3xShsameDBdiffAuth_EmSet");
        }
        [TestMethod]
        public void Test2xSharedDataSource()
        {
            TestFile("test_2xShSameDB_EmSet");
        }
        [TestMethod]
        public void TestSimpleEmbeddedDataSource()
        {
            TestFile("staff_test_Em");
        }
        [TestMethod]
        public void TestSimpleSharedDataSourceWithSharedDataSet()
        {
            TestFile("staff_test_Sh");
        }
        [TestMethod]
        public void TestSimpleSharedDataSet()
        {
            TestFile("film_test_simple");
        }
        [TestMethod]
        public void TestSharedDataSetNoRd()
        {
            TestFile("film_test_simple_nord");
        }
        [TestMethod]
        public void Test3xSameSharedDataSet()
        {
            TestFile("orders_3xSameDSet");
        }
        [TestMethod]
        public void TestNoDatasets()
        {
            TestFile("simpletextbox");
        }
        [TestMethod]
        public void TestSimpleSharedDataSet2()
        {
            TestFile("film_test_ShSet");
        }
        [TestMethod]
        public void TestSimpleEmbeddedDataSourceWithSharedDataSet()
        {
            TestFile("staff_test_Em_ShSet");
        }
        [TestMethod]
        public void TestSimpleSharedDataSourceWithMixedDataSets()
        {
            TestFile("staff_test_Mixed");
        }
        [TestMethod]
        public void TestSharedDataSourceWithFiveDataSetsMapToShareSource()
        {
            TestFile("bikeStore5xShSet");
        }
        [TestMethod]
        public void TestCalFieldSimple()
        {
            TestFile("film_test_CalF_simple");
        }
        [TestMethod]
        public void TestCalFieldSimple2()
        {
            TestFile("orders_test_CalF_simple2");
        }
        [TestMethod]
        public void TestCalFieldComplexWIthNamingIssue()
        {
            TestFile("order_test_CalField");
        }
        [TestMethod]
        public void Test3xCalFieldComplexWIthNamingIssue()
        {
            TestFile("order_test_3xCalF_complex");
        }
        [TestMethod]
        public void Test3xDataSetCalFieldComplex()
        {
            TestFile("order_test_3xCalF_complex2");
        }
        [TestMethod]
        public void TestSharedDataSourceWithFiveDataSetsMapToEmbeddedSource()
        {
            TestFile("bikeStore_Em_5xShSet");
        }
        [TestMethod]
        public void TestEnterDataSharedDataSourceParamSimple()
        {
            TestFile("enterdata_param_simple");
        }
        [TestMethod]
        public void TestaSharedDataSourceParamRdlOnly()
        {
            TestFile("orders_rdlParamOnly");
        }
        [TestMethod]
        public void TestaSharedDataSourceParamWithMixedParam()
        {
            TestFile("staff_1xmixedParam3xDSOnly");
        }
        [TestMethod]
        public void TestaSharedDataSourceParamWithOverridingParamInReport()
        {
            TestFile("staff_overridingDefaultParam");
        }
        [TestMethod]
        public void TestSharedDataSourceWithParamSimple()
        {
            TestFile("staff_simpleParam");
        }
        [TestMethod]
        public void TestSharedDataSourceWithDataSetParamSimple()
        {
            TestFile("staff_simpleParamDSOnly");
        }
        [TestMethod]
        public void TestSharedDataSourceWithParamVeryComplex()
        {
            TestFile("staff_3xDS_Param_VeryComplex");
        }
        [TestMethod]
        public void TestSharedDataSourceWithFilterSimple()
        {
            TestFile("orderItem_Filter_simple");
        }
        [TestMethod]
        public void TestSharedDataSourceWithFilterSimple2()
        {
            TestFile("orderItem_Filter_simple2");
        }
        [TestMethod]
        public void TestSharedDataSourceWithReportFilterOnly()
        {
            TestFile("film_Filter_reportOnly");
        }
        [TestMethod]
        public void TestSharedDataSourceWithDataSetFilterOnly()
        {
            TestFile("orderItem_Filter_DSOnly");
        }
        public void TestSharedDataSourceWithComplexFilterOrder()
        {
            TestFile("enterdata_FilterOrder");
        }
        public void TestSharedDataSourceWithFilterComplex()
        {
            TestFile("orderItem_Filter_complex");
        }

        private void TestFile(string test)
        {
            TestWithPath(downloadPath + test + ".rdl", downloadPath + test + ".rds", downloadPath + test + "_DataSets");
        }

        private void TestWithPath(string rdlFilePath, string dataSourcePath, string dataSetPath)
        {
            var app = new ConvertRDL();
            var dataSources = ReadDataSourceFile(dataSourcePath);
            var dataSets = ReadDataSet(rdlFilePath, dataSetPath);

            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(downloadPath);
            var file = app.ConvertFile(rdlFilePath, dataSources, dataSets, outputPath);

            ValidateSchema(file);
        }

        /// <summary>
        /// Returns the list of dataset names for a datasetPath used b
        /// </summary>
        public Dictionary<string, List<string>> GetDataSetsMap(string rdlFilePath)
        {
            var result = new Dictionary<string, List<string>>();
            XDocument doc = XDocument.Load(rdlFilePath);
            var dataSets = doc.Descendants().Where(p => p.Name.LocalName == DataSetConstants.SharedDataSetReference);
            foreach(var dataSet in dataSets)
            {
                if (!result.ContainsKey(dataSet.Value))
                {
                    result.Add(dataSet.Value, new List<string>());
                }

                result[dataSet.Value].Add(dataSet.Parent.Parent.Attribute("Name").Value);
            }
            return result;
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
        private Dictionary<KeyValuePair<string, string>, XElement> ReadDataSet(string rdlLocalFilePath, string dirPath, Dictionary<string, string> dataSetNameRef = null)
        {
            var retMap = new Dictionary<KeyValuePair<string, string>, XElement>();
            string[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(dirPath);
            }
            catch (Exception)
            {
                return retMap;
            }

            var dataSetMap = GetDataSetsMap(rdlLocalFilePath);
            foreach (string filename in filePaths)
            {
                var currDataSetNode = ReadDataSetFile(filename);
                var dataSetName = currDataSetNode.Attribute("Name").Value;

                // for tests that have the same dataset referenced multiple times, we need to add
                // to the map for each time its found.
                foreach (var rdlDataSetName in dataSetMap[dataSetName])
                {
                    retMap.Add(new KeyValuePair<string, string>(rdlLocalFilePath, rdlDataSetName), currDataSetNode);
                }
            }

            return retMap;
        }

        private XElement ReadDataSetFile(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            var dataSets = (XElement)doc.Root.FirstNode;
            return dataSets;
        }

        private void ValidateSchema(string filePath)
        {
            XmlSchemaSet schemas = new XmlSchemaSet();
            XmlSchema reportDefinitionSchema;
            using (var reader = new StringReader(Properties.Resources.reportdefinition))
            {
                reportDefinitionSchema = XmlSchema.Read(reader, ValidationEventHandler);
            }

            schemas.Add(reportDefinitionSchema);

            XDocument doc = XDocument.Load(filePath);
            doc.Validate(schemas, ValidationEventHandler);
        }

        private void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            throw e.Exception;
        }

    }
}
