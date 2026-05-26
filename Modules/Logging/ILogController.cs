using System.Collections.Generic;

namespace Build1.PostMVC.Unity.App.Modules.Logging
{
    public interface ILogController
    {
        void RecordMessage(string message, LogLevel level, bool forceFlush);

        string GetLog();
        void   FlushLog();
        void   ClearLogs();

        List<LogFile> GetLogFiles();
        LogFile       GetLastLogFile();
        void          DeleteLogFiles();
    }
}
