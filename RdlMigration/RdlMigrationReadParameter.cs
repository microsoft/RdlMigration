using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdlMigration
{
    internal class RdlMigrationReadParameter
    {
        public static void RunWithFile(string testFilePath, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            int counter = -1;
            foreach (var line in File.ReadAllLines(@testFilePath))
            {
                counter++;
                if (line.IndexOf(' ') != line.LastIndexOf(' '))
                {
                    Console.WriteLine("Line " + counter + " not valid");
                    continue;
                }

                string url = line.Substring(0, line.IndexOf(' '));
                string path = line.Substring(line.IndexOf(' ') + 1, line.Length - line.IndexOf(' ') - 1);
                try
                {
                    var app = new ConvertRDL();
                    app.RunSoap(url, path, "./test/");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Convertion for line " + counter + "Failed because:");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }
    }
}
