using MySql.Data.MySqlClient;

namespace MySqlProcess
{
    public class MySqlConnect
    {
        public static MySqlConnection Connection(string db = "")
        {
            return BanCa.Sql.SqlLogger.getConnection(db);
        }
    }
}
