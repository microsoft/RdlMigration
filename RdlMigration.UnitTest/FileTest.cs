using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using RdlMigration;
using RdlMigration.ReportServerApi;
using System.Xml.Schema;

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
            var dataSources = app.ReadDataSourceFile(dataSourcePath);
            var dataSets = app.ReadDataSet(dataSetPath);

            //var dataSets = RdlFileIO.WriteDataSetContent(rdlFilePath, "aka" ,out DataSource[] a);
            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(downloadPath);
            var file = app.ConvertFile(rdlFilePath, dataSources, dataSets, outputPath);

            ValidateSchema(file);
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
