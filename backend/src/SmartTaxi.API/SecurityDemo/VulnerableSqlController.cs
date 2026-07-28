// SECURITY DEMO ONLY
// DO NOT USE IN PRODUCTION
//
// This controller intentionally contains a SQL Injection vulnerability so that
// the GitHub CodeQL workflow can be demonstrated detecting it. It is not wired
// into any business module and will be deleted once the CodeQL alert is confirmed.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SmartTaxi.API.SecurityDemo;

[ApiController]
[Route("api/security-demo")]
public class VulnerableSqlController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public VulnerableSqlController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // SECURITY DEMO ONLY — DO NOT USE IN PRODUCTION
    [HttpGet("vulnerable-sql")]
    public async Task<IActionResult> GetAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // VULNERABLE: the "search" query parameter is concatenated directly into
        // the SQL text instead of being passed as a parameterized value. An input
        // such as `' OR '1'='1` or a `UNION SELECT ...` payload changes the
        // structure of the query rather than just its data, allowing an attacker
        // to bypass filtering or exfiltrate arbitrary rows.
        string search = HttpContext.Request.Query["search"].ToString();

        string sql =
            "SELECT Id, Email FROM Users WHERE Email = '" + search + "'";

        using var command = new SqlCommand(sql, connection);
        await command.ExecuteReaderAsync();

        return Ok();
    }
}
