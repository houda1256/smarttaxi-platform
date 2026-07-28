// SECURITY DEMO ONLY
// DO NOT USE IN PRODUCTION
//
// This file intentionally contains a SQL Injection vulnerability so that the
// GitHub CodeQL workflow can be demonstrated detecting it. It is not wired into
// any business module and will be deleted once the CodeQL alert is confirmed.

using Npgsql;

namespace SmartTaxi.API.SecurityDemo;

public static class VulnerableSqlController
{
    public static IEndpointRouteBuilder MapVulnerableSqlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/security-demo/vulnerable-sql", GetAsync)
            .WithName("SecurityDemo_VulnerableSql")
            .WithTags("SecurityDemo");

        return app;
    }

    // SECURITY DEMO ONLY — DO NOT USE IN PRODUCTION
    private static async Task<IResult> GetAsync(string search, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // VULNERABLE: the "search" query parameter is concatenated directly into
        // the SQL text instead of being passed as a parameterized value. An input
        // such as `' OR '1'='1` or a `UNION SELECT ...` payload changes the
        // structure of the query rather than just its data, allowing an attacker
        // to bypass filtering or exfiltrate arbitrary rows.
        var sql = "SELECT \"Id\", \"Email\" FROM \"Users\" WHERE \"Email\" = '" + search + "'";

        await using var command = new NpgsqlCommand(sql, connection);

        var results = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(1));
        }

        return Results.Ok(results);
    }
}
