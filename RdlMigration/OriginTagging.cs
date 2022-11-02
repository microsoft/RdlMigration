// Copyright (c) 2022 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Linq;
using System.Xml.Linq;

namespace RdlMigration
{
    public static class OriginTagging
    {
        public const string PsMigrationTool = "GitMigrationTool";
        public const string PsMigrationToolVersion = "1.0";

        private const string AuthoringShortNamespace = "am";
        private const string AuthoringFullQualifiedNamespace = "http://schemas.microsoft.com/sqlserver/reporting/authoringmetadata";
        private const string AuthoringMetadataElementName = "AuthoringMetadata";
        private const string LastModifiedElementName = "LastModifiedTimestamp";
        private const string CreatedBy = "CreatedBy";
        private const string UpdatedBy = "UpdatedBy";
        private const string Name = "Name";
        private const string Version = "Version";


        public static void Ensure(XDocument doc)
        {
            var root = doc.Root;

            XNamespace authoringNamespace = AuthoringFullQualifiedNamespace;
            var authoringNamespaceAttribute = new XAttribute(XNamespace.Xmlns + AuthoringShortNamespace, AuthoringFullQualifiedNamespace);
            root.Add(authoringNamespaceAttribute);

            var authoringMetadata = EnsureNodeIsChildOf(authoringNamespace, AuthoringMetadataElementName, root);
            var lastModified = EnsureNodeIsChildOf(authoringNamespace, LastModifiedElementName, authoringMetadata);
            lastModified.Value = DateTime.UtcNow.ToString("o");

            var createdBy = EnsureNodeIsChildOf(authoringNamespace, CreatedBy, authoringMetadata);
            var createdByName = EnsureNodeIsChildOf(authoringNamespace, Name, createdBy);
            createdByName.Value = PsMigrationTool;

            var createdByVersion = EnsureNodeIsChildOf(authoringNamespace, Version, createdBy);
            createdByVersion.Value = PsMigrationToolVersion;

            var updatedBy = EnsureNodeIsChildOf(authoringNamespace, UpdatedBy, authoringMetadata);
            var updatedByName = EnsureNodeIsChildOf(authoringNamespace, Name, updatedBy);
            updatedByName.Value = PsMigrationTool;

            var updatedByVersion = EnsureNodeIsChildOf(authoringNamespace, Version, createdBy);
            updatedByVersion.Value = PsMigrationToolVersion;
        }

        private static XElement EnsureNodeIsChildOf(XNamespace xmlNamespace, string nodeName, XElement childOf)
        {
            var fullyQualifiedName = xmlNamespace + nodeName;

            var nodeToReturn = childOf.Descendants(fullyQualifiedName).FirstOrDefault();
            if (nodeToReturn == null)
            {
                childOf.Add(new XElement(fullyQualifiedName));
                nodeToReturn = childOf.Descendants(fullyQualifiedName).FirstOrDefault();
            }

            return nodeToReturn;
        }
    }
}
