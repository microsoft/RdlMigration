// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RdlMigration.ReportServerApi;
using static RdlMigration.ElementNameConstants;

namespace RdlMigration.UnitTest
{
    [TestClass]
    public class RdlFileIOTests
    {
        [TestMethod]
        public void TestFromServer_SharedDataSourceNamesAreKept()
        {
            var mockServer = new Mock<IReportingService2010>();
            var mockDataSource = new ItemReferenceData
            {
                Name = "DataSource1",
                Reference = "/reference/ds1",
                ReferenceType = "DataSource"
            };

            mockServer.Setup(p => p.GetItemReferences(It.IsAny<string>(), "DataSource")).Returns(new ItemReferenceData[] { mockDataSource });
            var sut = new RdlFileIO(mockServer.Object);
            var references = sut.GetDataSourceReference("path");

            Assert.AreEqual(1, references.Count);
            Assert.AreEqual("DataSource1", references[0].Name);
        }

        [TestMethod]
        public void TestFromServer_SharedDataSetDataSourceNamesAreRenamed()
        {
            var mockServer = new Mock<IReportingService2010>();
            var mockDataSource = new ItemReferenceData
            {
                Name = "DataSource1",
                Reference = "/reference/ds1",
                ReferenceType = "DataSource"
            };

            var mockDataSource2 = new ItemReferenceData
            {
                Name = "DataSource2",
                Reference = "/reference/ds2",
                ReferenceType = "DataSource"
            };

            var mockDataSet = new ItemReferenceData
            {
                Name = "Dataset1",
                Reference = "/reference/ds2",
                ReferenceType = "DataSource"
            };

            mockServer.Setup(p => p.GetItemReferences("/report", "DataSource"))
                .Returns(new ItemReferenceData[] { mockDataSource });

            mockServer.Setup(p => p.GetItemReferences("/reference/ds2", "DataSource"))
                .Returns(new ItemReferenceData[] { mockDataSource2 });

            mockServer.Setup(p => p.GetItemReferences(It.IsAny<string>(), "DataSet"))
                .Returns(new ItemReferenceData[] { mockDataSet });

            var sut = new RdlFileIO(mockServer.Object);
            var references = sut.GetDataSourceReference("/report");

            Assert.AreEqual(2, references.Count);
            Assert.AreEqual("DataSource1", references[0].Name);
            Assert.IsTrue(references[1].Name.Contains("ds2"));
        }

        [TestMethod]
        public void DuplicateDataSource()
        {
            string reportPath = "/Report";
            string dataSet1Name = "Data Set";
            string dataSet1Path = "/Data Set";
            string dataSet2Name = "Data Set2";
            string dataSet2Path = "/Data Set2";

            var mockApp = new Mock<ConvertRDL>();
            var mockServer = new Mock<IReportingService2010>();

            ItemReferenceData[] referenceDatasDataSets = new ItemReferenceData[]
            {
                new ItemReferenceData{Name = dataSet1Name,
                Reference = dataSet1Path},
                new ItemReferenceData{Name = dataSet2Name,
                Reference = dataSet2Path}
            };

            ItemReferenceData[] referenceDatasDataSource = new ItemReferenceData[]
            {
                new ItemReferenceData{Name = "DataSourceNewShare",
                Reference = "/New Share"},
            };

            byte[] dataSet1 = File.ReadAllBytes("../../testFiles/one_DataSource_for_two_DataSets/Data Set.rsd");
            byte[] dataSet2 = File.ReadAllBytes("../../testFiles/one_DataSource_for_two_DataSets/Data Set2.rsd");


            mockServer.Setup(p => p.GetItemReferences(reportPath, DataSetConstants.DataSet)).Returns(referenceDatasDataSets);
            mockServer.Setup(p => p.GetItemDefinition(dataSet1Path)).Returns(dataSet1);
            mockServer.Setup(p => p.GetItemDefinition(dataSet2Path)).Returns(dataSet2);
            mockServer.Setup(p => p.GetItemReferences(dataSet1Path, DataSourceConstants.DataSource)).Returns(referenceDatasDataSource);
            mockServer.Setup(p => p.GetItemReferences(dataSet2Path, DataSourceConstants.DataSource)).Returns(referenceDatasDataSource);

            var sut = new RdlFileIO(mockServer.Object);

            DataSource[] uniqueDataSource = sut.GetUniqueDataSources(reportPath);
            
            Assert.AreEqual(uniqueDataSource.Length, 1);          
        }
    }
}
