using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SmartEntityFrameworkPocoGenerator
{
	class SqlServerConnection : BaseDatabaseConnection
    {
        public SqlServerConnection(string connectionString) : base(new SqlConnection(connectionString)) {}

        public override string[] getTables()
        {
            return connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(row => (string)row[2]).ToArray();
		}

        public override ColumnInfo[] getColumns(string table)
        {
            throw new NotImplementedException();
        }
	}
}
