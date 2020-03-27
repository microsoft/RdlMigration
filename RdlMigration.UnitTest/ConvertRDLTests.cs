// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Linq;
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
    public class ConvertRDLTests
    {
        [TestMethod]
        public void SubreportsAreDiscovered()
        {
            var mockApp = new Mock<ConvertRDL>();
            var mockServer = new Mock<IReportingService2010>();

            mockServer.Setup(p => p.GetItemType(It.IsAny<string>())).Returns(SoapApiConstants.Report);
            var sut = new ConvertRDL();
            sut.rootFolder = "/test";
            sut.rdlFileIO = new RdlFileIO(mockServer.Object);
            XDocument doc = XDocument.Load("../../testFiles/subreport_SubreportInBody.rdl");
            var subreports = sut.DiscoverSubreports(doc);

            Assert.AreEqual(subreports.Count, 2);
            Assert.AreEqual(subreports[0], "/test/SimpleRectangles");
            Assert.AreEqual(subreports[1], "/test/Burger");
        }

        [TestMethod]
        public void InvalidPathsAreIgnored()
        {
            var mockApp = new Mock<ConvertRDL>();
            var mockServer = new Mock<IReportingService2010>();

            mockServer.Setup(p => p.GetItemType(It.IsAny<string>())).Returns(SoapApiConstants.Folder);
            var sut = new ConvertRDL();
            sut.rootFolder = "/test";
            sut.rdlFileIO = new RdlFileIO(mockServer.Object);
            XDocument doc = XDocument.Load("../../testFiles/subreport_SubreportInBody.rdl");
            var subreports = sut.DiscoverSubreports(doc);

            Assert.AreEqual(subreports.Count, 0);
        }

        [TestMethod]
        public void DuplicateNamesAreIgnored()
        {
            var mockApp = new Mock<ConvertRDL>();
            var mockServer = new Mock<IReportingService2010>();

            mockServer.Setup(p => p.GetItemType(It.IsAny<string>())).Returns(SoapApiConstants.Report);
            var sut = new ConvertRDL();
            sut.rootFolder = "/test";
            sut.rdlFileIO = new RdlFileIO(mockServer.Object);
            sut.reportNameMap.TryAdd("SimpleRectangles", 1);
            XDocument doc = XDocument.Load("../../testFiles/subreport_SubreportInBody.rdl");
            var subreports = sut.DiscoverSubreports(doc);

            Assert.AreEqual(subreports.Count, 1);
            Assert.AreEqual(subreports[0], "/test/Burger");
        }
    }
}
