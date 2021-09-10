using System;

namespace CoreChatApi.Dtos
{
    public class ChatDTO
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTime? DateTime { get; set; }
    }
}