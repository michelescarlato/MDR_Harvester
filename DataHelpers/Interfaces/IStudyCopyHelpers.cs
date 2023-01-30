using PostgreSQLCopyHelper;
namespace MDR_Harvester;

public interface IStudyCopyHelpers
{
    public PostgreSQLCopyHelper<StudyIdentifier> studyIdentifiersHelper { get; }
    public PostgreSQLCopyHelper<StudyTitle> studyTitlesHelper { get; }
    public PostgreSQLCopyHelper<StudyTopic> studyTopicsHelper { get; }
    public PostgreSQLCopyHelper<StudyCondition> studyConditionsHelper { get; }
    public PostgreSQLCopyHelper<StudyIEC> studyIECHelper { get; }
    public PostgreSQLCopyHelper<StudyContributor> studyContributorsHelper { get; }
    public PostgreSQLCopyHelper<StudyRelationship> studyRelationshipsHelper { get; }
    public PostgreSQLCopyHelper<StudyLink> studyLinksHelper { get; }
    public PostgreSQLCopyHelper<StudyFeature> studyFeaturesHelper { get; }
    public PostgreSQLCopyHelper<StudyReference> studyReferencesHelper { get; }
    public PostgreSQLCopyHelper<StudyLocation> studyLocationsHelper { get; }
    public PostgreSQLCopyHelper<StudyCountry> studyCountriesHelper { get; }
    public PostgreSQLCopyHelper<AvailableIPD> studyAvailIPDHelper { get; }
}