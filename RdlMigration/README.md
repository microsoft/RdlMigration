# RDL Migration
This is a migration Tool to convert RDL with shared Data Source to ones with Embedded Data Sourcce

## Usage:

In command window, run RdlMigration \<urlEndpoint> \<path> \<workspaceName> \<clientID>, for example:
    
    RdlMigration https://rosereports/reportserver /Rosetta/EricReports EricTestWorkspace 7ff93811-bdad-4eb8-ac81-72b442e07572


The default save Directory is in ./test/

* ### If successfully converted you will see a sucessful massage in command window and a file called **S2D_Filename.rdl** in the specificed folder/Directory

* ### If error occured it will throw the error massage in the command window


## Unit Testing:

In the solution there is a unit test project called UnitTestForRdlMigration.

It reads files in TestFiles folder and take files as the report components and do the conversion

 **The unit test Only check the conversion**

