using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

public class GetEventHistory
{
    private readonly IConfiguration _configuration;

    public GetEventHistory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("GetEventHistory")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "event_history/{binId}/{timeRange}")] HttpRequestData req,
        int binId, string timeRange,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("GetEventHistory");
        logger.LogInformation($"Getting event history for bin ID: {binId} within {timeRange}");

        DateTime startDate;
        switch (timeRange.ToLower())
        {
            case "one_week":
                startDate = DateTime.UtcNow.AddDays(-7);
                break;
            case "three_weeks":
                startDate = DateTime.UtcNow.AddDays(-21);
                break;
            case "one_month":
                startDate = DateTime.UtcNow.AddMonths(-1);
                break;
            case "two_months":
                startDate = DateTime.UtcNow.AddMonths(-2);
                break;
            case "three_months":
                startDate = DateTime.UtcNow.AddMonths(-3);
                break;
            case "four_months":
                startDate = DateTime.UtcNow.AddMonths(-4);
                break;
            case "five_months":
                startDate = DateTime.UtcNow.AddMonths(-5);
                break;
            case "six_months":
                startDate = DateTime.UtcNow.AddMonths(-6);
                break;
            default:
                startDate = DateTime.UtcNow.AddDays(-7);
                break;
        }

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");
        var eventList = new List<dynamic>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "SELECT * FROM historial_eventos WHERE bote_id = @binId AND timestamp >= @startDate AND distancia_llenado <= 900";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@binId", binId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        eventList.Add(new
                        {
                            Timestamp = reader.GetDateTime("timestamp"),
                            FillLevel = reader.GetFloat("distancia_llenado"),
                            Status = reader.GetBoolean("estado")
                        });
                    }
                }
            }
        }

        // Análisis de Tendencias de Llenado
        var fillLevelTrend = eventList
            .OrderBy(e => e.Timestamp)
            .Select(e => new { e.Timestamp, e.FillLevel })
            .ToList();

        // Análisis de Frecuencia de Llenado
        var fillFrequency = eventList
            .GroupBy(e => e.FillLevel)
            .Select(g => new { FillLevel = g.Key, Count = g.Count() })
            .ToList();

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { FillLevelTrend = fillLevelTrend, FillFrequency = fillFrequency });

        return response;
    }
}
