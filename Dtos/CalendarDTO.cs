using System;

namespace CoreChatApi.Dtos
{
    public class CalendarDTO
    {
        public int? Id { get; set; }
        public string What { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
    }
}