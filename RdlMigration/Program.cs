// Copyright (c) 2019 Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License (MIT)

using System;
using System.Linq;

namespace RdlMigration
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Count() == 4)
            {
                var app = new ConvertRDL();
                try
                {
                    app.ConvertFolder(args[0], args[1], args[2], args[3]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.Read();
                }
            }
            else
            {
                DisplayUsage();
            }

            return;
        }

        private static void DisplayUsage()
        {
            Console.WriteLine(@"Usage:  # RdlMigration <your Base url endpoint> <file Path> <WorkspaceName> <client-id>
For example: to push http://basepath/Reports/browse/Reports_test/test01.rdl to MyWorkspace, RUN: 
        RdlMigration http://basepath/reportserver /Report_test/test01 MyWorkspace client_id");
            return;
        }
    }
}
