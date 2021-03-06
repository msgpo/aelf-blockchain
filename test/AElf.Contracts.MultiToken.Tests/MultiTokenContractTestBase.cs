using System.Collections.Generic;
using System.Linq;
using Acs2;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.Parliament;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;
using AElf.ContractTestBase.ContractTestKit;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
    {
        protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
        protected long BobCoinTotalAmount => 1_000_000_000_0000L;
        protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
        protected Address DefaultAddress => Accounts[0].Address;
        protected ECKeyPair User1KeyPair => Accounts[10].KeyPair;
        protected Address User1Address => Accounts[10].Address;
        protected Address User2Address => Accounts[11].Address;
        protected const string DefaultSymbol = "ELF";
        
        protected const string SymbolForTest = "ELF";

        protected const long Amount = 100;
        
        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();
        
        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
        internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;
        
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub;
        
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub;
        
        protected Hash BasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction");
        protected Address BasicFunctionContractAddress { get; set; }
        internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }

        protected Hash OtherBasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.OtherBasicFunction");
        protected Address OtherBasicFunctionContractAddress { get; set; }
        internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        
        public MultiTokenContractTestBase()
        {
            TokenContractStub = GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);
            Acs2BaseStub = GetTester<ACS2BaseContainer.ACS2BaseStub>(TokenContractAddress, DefaultKeyPair);

            TreasuryContractStub = GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                DefaultKeyPair);
            
            TokenConverterContractStub =  GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                DefaultKeyPair);

            BasicFunctionContractAddress = SystemContractAddresses[BasicFunctionContractName];
            BasicFunctionContractStub = GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
                BasicFunctionContractAddress, DefaultKeyPair);
           
            OtherBasicFunctionContractAddress = SystemContractAddresses[OtherBasicFunctionContractName];
            OtherBasicFunctionContractStub = GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
                OtherBasicFunctionContractAddress, DefaultKeyPair);

            ParliamentContractStub = GetTester<ParliamentContractContainer.ParliamentContractStub>(
                ParliamentContractAddress, DefaultKeyPair);
        }
        
        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }
    }
}