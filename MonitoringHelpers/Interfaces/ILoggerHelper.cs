namespace MDR_Harvester
{
    public interface ILoggerHelper
    {
        void LogCommandLineParameters(Options opts);
        void LogHeader(string header_text);
        void LogStudyHeader(Options opts, string dbline);
        void LogTableStatistics(ISource s, string schema);
    }
}