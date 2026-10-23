global using System.ComponentModel;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;

global using Ardalis.SmartEnum;

global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Controls.Primitives;
global using Avalonia.Data.Converters;
global using Avalonia.Input;
global using Avalonia.Interactivity;
global using Avalonia.Markup.Xaml;
global using Avalonia.Threading;
global using Avalonia.VisualTree;

global using Classic.Avalonia.Theme;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CommunityToolkit.Mvvm.Messaging;

global using ErrorOr;

global using Mediator;

global using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
global using Erdmier.ZooTycoonNexus.Application.Common.Extensions;
global using Erdmier.ZooTycoonNexus.Application.Common.Messaging;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Application.Game.Launch;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Common;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Get;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.History;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Recovery;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Restore;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Save;
global using Erdmier.ZooTycoonNexus.Application.Installations.Add;
global using Erdmier.ZooTycoonNexus.Application.Installations.Delete;
global using Erdmier.ZooTycoonNexus.Application.Installations.GetAll;
global using Erdmier.ZooTycoonNexus.Application.Installations.GetById;
global using Erdmier.ZooTycoonNexus.Application.Installations.Relocate;
global using Erdmier.ZooTycoonNexus.Application.Installations.Update;
global using Erdmier.ZooTycoonNexus.Application.Installations.Verify;
global using Erdmier.ZooTycoonNexus.Application.Settings.Common;
global using Erdmier.ZooTycoonNexus.Application.Settings.Get;
global using Erdmier.ZooTycoonNexus.Application.Settings.Save;
global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.IniKeys;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;
global using Erdmier.ZooTycoonNexus.Domain.Settings;
global using Erdmier.ZooTycoonNexus.Desktop.Composition;
global using Erdmier.ZooTycoonNexus.Desktop.Models;
global using Erdmier.ZooTycoonNexus.Desktop.Models.IniConfig;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Common;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Dialogs;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.IniConfig.Fields;
global using Erdmier.ZooTycoonNexus.Desktop.ViewModels.Tabs;
global using Erdmier.ZooTycoonNexus.Desktop.Views.Dialogs;
global using Erdmier.ZooTycoonNexus.Infrastructure.Common.Extensions;

global using JetBrains.Annotations;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
