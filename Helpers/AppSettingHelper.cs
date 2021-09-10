using Microsoft.Extensions.Configuration;

namespace CoreChatApi.Helpers
{
    public static class AppSettingHelper
    {
        public static IConfiguration AddDatabaseConnectionString(this IConfiguration config)
        {
            config["ConnectionStrings:db"] = @$"
                Server={config.GetConnectionString("server")};
                Initial Catalog={config.GetConnectionString("database")};
                Persist Security Info=False;
                User ID={config.GetConnectionString("username")};
                Password={config.GetConnectionString("password")};
                MultipleActiveResultSets=False;
                Encrypt=True;TrustServerCertificate=True;
                Connection Timeout=60;";
            return config;
        }
    }
}