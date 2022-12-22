using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace DataHarvester
{
    public interface IStudyProcessor
    {
        public Study ProcessData(XmlDocument d, DateTime? download_datetime);
    }
}
