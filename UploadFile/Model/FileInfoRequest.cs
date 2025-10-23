namespace UploadFile.Model
{
    public class FileInfoRequest
    {
        public IFormFile? FileInfo { get; set; }
        public string? DateRequest { get; set; }
        public FileInfoRequest()
        {
        }
    }
}
