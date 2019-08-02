
# RdlMigration
This is a Tool that takes files from the report server and convert the shared component in the report files, save the converted files and push them to a specified Power BI Workspace.

## Usage:

    # RdlMigration <your Base url endpoint> <file Path> <WorkspaceName> <client-id>

It will save all the converted files to local disk and display the status of each file
The status would be displayed in the command window as well as a file called *ConversionLog.txt* 

**NOTE:** it will **NOT** take the correpond datasource or dataset down and thus will **NOT** push any datasource or dataset to PBI Workspace.

---
## Input details:

### Base url endpoint: 
It's usually set in the report server configuration manager under Web Service Url -> Report server web service url

![image](https://user-images.githubusercontent.com/52690905/62327114-9ae5ee00-b464-11e9-9bf1-0fe399bcd152.png)

### File Path: 
The relative path to your file or folder on the report server
if the path is a file then the tool would convert and push it only.
If the path is a folder then the tool would convert and try to push all the report files in that folder.

### Workspace Name:
The name of the workspace you want to upload your files to. use "" is there is space in the name
For example:

    "Eric's Workspace"

### client-id: 
The Application Client ID that give you permissions to read and write with Power BI API:

The way to get it is simple:

1. Go to dev.powerbi.com/apps.
   
2. Select Sign in with your existing account then select Next.

3. Provide an Application Name you want to call it.

4. Select Native for Application Type

5. Select the access permissions, for this application the minimum access are **Read all Workspaces, Read and write all datasets, Read and write all reports**

![image](https://user-images.githubusercontent.com/52690905/62328377-d9c97300-b467-11e9-8625-775a6e23c314.png)

then click Register an application ID (Client-ID) would be provided to you!
link :  https://docs.microsoft.com/en-us/power-bi/developer/register-app

**NOTE:** In some cases you may need an admin's approval for that app-id to work.

