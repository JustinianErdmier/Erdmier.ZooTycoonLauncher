global using System.Data;
global using System.Data.Common;
global using System.IO.Abstractions.TestingHelpers;

global using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;
global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.Settings;
global using Erdmier.ZooTycoonNexus.Infrastructure.Discovery;
global using Erdmier.ZooTycoonNexus.Infrastructure.Game;
global using Erdmier.ZooTycoonNexus.Infrastructure.IniConfig;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Installation;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Launcher;
global using Erdmier.ZooTycoonNexus.Infrastructure.Persistence.Launcher.Repositories;

global using Microsoft.Data.Sqlite;
global using Microsoft.EntityFrameworkCore;

global using NSubstitute;

global using Shouldly;

global using Xunit;
