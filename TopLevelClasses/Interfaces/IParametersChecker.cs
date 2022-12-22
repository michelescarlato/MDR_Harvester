using System.Threading.Tasks;

namespace DataHarvester
{
    internal interface IParametersChecker
    {
        Options ObtainParsedArguments(string[] args);
        bool ValidArgumentValues(Options opts);
    }
}