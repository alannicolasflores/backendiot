using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

public class registerEvent
{
    private readonly IConfiguration _configuration;

    public registerEvent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("RegisterEvent")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "register_event1")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RegisterEvent");
        logger.LogInformation("Registering plastic bottle caps bin event.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        int binId = data.bin_id;
        float fillLevel = data.fill_level;
        bool status = data.status;

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var text = "INSERT INTO historial_eventos (bote_id, distancia_llenado, estado) VALUES (@binId, @fillLevel, @status)";
            using (MySqlCommand cmd = new MySqlCommand(text, conn))
            {
                cmd.Parameters.AddWithValue("@binId", binId);
                cmd.Parameters.AddWithValue("@fillLevel", fillLevel);
                cmd.Parameters.AddWithValue("@status", status);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Event registered successfully");

        return response;
    }
}
