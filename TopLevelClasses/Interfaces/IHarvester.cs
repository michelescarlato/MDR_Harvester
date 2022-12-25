using System.Threading.Tasks;

namespace MDR_Harvester
{
    interface IHarvester
    {
        int Run(Options opts);
    }
}