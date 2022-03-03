using System.Text.Json.Serialization;

namespace CoreChatApi.Dtos
{
    public class UserLoginDTO
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class UserSqlDTO
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }
        public string Role { get; set; }

    }
}