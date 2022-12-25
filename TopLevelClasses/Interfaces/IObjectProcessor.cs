using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MDR_Harvester
{
    public interface IObjectProcessor
    {
        public FullDataObject ProcessData(XmlDocument d, DateTime? download_datetime);

    }
}
