// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;

using static RdlMigration.ElementNameConstants;

namespace RdlMigration
{
    /// <summary>
    /// The class of accessing the Power BI Api.
    /// </summary>
    public sealed class PowerBIClientWrapper
    {
        public string ClientId { get; set; }

        // the selected workspace. if null then means "My Workspace" is added.
        private Group workspace;

        private HashSet<string> workspaceReports = new HashSet<string>();

        private PowerBIClient client;
        private IImportsOperations importClient;
        private IReportsOperations reportsClient;
        private IGroupsOperations groupsClient;

        public PowerBIClientWrapper(string workspaceName, string clientId)
        {
            this.ClientId = clientId;
            InitializeClients();
            GetWorkspaces(workspaceName);
        }

        public PowerBIClientWrapper(string workspaceName, string clientId, IImportsOperations importClient, IReportsOperations reportsClient, IGroupsOperations groupsClient)
        {
            this.ClientId = clientId;
            this.importClient = importClient;
            this.reportsClient = reportsClient;
            this.groupsClient = groupsClient;
            GetWorkspaces(workspaceName);
        }

        /// <summary>
        /// Pops up the log-in window for user to log in with their Microsoft account.
        /// </summary>
        /// <returns> an Authentication result retrieved from the server.</returns>
        public AuthenticationResult DoInteractiveSignIn()
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(PowerBIWrapperConstants.AuthorityUri, false, new TokenCache());
            AuthenticationResult userAuthnResult = authenticationContext.AcquireTokenAsync(
                PowerBIWrapperConstants.ResourceUrl,
                this.ClientId,
                new Uri(PowerBIWrapperConstants.RedirectUrl),
                new PlatformParameters(PromptBehavior.Auto)).Result;

            return userAuthnResult;
        }

        /// <summary>
        /// Upload the report file stream to default workspace under the logged in account that was set when
        /// constructing the class.
        /// </summary>
        /// <param name="fileName">The file name displayed after uploaded to the workspace.</param>
        /// <param name="file">the file stream itself.</param>
        /// <returns> true if success, false if conflict. </returns>
        public bool UploadRDL(string fileName, Stream file)
        {
            if (!ExistReport(fileName))
            {
                if (workspace == null)
                {
                    importClient.PostImportWithFile(file, fileName, ImportConflictHandlerMode.Abort);
                }
                else
                {
                    importClient.PostImportWithFileInGroup(workspace.Id, file, fileName, ImportConflictHandlerMode.Abort);
                }
                workspaceReports.Add(fileName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// if the default workspace contains a specific report.
        /// </summary>
        /// <param name="reportName">the name of the report.</param>
        /// <returns>true if the report exists.</returns>
        public bool ExistReport(string reportName)
        {
            return workspaceReports.Contains(Path.GetFileNameWithoutExtension(reportName));
        }

        private void InitializeClients()
        {
            AuthenticationResult result = DoInteractiveSignIn();
            client = new PowerBIClient(new Uri(PowerBIWrapperConstants.PowerBiApiUri), new TokenCredentials(result.AccessToken));

            importClient = new ImportsOperations(client);
            reportsClient = new ReportsOperations(client);
            groupsClient = new GroupsOperations(client);
        }

        private Group GetWorkspaces(string workspaceName)
        {
            if (workspaceName == PowerBIWrapperConstants.MyWorkspace)
            {
                workspace = null;
                var reportNames = reportsClient.GetReports().Value.Select(report => report.Name);
                workspaceReports = new HashSet<string>(reportNames);
                return null;
            }
            var workspaces = groupsClient.GetGroups().Value;
            var groups = workspaces.Where(g => (g.Name == workspaceName));
            if (groups.Count() == 1)
            {
                workspace = groups.First();
                var reportNames = reportsClient.GetReportsInGroup(workspace.Id).Value.Select(report => report.Name);
                workspaceReports = new HashSet<string>(reportNames);
                return workspace;
            }
            else if (groups.Count() == 0)
            {
                throw new Exception($"WORKSPACE {workspaceName} NOT FOUND.  Please make sure it is a valid workspace");
            }
            else
            {
                throw new Exception($"MULTIPLE WORKSPACE {workspaceName} FOUND. This should not happen, make sure you have valid workspaces");
            }
        }
    }
}
