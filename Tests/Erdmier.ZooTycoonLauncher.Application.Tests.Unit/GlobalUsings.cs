global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Erdmier.ZooTycoonLauncher.Application.Common.Abstractions;
global using Erdmier.ZooTycoonLauncher.Application.Common.Models;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Add;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Delete;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetAll;
global using Erdmier.ZooTycoonLauncher.Application.Installations.GetById;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Locate;
global using Erdmier.ZooTycoonLauncher.Application.Installations.SetDefault;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Relocate;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Update;
global using Erdmier.ZooTycoonLauncher.Application.Installations.Verify;
global using Erdmier.ZooTycoonLauncher.Application.Boot;
global using Erdmier.ZooTycoonLauncher.Domain.Installations;
global using Erdmier.ZooTycoonLauncher.Domain.Settings;

global using FluentValidation;
global using FluentValidation.Results;

global using ErrorOr;

global using NSubstitute;

global using Shouldly;

global using Xunit;
