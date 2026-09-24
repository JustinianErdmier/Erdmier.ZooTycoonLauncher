global using System.ComponentModel;
global using System.Globalization;

global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Data.Converters;
global using Avalonia.Interactivity;
global using Avalonia.Markup.Xaml;
global using Avalonia.Threading;

global using Classic.Avalonia.Theme;

global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CommunityToolkit.Mvvm.Messaging;

global using ErrorOr;

global using Mediator;

global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Extensions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Messaging;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Game.Launch;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Add;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Delete;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetById;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Update;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Verify;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Desktop.Composition;
global using Erdmier.ZooTycoonLauncher.Desktop.Models;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Boot;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Common;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Dialogs;
global using Erdmier.ZooTycoonLauncher.Desktop.ViewModels.Tabs;
global using Erdmier.ZooTycoonLauncher.Desktop.Views.Dialogs;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Extensions;

global using JetBrains.Annotations;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
