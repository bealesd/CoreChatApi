using System;

namespace CoreChatApi.Dtos
{
    public class TodoDTO
    {
        public int? Id { get; set; }
        public string Text { get; set; }  
        public int Complete { get; set; }
        public DateTime DateTime { get; set; }
    }
}