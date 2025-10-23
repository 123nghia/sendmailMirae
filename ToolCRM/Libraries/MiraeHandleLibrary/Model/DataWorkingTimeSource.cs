namespace ToolCRM.Libraries.MiraeHandleLibrary.Model
{
    public class DataWorkingTimeSource
    {
        public DataWorkingTimeSource()
        {

        }
        public string UserName { get; set; } = string.Empty;
        public string TeamLead { get; set; } = string.Empty;
    }


    public class DataWorkingTimeHandle
    {
        public string Check_In { get; set; } = string.Empty;
        public string Check_Out { get; set; } = string.Empty;
        public float Duration { get; set; }

        public string Week_day { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
