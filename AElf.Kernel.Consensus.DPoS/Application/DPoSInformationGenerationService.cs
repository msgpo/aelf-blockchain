﻿using System;
using System.Diagnostics;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class DPoSInformationGenerationService : IConsensusInformationGenerationService
    {
        private readonly DPoSOptions _dpoSOptions;
        private readonly IAccountService _accountService;
        private readonly ConsensusControlInformation _controlInformation;
        // TODO: Add RSAPublicKey, put in value to contract.
        private Hash _inValue;

        public ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public DPoSHint Hint => DPoSHint.Parser.ParseFrom(_controlInformation.ConsensusCommand.Hint);

        public ILogger<DPoSInformationGenerationService> Logger { get; set; }

        public DPoSInformationGenerationService(IOptions<DPoSOptions> consensusOptions, IAccountService accountService,
            ConsensusControlInformation controlInformation)
        {
            _dpoSOptions = consensusOptions.Value;
            _accountService = accountService;
            _controlInformation = controlInformation;

            Logger = NullLogger<DPoSInformationGenerationService>.Instance;
        }

        public IMessage GetTriggerInformation()
        {
            if (_controlInformation.ConsensusCommand == null)
            {
                return new DPoSTriggerInformation
                {
                    IsBootMiner = _dpoSOptions.IsBootMiner,
                    PublicKey = PublicKey,
                    InitialTermNumber = _dpoSOptions.InitialTermNumber
                };
            }

            switch (Hint.Behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                        Miners = {_dpoSOptions.InitialMiners},
                        InitialTermNumber = _dpoSOptions.InitialTermNumber,
                        IsBootMiner = _dpoSOptions.IsBootMiner,
                        MiningInterval = _dpoSOptions.MiningInterval
                    };
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                    // First Round.
                    if (_inValue == null)
                    {
                        _inValue = Hash.Generate();
                    }
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                        PreviousInValue = Hash.Empty,
                        CurrentInValue = _inValue
                    };
                case DPoSBehaviour.UpdateValue:
                    if (_inValue == null)
                    {
                        _inValue = Hash.Generate();
                    }
                    var previousInValue = _inValue;
                    _inValue = Hash.Generate();
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                        PreviousInValue = previousInValue,
                        CurrentInValue = _inValue
                    };
                case DPoSBehaviour.NextRound:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                    };
                case DPoSBehaviour.NextTerm:
                    return new DPoSTriggerInformation
                    {
                        PublicKey = PublicKey,
                    };
                case DPoSBehaviour.Invalid:
                    throw new InvalidOperationException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IMessage ParseConsensusTriggerInformation(byte[] consensusTriggerInformation)
        {
            return DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);
        }
    }
}