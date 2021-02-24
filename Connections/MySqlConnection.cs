using System;
using System.Data;
using System.Linq;

namespace SmartEntityFrameworkPocoGenerator
{
	class MySqlConnection : BaseDatabaseConnection
    {
        public MySqlConnection(string connectionString) : base(new MySql.Data.MySqlClient.MySqlConnection(connectionString)) {}

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
