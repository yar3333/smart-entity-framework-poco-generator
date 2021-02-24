using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SmartEntityFrameworkPocoGenerator
{
	[JsonConverter(typeof(StringEnumConverter))]
	enum DriverType
	{
		MySql = 0,
		SqlServer = 1,
		Oracle = 3,
	}
}
