global using System.Globalization;

global using Erdmier.ZooTycoonNexus.Application.Boot;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Application.Game.Launch;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Common;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Get;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.History;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Recovery;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Restore;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Save;
global using Erdmier.ZooTycoonNexus.Application.Installations.GetById;
global using Erdmier.ZooTycoonNexus.Application.Installations.Verify;
global using Erdmier.ZooTycoonNexus.Application.Settings.Common;
global using Erdmier.ZooTycoonNexus.Application.Settings.Get;
global using Erdmier.ZooTycoonNexus.Application.Settings.Save;
global using Erdmier.ZooTycoonNexus.Desktop.Composition;
global using Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Common;
global using Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.Headless;
global using Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;
global using Erdmier.ZooTycoonNexus.Desktop.Styles;
global using Erdmier.ZooTycoonNexus.Desktop.Tests.Unit.IniConfig;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Common;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Dialogs;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig.Fields;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Tabs;
global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.IniKeys;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;
global using Erdmier.ZooTycoonNexus.Domain.Settings;

global using CommunityToolkit.Mvvm.Messaging;

global using ErrorOr;

global using Mediator;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;

global using NSubstitute;

global using Shouldly;

global using Xunit;
