// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RdlMigration.UnitTest
{
    [TestClass]
    public class AttachMetadataTests
    {
        [TestMethod]
        public void NoExistingMetadata()
        {
            XDocument doc = XDocument.Load("../../testFiles/ReportWithoutAuthoringMetadata.rdl");
            OriginTagging.Ensure(doc);

            var xml= doc.Root.ToString();

            Assert.IsTrue(xml.Contains("xmlns:am=\"http://schemas.microsoft.com/sqlserver/reporting/authoringmetadata\""), "Missing authoring namespace");
            Assert.IsTrue(xml.Contains($"<am:Name>{OriginTagging.PsMigrationTool}</am:Name>"), "Missing authoring tool name");
        }

        [TestMethod]
        public void ExistingMetadata()
        {
            XDocument doc = XDocument.Load("../../testFiles/ReportWithAuthoringMetadata.rdl");
            OriginTagging.Ensure(doc);

            var xml = doc.Root.ToString();

            Assert.IsTrue(xml.Contains("xmlns:am=\"http://schemas.microsoft.com/sqlserver/reporting/authoringmetadata\""), "Missing authoring namespace");
            Assert.IsTrue(xml.Contains($"<am:Name>{OriginTagging.PsMigrationTool}</am:Name>"), "Missing authoring tool name");
        }
    }
}
