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

namespace RdlMigration.UnitTest
{
    [TestClass]
    public class PowerBIClientWrapperTests
    {
        [TestMethod]
        public void TestClient_SpecificWorkspace()
        {
            var sut = CreateMockOperations("testWorkspace");
            Assert.IsTrue(sut.UploadRDL(String.Empty, new MemoryStream()));
        }

        [TestMethod]
        public void TestClient_MyWorkspace()
        {
            var sut = CreateMockOperations("My Workspace");
            Assert.IsTrue(sut.UploadRDL(String.Empty, new MemoryStream()));
        }

        [TestMethod]
        public void TestClient_NoWorkspaceFound()
        {
            var workspaceName = "Workspace";
              try
            {
                var sut = CreateMockOperations(workspaceName);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == $"WORKSPACE {workspaceName} NOT FOUND.  Please make sure it is a valid workspace");
            }
        }

        [TestMethod]
        public void TestClient_NotPremiumWorkspace()
        {
            var workspaceName = "NonPremiumWorkspace";
            try
            {
                var sut = CreateMockOperations(workspaceName);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == $"WORKSPACE {workspaceName} IS NOT A PREMIUM WORKSPACE. Only premium workspaces can upload reports");
            }
        }

        private PowerBIClientWrapper CreateMockOperations(string workspaceName)
        {
            var importOperationResult = System.Threading.Tasks.Task.FromResult(new HttpOperationResponse<Import>());
            var groupList = new List<Group>();
            groupList.Add(new Group(new Guid(), "testWorkspace"));
            groupList.Add(new Group(new Guid(), "NonPremiumWorkspace", isOnDedicatedCapacity: false));
            var groupOperationResult = System.Threading.Tasks.Task.FromResult(new HttpOperationResponse<Groups>()
            {
                Body = new Groups()
                {
                    Value = groupList
                }
            }
            );

            var reportOperationResult = System.Threading.Tasks.Task.FromResult(new HttpOperationResponse<Reports>()
            {
                Body = new Reports()
                {
                    Value = new List<Report>()
                }
            });

            var importClientOutput = Mock.Of<IImportsOperations>(p =>
                p.PostImportFileWithHttpMessage(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<ImportConflictHandlerMode?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()) == importOperationResult &&
                p.PostImportFileWithHttpMessage(
                    It.IsAny<Guid>(),
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<ImportConflictHandlerMode?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()) == importOperationResult
                    );
            var reportClientOutput = Mock.Of<IReportsOperations>(p =>
                p.GetReportsWithHttpMessagesAsync(
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()) == reportOperationResult &&
                p.GetReportsInGroupWithHttpMessagesAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()) == reportOperationResult);
            var groupsClientOutput = Mock.Of<IGroupsOperations>(p =>
                p.GetGroupsWithHttpMessagesAsync(
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()) == groupOperationResult
                    );

            return new PowerBIClientWrapper(workspaceName, String.Empty, importClientOutput, reportClientOutput, groupsClientOutput);
        }
    }
}
