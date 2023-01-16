using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDR_Harvester.Pubmed
{
    internal class PubMedHelpers
    {
        // Two check routines that scan previously extracted Identifiers or Dates, to 
        // indicate if the input Id / Date type has already beenm extracted.

        public bool IdNotPresent(List<ObjectIdentifier> ids, int id_type, string id_value)
        {
            bool to_add = true;
            if (ids.Count > 0)
            {
                foreach (ObjectIdentifier id in ids)
                {
                    if (id.identifier_type_id == id_type && id.identifier_value == id_value)
                    {
                        to_add = false;
                        break;
                    }
                }
            }
            return to_add;
        }

        public bool DateNotPresent(List<ObjectDate> dates, int datetype_id, int? year, int? month, int? day)
        {
            bool to_add = true;
            if (dates.Count > 0)
            {
                foreach (ObjectDate d in dates)
                {
                    if (d.date_type_id == datetype_id
                        && d.start_year == year && d.start_month == month && d.start_day == day)
                    {
                        to_add = false;
                        break;
                    }
                }
            }
            return to_add;
        }

    }
}
