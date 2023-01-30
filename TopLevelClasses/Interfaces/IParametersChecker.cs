namespace MDR_Harvester;

internal interface IParameterChecker
{
    Options ObtainParsedArguments(string[] args);
    bool ValidArgumentValues(Options opts);
}
