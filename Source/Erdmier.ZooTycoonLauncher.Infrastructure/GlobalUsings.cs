global using System.IO.Abstractions;

global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Common.Storage;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Discovery;
global using Erdmier.ZooTycoonLauncher.Infrastructure.IniSnapshots;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Logging;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Installation;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher;
global using Erdmier.ZooTycoonLauncher.Infrastructure.Persistence.Launcher.Repositories;

global using ErrorOr;

global using JetBrains.Annotations;

global using Microsoft.Data.Sqlite;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.Extensions.DependencyInjection;

global using Serilog;
