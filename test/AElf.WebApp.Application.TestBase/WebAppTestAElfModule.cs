using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using AElf.WebApp.Web;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(WebWebAppAElfModule),
        typeof(FeeCalculationModule)
    )]
    public class WebAppTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                        s.GenerateSystemTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(new List<Transaction>()));
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraDataAsync(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockValidationService>(o =>
            {
                var mockService = new Mock<IBlockValidationService>();
                mockService.Setup(s =>
                    s.ValidateBlockBeforeAttachAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                return mockService.Object;
            });

            context.Services.AddSingleton<IAElfNetworkServer>(o => Mock.Of<IAElfNetworkServer>());
            context.Services.AddSingleton<ITxHub, MockTxHub>();

            context.Services.AddSingleton<ISmartContractRunner, UnitTestCSharpSmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new UnitTestCSharpSmartContractRunner(
                    option.Value.SdkDir);
            });
            context.Services.AddSingleton<IDefaultContractZeroCodeProvider, UnitTestContractZeroCodeProvider>();
            context.Services.AddSingleton<ISmartContractAddressService, UnitTestSmartContractAddressService>();
            context.Services
                .AddSingleton<ISmartContractAddressNameProvider, ParliamentSmartContractAddressNameProvider>();
            
            context.Services.Replace(ServiceDescriptor.Singleton<INetworkService, NetworkService>());
            
            context.Services.Replace(ServiceDescriptor.Singleton(o =>
            {
                var pool = o.GetService<IPeerPool>();
                var serverMock = new Mock<IAElfNetworkServer>();
                
                serverMock.Setup(p => p.DisconnectAsync(It.IsAny<IPeer>(), It.IsAny<bool>()))
                    .Returns(Task.CompletedTask)
                    .Callback<IPeer, bool>((peer, disc) => pool.RemovePeer(peer.Info.Pubkey));
                
                return serverMock.Object;
            }));

            context.Services.AddSingleton(provider =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(m => m.GetExtraDataFromBlockHeader(It.IsAny<string>(), It.IsAny<BlockHeader>()))
                    .Returns(ByteString.CopyFrom(new AElfConsensusHeaderInformation
                    {
                        Behaviour = AElfConsensusBehaviour.NextRound,
                        Round = new Round
                        {
                            RoundNumber = 12,
                            TermNumber = 1,
                            BlockchainAge = 3,
                            ExtraBlockProducerOfPreviousRound = "bp2-pubkey",
                            MainChainMinersRoundNumber = 3,
                            RealTimeMinersInformation =
                            {
                                {
                                    "bp1-pubkey", new MinerInRound
                                    {
                                        Order = 2,
                                        ProducedBlocks = 3,
                                        ExpectedMiningTime = TimestampHelper.GetUtcNow().AddSeconds(3),
                                        ActualMiningTimes = { },
                                        MissedTimeSlots = 1
                                    }
                                }
                            }
                        },
                        SenderPubkey = ByteString.CopyFromUtf8("pubkey")
                    }.ToByteArray()));

                return mockService.Object;
            });
            
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddTransient<ITransactionSizeFeeSymbolsProvider, TransactionSizeFeeSymbolsProvider>();
            context.Services.Replace(ServiceDescriptor.Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
        }
    }
}