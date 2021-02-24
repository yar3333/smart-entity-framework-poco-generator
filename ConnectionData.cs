using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace SmartEntityFrameworkPocoGenerator
{
	class ConnectionData
	{
		public readonly DriverType driver;
		public readonly string connectionString;

		public readonly string database;
		public readonly string schema;

		public ConnectionData(DriverType driver, string connectionString)
		{
			this.driver = driver;
			this.connectionString = connectionString;

			switch (driver)
			{
				case DriverType.MySql:
				{
					var builder = new MySqlConnectionStringBuilder(connectionString);
					database = builder.Database;
					break;
				}
				
				case DriverType.SqlServer:
				{
					var builder = new SqlConnectionStringBuilder(connectionString);
					database = builder.InitialCatalog;
					break;
				}
				
				case DriverType.Oracle:
				{
					var builder = new OracleConnectionStringBuilder(connectionString);
                    
                    var re = new Regex("SERVICE_NAME\\s*=\\s*([a-zA-Z0-9.]+)");
                    database = re.Match(builder.DataSource).Groups[1].Value;

                    schema = builder.UserID.ToUpperInvariant();

					break;
				}
				
				default:
					throw new Exception("Unknow driver: " + driver + ".");
			}
		}

		public BaseDatabaseConnection createConnection()
		{
			switch (driver)
			{
				case DriverType.MySql:     return new MySqlConnection(connectionString);
				case DriverType.SqlServer: return new SqlServerConnection(connectionString);
				case DriverType.Oracle:    return new OracleConnection(connectionString);
			}
			throw new Exception("Unknow driver: " + driver + ".");
		}
	}
}
