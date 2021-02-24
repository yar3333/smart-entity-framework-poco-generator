namespace SmartEntityFrameworkPocoGenerator
{
    class ColumnInfo
    {
        public string type;
        public string name;
        public bool primaryKey;
        public string dbType;
        public bool identity;

        public int? precision;
        public int? scale;
    }
}
