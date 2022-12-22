using PostgreSQLCopyHelper;
using System.Collections.Generic;

namespace DataHarvester
{
    public interface IStorageDataLayer
    {
        void StoreFullStudy(Study s, ISource source);
        void StoreFullObject(FullDataObject b, ISource source);
    }
}