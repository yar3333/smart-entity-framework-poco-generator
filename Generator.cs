using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartEntityFrameworkPocoGenerator
{
	class Generator
	{
		public static void run(ConnectionData connData, string destFolder, string ns, string contextClassName, IEnumerable<string> ignoreTables, IEnumerable<string> useTables, List<string> identities, List<string> forceFieldNames)
        {
            Directory.CreateDirectory(destFolder);
            identities = identities.Select(x => x.ToLowerInvariant()).ToList();

            var connection = connData.createConnection();

            var tables = connection.getTables().Where(x => !ignoreTables.Contains(x) && (!useTables.Any() || useTables.Contains(x))).ToArray();

             foreach (var table in tables)
             {
                Console.WriteLine("Generate DTO for table '" + table + "' => " +  connection.tableNameToCs(table) + ".cs");
                
                var entityLines = new List<string>();

                var className = WordSplitter.toOne(connection.tableNameToCs(table));
                
                entityLines.Add("using System;");
                entityLines.Add("using System.Collections.Generic;");
                entityLines.Add("using System.ComponentModel.DataAnnotations;");
                entityLines.Add("using System.ComponentModel.DataAnnotations.Schema;");
                entityLines.Add("");
                entityLines.Add("namespace " + ns);
                entityLines.Add("{");
                entityLines.Add("\t[Table(\"" + table + "\")]");
                entityLines.Add("\tpublic class " + className);
                entityLines.Add("\t{");

                var colOrder = 0;
                var columns = connection.getColumns(table);
                var primaryKeyColumnCount = columns.Count(x => x.primaryKey);
                foreach (var col in columns)
                {
                    entityLines.Add("\t\t[" 
                                    + (col.primaryKey ? "Key, " : "") 
                                    + "Column(\"" + col.name + "\""
                                    + (primaryKeyColumnCount == 0 || (primaryKeyColumnCount > 1 && col.primaryKey) ? ", Order = " + colOrder : "")
                                    + ", TypeName = \"" + col.dbType + "\""
                                    + ")]");
                    if (col.identity || identities.Contains((table + "." + col.name).ToLowerInvariant())) entityLines.Add("\t\t[DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                    entityLines.Add("\t\tpublic " + col.type + " " + getFieldName(table, col, connection, forceFieldNames) + " { get; set; }");
                    entityLines.Add("");
                    colOrder++;
                }

                entityLines.RemoveAt(entityLines.Count - 1);
                entityLines.Add("\t}");
                entityLines.Add("}");
                
                File.WriteAllLines(Path.Combine(destFolder, className + ".cs"), entityLines);
            }

            if (contextClassName != null)
            {
                Console.WriteLine("Generate database context => " + contextClassName + ".cs");
            
                var dbContextLines = new List<string>();
                dbContextLines.Add("using System;");
                dbContextLines.Add("using System.Data.Entity;");
                dbContextLines.Add("");
                dbContextLines.Add("namespace " + ns);
                dbContextLines.Add("{");
                dbContextLines.Add("\tpublic class " + contextClassName + " : DbContext");
                dbContextLines.Add("\t{");
                foreach (var table in tables)
                {
                    dbContextLines.Add("\t\tpublic DbSet<" + WordSplitter.toOne(connection.tableNameToCs(table)) + "> " + WordSplitter.toMany(connection.tableNameToCs(table)) + " { get; set; }");
                }
                dbContextLines.Add("");
                dbContextLines.Add("\t\tpublic " + contextClassName + "(string connectionString) : base(connectionString)");
                dbContextLines.Add("\t\t{");
                dbContextLines.Add("\t\t\tSystem.Data.Entity.Database.SetInitializer<" + contextClassName + ">(null);");

                dbContextLines.Add("\t\t}");
                dbContextLines.Add("");
                dbContextLines.Add("\t\tprotected override void OnModelCreating(DbModelBuilder modelBuilder)");
                dbContextLines.Add("\t\t{");
                if (connData.schema != null) dbContextLines.Add("\t\t\tmodelBuilder.HasDefaultSchema(\"" + connData.schema + "\");");
                
                foreach (var table in tables)
                {
                    var decimalColumns = connection.getColumns(table).Where(x => x.type == "decimal" && (x.precision != null || x.scale != null)).ToArray();
                    if (decimalColumns.Length > 0)
                    {
                        dbContextLines.Add("");
                        foreach (var col in decimalColumns)
                        {
                            var className = WordSplitter.toOne(connection.tableNameToCs(table));
                            // do not specify real precision due to mapping errors by Oracle driver (bug in driver?)
                            dbContextLines.Add("\t\t\tmodelBuilder.Entity<" + className + ">().Property(x => x." + getFieldName(table, col, connection, forceFieldNames) + ").HasPrecision(" + (/*col.precision ?? */38) + (col.scale != null ? ", " + col.scale : "") + ");");
                        }
                    }
                }
                
                dbContextLines.Add("\t\t}");
                dbContextLines.Add("\t}");
                dbContextLines.Add("}");

                File.WriteAllLines(Path.Combine(destFolder, contextClassName + ".cs"), dbContextLines);
            }
		}

        static string wordManyToOne(string word)
        {
            if (word.Length > 2 && word.EndsWith("es")) return word.Substring(0, word.Length - "es".Length);
            if (word.Length > 1 && word.EndsWith("s")) return word.Substring(0, word.Length - "s".Length);

            if (word.EndsWith("People")) return word.Substring(0, word.Length - "People".Length) + "Person";
            
            return word;
        }

        static string getFieldName(string table, ColumnInfo column, BaseDatabaseConnection connection, List<string> forceFieldNames)
        {
            var forceFielName = forceFieldNames.Find(x => x.StartsWith(table + "." + column.name + ":"));
            if (forceFielName != null) return forceFielName.Split(':')[1];
            return connection.columnNameToCs(column.name);
        }
	}
}
