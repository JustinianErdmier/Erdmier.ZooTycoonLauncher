global using System.IO.Abstractions.TestingHelpers;

global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Game;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

global using Microsoft.Data.Sqlite;
global using Microsoft.EntityFrameworkCore;

global using NSubstitute;

global using Shouldly;

global using Xunit;
