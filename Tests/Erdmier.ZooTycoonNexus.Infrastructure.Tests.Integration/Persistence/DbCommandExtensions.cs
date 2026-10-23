namespace Erdmier.ZooTycoonNexus.Infrastructure.Tests.Integration.Persistence;

/// <summary>Helpers for the persistence tests' hand-written SQLite queries.</summary>
public static class DbCommandExtensions
{
    /// <summary>Adds a named parameter to <paramref name="command" />.</summary>
    /// <param name="command">The command to add the parameter to.</param>
    /// <param name="name">The parameter name, including its prefix (<c>$name</c>).</param>
    /// <param name="value">The parameter value.</param>
    public static void AddParameter(this DbCommand command, string name, string value)
    {
        DbParameter parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.Value         = value;

        command.Parameters.Add(parameter);
    }
}
