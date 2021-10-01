namespace CoreChatApi.Dtos
{
    public class UserDTO
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserSqlDTO
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }
    }
}