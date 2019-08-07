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
            Assert.AreEqual("DataSource0_ds2", references[1].Name);
        }
    }
}
