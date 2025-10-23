namespace MiraeHandleReport
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var fileImport = new HanleFileExcel();
            fileImport.OutPutFile();
            var fileImport2 = new HandleFileWorkingTime();
            fileImport2.OutputFileWorkingTime();
        }
    }
}
