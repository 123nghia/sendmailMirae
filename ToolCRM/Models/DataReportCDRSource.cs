namespace ToolCRM.Models
{
    public class DataReportCDRSource
    {
        public DataReportCDRSource()
        {

        }
        public string UserName { get; set; } = string.Empty;
        public string TeamLead { get; set; } = string.Empty;
        public string Agreement { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public string PromiseDate { get; set; } = string.Empty;
        public string PromiseAmt { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string CallDate { get; set; } = string.Empty;
        public string? Time { get; set; }
        public string? Contact_Person { get; set; }
    }
}
