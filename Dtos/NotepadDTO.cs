using System;

namespace CoreChatApi.Dtos
{
    public class NotepadDTO
    {
        public int? Id { get; set; }
        public string Text { get; set; }  
        public string Name { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }
        public DateTime Created { get; set; }
    }
}