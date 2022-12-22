using System.Threading.Tasks;

namespace DataHarvester
{
    interface IHarvester
    {
        int Run(Options opts);
    }
}