global using System.IO.Abstractions;
global using System.Text;

global using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;
global using Erdmier.ZooTycoonNexus.Domain.IniKeys;
global using Erdmier.ZooTycoonNexus.Domain.Settings;
global using Erdmier.ZooTycoonNexus.Infrastructure.Common.Storage;
global using Erdmier.ZooTycoonNexus.Infrastructure.Discovery;
global using Erdmier.ZooTycoonNexus.Infrastructure.Game;
global using Erdmier.ZooTycoonNexus.Infrastructure.IniConfig;
global using Erdmier.ZooTycoonNexus.Infrastructure.Logging;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Installation;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Launcher;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Launcher.Repositories;

global using ErrorOr;

global using JetBrains.Annotations;

global using Microsoft.Data.Sqlite;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.EntityFrameworkCore.Storage;
global using Microsoft.Extensions.DependencyInjection;

global using Serilog;
