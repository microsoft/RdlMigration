# RDL Migration
This is a migration Tool to convert RDL with shared Data Source to ones with Embedded Data Sourcce

## Usage:

In command window, run RdlMigration \<url> \<path> \<saveDirectory>(Optional), for example:
    
    RdlMigration http://ericpbi /Report_test/Shared/Shared_report

OR

    RdlMigration http://ericpbi /Report_test/Shared/Shared_report ./OutputFiles

The default save Directory is in ./test/

* ### If successfully converted you will see a sucessful massage in command window and a file called **S2D_Filename.rdl** in the specificed folder/Directory

* ### If error occured it will throw the error massage in the command window


## Unit Testing:

In the solution there is a unit test project called UnitTestForRdlMigration.

It reads a file  "./test.txt" (by default) and take each line as two input arguments(seperated by space) as above. It iternatively runs all lines.

 **Only one space allowed each line otherwise a error would be thrown**

Still, it would save the output file to ./test/ with all S2D_*.rdl



