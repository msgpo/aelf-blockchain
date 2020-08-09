using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.OS;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    public class OSCoreWithChainTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).ToBase58();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            Configure<ConsensusOptions>(o =>
            {
                var miners = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    miners.Add(CryptoHelper.GenerateKeyPair().PublicKey.ToHex());
                }

                o.InitialMinerList = miners;
                o.MiningInterval = 4000;
                o.PeriodSeconds = 604800;
                o.MinerIncreaseInterval = 31536000;
            });

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelper.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });

            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            
            Configure<NetworkOptions>(o=>
            {
                o.PeerInvalidTransactionLimit = 5;
                o.PeerInvalidTransactionTimeout = 1000;
                o.PeerDiscoveryMaxNodesToKeep = 5;
                o.MaxPeers = 5;
            });
        }
    }
    
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class BenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }

    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(KernelAElfModule),
        typeof(CSharpRuntimeAElfModule)
    )]
    public class MiningBenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            context.Services.AddTransient<AccountService>();
            context.Services.AddTransient(o => Mock.Of<IConsensusService>());
            // Configure<TransactionOptions>(options => options.EnableTransactionExecutionValidation = false);
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
            });
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var keyPairProvider = context.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
            keyPairProvider.SetKeyPair(CryptoHelper.GenerateKeyPair());
        }
    }

    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class BenchmarkParallelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}