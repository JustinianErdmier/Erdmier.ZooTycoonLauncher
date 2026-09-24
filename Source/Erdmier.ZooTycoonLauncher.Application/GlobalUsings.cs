global using System.IO.Abstractions;

global using ErrorOr;

global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Domain.IniDocuments;
global using Erdmier.ZooTycoonLauncher.Domain.IniDrift;
global using Erdmier.ZooTycoonLauncher.Domain.IniKeys;
global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
global using Erdmier.ZooTycoonLauncher.Application.IniConfig.Common;

global using JetBrains.Annotations;

global using Mediator;

global using FluentValidation;
global using FluentValidation.Results;

global using Microsoft.Extensions.DependencyInjection;
