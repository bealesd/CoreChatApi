using Microsoft.AspNetCore.Http;

namespace CoreChatApi.Dtos
{
    public class Profile
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public IFormFile Picture { get; set; }  
    }
    public class ProfileDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public byte[] Picture { get; set; }  
        public string PictureType { get; set; }
        public string PictureName { get; set; }
    }
}