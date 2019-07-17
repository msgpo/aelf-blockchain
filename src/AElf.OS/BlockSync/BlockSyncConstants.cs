
using System.Collections.Concurrent;

namespace AElf.OS.BlockSync
{
    public class BlockSyncConstants
    {
        public const long BlockSyncFetchBlockAgeLimit = 1000;
        public const long BlockSyncAttachBlockAgeLimit = 2000;
        public const long BlockSyncAttachAndExecuteBlockAgeLimit = 500;

        public const int BlockSyncModeHeightOffset = 12;
        public const int FetchBlockRetryTimes = 3;
        public const int SyncBlockRetryTimes = 3;
        public const long BlockDownloadHeightOffset = 50;
        public const int BlockDownloadTimerPeriod = 1000;
    }
}