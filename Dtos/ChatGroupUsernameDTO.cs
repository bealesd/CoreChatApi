using System;
using System.Collections.Generic;

namespace CoreChatApi.Dtos
{
    public class ChatGroupUsernameDTO
    {
        public List<string> Usernames { get; set; }
        public Guid Guid { get; set; }
    }
}