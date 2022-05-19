using System;

namespace CoreChatApi.Dtos
{
    public class ChatReadDTO
    {
        public int? Id { get; set; }
        public int UsernameId { get; set; }
        public int ChatId { get; set; }
        public DateTime? DateTime { get; set; }
    }
}