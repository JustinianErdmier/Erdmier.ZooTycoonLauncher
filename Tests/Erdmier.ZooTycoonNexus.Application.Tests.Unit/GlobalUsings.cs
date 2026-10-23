global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Erdmier.ZooTycoonNexus.Application.Common.Abstractions;
global using Erdmier.ZooTycoonNexus.Application.Common.Messaging;
global using Erdmier.ZooTycoonNexus.Application.Common.Models;
global using Erdmier.ZooTycoonNexus.Application.Installations.Add;
global using Erdmier.ZooTycoonNexus.Application.Installations.Delete;
global using Erdmier.ZooTycoonNexus.Application.Installations.GetAll;
global using Erdmier.ZooTycoonNexus.Application.Installations.GetById;
global using Erdmier.ZooTycoonNexus.Application.Installations.Locate;
global using Erdmier.ZooTycoonNexus.Application.Installations.SetDefault;
global using Erdmier.ZooTycoonNexus.Application.Installations.Relocate;
global using Erdmier.ZooTycoonNexus.Application.Installations.Update;
global using Erdmier.ZooTycoonNexus.Application.Installations.Verify;
global using Erdmier.ZooTycoonNexus.Application.Boot;
global using Erdmier.ZooTycoonNexus.Application.Game.Launch;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Common;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Get;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.History;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Recovery;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Restore;
global using Erdmier.ZooTycoonNexus.Application.IniConfig.Save;
global using Erdmier.ZooTycoonNexus.Application.Settings.Common;
global using Erdmier.ZooTycoonNexus.Application.Settings.Get;
global using Erdmier.ZooTycoonNexus.Application.Settings.Save;
global using Erdmier.ZooTycoonNexus.Application.Tests.Unit.Common;
global using Erdmier.ZooTycoonNexus.Application.Tests.Unit.IniConfig;
global using Erdmier.ZooTycoonNexus.Domain.Installations;
global using Erdmier.ZooTycoonNexus.Domain.Settings;
global using Erdmier.ZooTycoonNexus.Domain.IniDocuments;
global using Erdmier.ZooTycoonNexus.Domain.IniKeys;
global using Erdmier.ZooTycoonNexus.Domain.IniSnapshots;

global using FluentValidation;
global using FluentValidation.Results;

global using ErrorOr;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Time.Testing;

global using NSubstitute;
global using NSubstitute.ExceptionExtensions;

global using Shouldly;

global using Xunit;
