using System;

namespace CoreChatApi.Dtos
{
    public class ChatGroupDTO
    {
        public int? Id { get; set; }
        public int UsernameId { get; set; }
        public Guid Guid { get; set; }
    }
}