using System;

namespace CoreChatApi.Dtos
{
    public class LogDTO
    {
        public int? Id { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public DateTime? DateTime { get; set; }
    }
}