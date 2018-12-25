using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;

namespace AElf.Kernel.Storage
{
    public class TransactionStore : KeyValueStoreBase
    {
        public TransactionStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.TransactionPrefix)
        {
        }
    }
}
