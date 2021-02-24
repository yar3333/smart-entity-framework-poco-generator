using System;
using System.Collections.Generic;
using SmartCommandLineParser;

namespace SmartEntityFrameworkPocoGenerator
{
	class Program
	{
		static int Main(string[] args)
        {
            var options = new CommandLineOptions();

            options.AddRequired<DriverType>("driverType", help: "Database driver type: 'oracle'.");
            options.AddRequired<string>("connectionString", help: "Database connection string.");
            options.AddOptional<string>("destFolder",  "out", "--destFolder", "Destination folder for generated files. Default is 'out'.");
            options.AddOptional<string>("namespace",  "DTO", "--namespace", "Result C# namespace. Default is 'DTO'.");
            options.AddOptional<string>("contextClass",  null, "--contextClass", "Desired database context class name.\nIf not specified, that class will not be generated.");
            options.AddRepeatable<string>("tables", "--table", "Table name to generate DTO class.\nBy default all tables are included.");
            options.AddRepeatable<string>("ignoreTables", "--ignoreTable", "Table name to ignore.");
            options.AddRepeatable<string>("identities", "--identity", "Specify identity (autoincrement) column in `table.column` format.");
            options.AddRepeatable<string>("forceFieldNames", "--field-name", "Specify field name for column in `table.column:field` format.");

            try
            {
                options.Parse(args);
            }
            catch (CommandLineParserException e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine("Usage: SmartEntityFrameworkPocoGenerator <options> <driverType> <connectionString>");
                Console.WriteLine("Details:");
                Console.WriteLine(options.GetHelpMessage());
                return 1;
            }
            
            Generator.run
            (
                new ConnectionData(options.Get<DriverType>("driverType"), options.Get<string>("connectionString")), 
                options.Get<string>("destFolder"),
                options.Get<string>("namespace"),
                options.Get<string>("contextClass"),
                options.Get<List<string>>("ignoreTables"),
                options.Get<List<string>>("tables"),
                options.Get<List<string>>("identities"),
                options.Get<List<string>>("forceFieldNames")
            );
            return 0;
        }
	}
}
