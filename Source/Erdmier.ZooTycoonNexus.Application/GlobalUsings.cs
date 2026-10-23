global using System.IO.Abstractions;

global using ErrorOr;

global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.Settings;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;
global using Erdmier.ZooTycoonNexus.Domain.IniDocuments;
global using Erdmier.ZooTycoonNexus.Domain.IniDrift;
global using Erdmier.ZooTycoonNexus.Domain.IniKeys;
global using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Application.Common.Messaging;
global using Erdmier.ZooTycoonNexus.Application.Game.Launch;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Common;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.History;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Recovery;
global using Erdmier.ZooTycoonNexus.Application.Settings.Common;

global using JetBrains.Annotations;

global using Mediator;

global using FluentValidation;
global using FluentValidation.Results;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
