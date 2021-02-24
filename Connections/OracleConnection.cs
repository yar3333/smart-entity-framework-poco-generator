using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;

namespace SmartEntityFrameworkPocoGenerator
{
	class OracleConnection : BaseDatabaseConnection
    {
        private Dictionary<string, List<string>> primaryKeysCache;
        private Dictionary<string, List<string>> identitiesCache;
        
        public OracleConnection(string connectionString) : base(new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString)) {}

        public override string[] getTables()
        {
            var tables = new List<string>();
            using (var reader = connection.ExecuteReader("SELECT table_name FROM user_tables ORDER BY table_name"))
            {
                while (reader.Read())
                {
                    tables.Add(reader["table_name"].ToString());
                }
            }

            return tables.ToArray();
        }

        public override ColumnInfo[] getColumns(string table)
        {
            var fields = new List<ColumnInfo>();
            
            var sql = "SELECT column_name, data_type, data_length, nullable, data_precision, data_scale"
                    + " FROM user_tab_columns"
                    + " WHERE table_name = '" + table + "'"
                    + " ORDER BY column_id";
            
            using (var reader = connection.ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    var precision = reader["data_precision"] != DBNull.Value ? (int?)(decimal)reader["data_precision"] : null;
                    var scale = reader["data_scale"] != DBNull.Value ? (int?)(decimal)reader["data_scale"] : null;
                    
                    fields.Add(new ColumnInfo
                    {
                        name = reader["column_name"].ToString(),
                        type = dbTypeToCs(
                            reader["data_type"].ToString(), 
                            (int)(decimal)reader["data_length"], 
                            reader["nullable"].ToString() == "Y",
                            precision, 
                            scale
                        ),
                        dbType = filterDbType(reader["data_type"].ToString()),
                        precision = precision,
                        scale = scale
                    });
                }
            }

            if (primaryKeysCache == null)
            {
                primaryKeysCache = new Dictionary<string, List<string>>();
                identitiesCache = new Dictionary<string, List<string>>();

                sql = "SELECT cols.table_name, cols.column_name, allcols.data_default"
                      + " FROM all_constraints cons, all_cons_columns cols, all_tab_columns allcols"
                      + " WHERE cons.constraint_type = 'P'"
                      + " AND allcols.table_name = cols.table_name AND allcols.column_name = cols.column_name"
                      + " AND cons.constraint_name = cols.constraint_name"
                      + " AND cons.owner = cols.owner"
                      + " ORDER BY cols.position";

                using (var reader = connection.ExecuteReader(sql))
                {
                    while (reader.Read())
                    {
                        var tab = reader["table_name"].ToString();
                        var col = reader["column_name"].ToString();
                        if (!primaryKeysCache.ContainsKey(tab)) primaryKeysCache[tab] = new List<string>();
                        primaryKeysCache[tab].Add(col);
                        
                        var dataDefault = reader["data_default"].ToString();
                        if (dataDefault.EndsWith(".nextval"))
                        {
                            if (!identitiesCache.ContainsKey(tab)) identitiesCache[tab] = new List<string>();
                            identitiesCache[tab].Add(col);
                        }
                    }
                }
            }
            
            if (primaryKeysCache.ContainsKey(table))
            {
                foreach (var key in primaryKeysCache[table])
                {
                    var field = fields.FirstOrDefault(x => x.name == key);
                    if (field != null) field.primaryKey = true;
                }
            }

            if (identitiesCache.ContainsKey(table))
            {
                foreach (var key in identitiesCache[table])
                {
                    var field = fields.FirstOrDefault(x => x.name == key);
                    if (field != null) field.identity = true;
                }
            }

            return fields.ToArray();
        }

        public override string dbTypeToCs(string dataType, int len, bool nullable, int? precision, int? scale)
        {
            if (Regex.IsMatch(dataType, @"^INTERVAL DAY[(]\d+[)] TO SECOND[(]\d+[)]$")) return "TimeSpan";
            if (dataType.StartsWith("TIMESTAMP(")) return "DateTime" + (nullable ? "?" : "");

            switch (dataType)
            {
                case "RAW":
                    return len == 16 ? "Guid" + (nullable ? "?" : "") : "Byte[]";

                case "NUMBER":
                    if (scale == 0 && precision == 1) return "bool" + (nullable ? "?" : "");
                    
                    // not supported by current Oracle driver
                    //if (scale == 0 && precision >= 2 && precision <= 9) return "int" + (nullable ? "?" : "");
                    //if (scale == 0 && precision >= 10 && precision <= 18) return "long" + (nullable ? "?" : "");
                    //if (precision != null && scale != null && scale > 0 && precision > scale && precision < 16) return "double" + (nullable ? "?" : "");
                    
                    return "decimal" + (nullable ? "?" : "");

                case "CHAR":
                case "VARCHAR2":
                case "CLOB":
                case "NCLOB":
                    return "string";

                case "DATE":
                    return "DateTime" + (nullable ? "?" : "");

                case "BLOB":
                    return "Byte[]";
            }

            return dataType;
        }

        public override string tableNameToCs(string table)
        {
            var textInfo = CultureInfo.InvariantCulture.TextInfo;

            var words = table
                            .Replace("#", "_")
                            .Split('_')
                            .Select(x => string.Join("", WordSplitter.split(x).Select(y => textInfo.ToTitleCase(y.ToLowerInvariant()))));
            
            return string.Join("_", words);
        }

        public override string columnNameToCs(string name)
        {
            return tableNameToCs(name);
        }

        private string filterDbType(string s)
        {
            if (s.StartsWith("TIMESTAMP(")) return "TIMESTAMP";
            return s;
        }
    }
}
