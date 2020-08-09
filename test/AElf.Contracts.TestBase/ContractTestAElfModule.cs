using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule)
    )]
    public class ContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            var transactionList = new List<Transaction>
            {
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = SampleAddress.AddressList[1],
                    MethodName = "GenerateConsensusTransactions"
                }
            };
            services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerator>();
                mockService.Setup(m =>
                    m.GenerateTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()));

                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(m =>
                        m.GenerateSystemTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(transactionList));

                return mockService.Object;
            });

            services.AddTransient<IBlockExecutingService, TestBlockExecutingService>();


            //For BlockExtraDataService testing.
            services.AddTransient(
                builder =>
                {
                    var dataProvider = new Mock<IBlockExtraDataProvider>();

                    ByteString bs = ByteString.CopyFrom(BitConverter.GetBytes(long.MaxValue - 1));

                    dataProvider.Setup(m => m.GetBlockHeaderExtraDataAsync(It.IsAny<BlockHeader>()))
                        .Returns(Task.FromResult(bs));

                    dataProvider.Setup(d => d.BlockHeaderExtraDataKey).Returns("TestExtraDataKey");

                    return dataProvider.Object;
                });
            services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m => m.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(default(Address)));
                return mockService.Object;
            });

            services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            services.AddSingleton(o => Mock.Of<IPeerPool>());

            services.AddSingleton(o => Mock.Of<INetworkService>());

            // When testing contract and packaging transactions, no need to generate and schedule real consensus stuff.
            context.Services.AddSingleton(o => Mock.Of<IConsensusService>());
            context.Services.AddSingleton(o => Mock.Of<IConsensusScheduler>());

            var ecKeyPair = CryptoHelper.GenerateKeyPair();

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelper.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });
            
            context.Services.RemoveAll<IPreExecutionPlugin>();
            
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
            context.Services
                .AddSingleton<ISmartContractAddressNameProvider, CrossChainSmartContractAddressNameProvider>();
        }
    }
}