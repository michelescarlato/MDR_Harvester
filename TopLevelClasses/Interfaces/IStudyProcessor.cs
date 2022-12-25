using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MDR_Harvester
{
    public interface IStudyProcessor
    {
        public Study? ProcessData(string json_string, DateTime? download_datetime);
    }
}
