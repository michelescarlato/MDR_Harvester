using PostgreSQLCopyHelper;
namespace MDR_Harvester;

public interface IObjectCopyHelpers
{
    public PostgreSQLCopyHelper<DataObject> dataObjectsHelper { get; }
    public PostgreSQLCopyHelper<ObjectDataset> objectDatasetsHelper { get; }
    public PostgreSQLCopyHelper<ObjectTitle> objectTitlesHelper { get; }
    public PostgreSQLCopyHelper<ObjectInstance> objectInstancesHelper { get; }
    public PostgreSQLCopyHelper<ObjectDate> objectDatesHelper { get; }
    public PostgreSQLCopyHelper<ObjectContributor> objectContributorsHelper { get; }
    public PostgreSQLCopyHelper<ObjectIdentifier>objectIdentifiersHelper { get; }
    public PostgreSQLCopyHelper<ObjectDescription> objectDescriptionsHelper { get; }
    public PostgreSQLCopyHelper<ObjectDBLink> objectDbLinksHelper { get; }
    public PostgreSQLCopyHelper<ObjectPublicationType> objectPubTypesHelper { get; }
    public PostgreSQLCopyHelper<ObjectComment> objectCommentsHelper { get; }
    public PostgreSQLCopyHelper<ObjectTopic> objectTopicsHelper { get; }
    public PostgreSQLCopyHelper<ObjectRight> objectRightsHelper { get; }
    public PostgreSQLCopyHelper<ObjectRelationship> objectRelationshipsHelper { get; }
    
    
}