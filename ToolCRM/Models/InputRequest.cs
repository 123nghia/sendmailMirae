namespace ToolCRM.Models
{
    public class InputRequest
    {

        public IFormFile? FileTC { get; set; }

        public IFormFile? FileReport { get; set; }

        public DateTime? DayReport { get; set; }
        public InputRequest()
        {

        }
    }
}
