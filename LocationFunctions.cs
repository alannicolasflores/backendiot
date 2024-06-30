using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;

public class LocationFunctions
{
    private readonly IConfiguration _configuration;

    public LocationFunctions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Function("LocationFunctions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("LocationFunctions");
        logger.LogInformation("Processing a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        string operation = data.operation;

        string connectionString = _configuration.GetValue<string>("MySqlConnectionString");

        switch (operation)
        {
            case "create":
                return await CreateLocation(data, req, connectionString);
            case "read":
                return await GetLocations(req, connectionString);
            case "update":
                return await UpdateLocation(data, req, connectionString);
            case "delete":
                return await DeleteLocation(data, req, connectionString);
            default:
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid operation");
                return errorResponse;
        }
    }

    private async Task<HttpResponseData> CreateLocation(dynamic data, HttpRequestData req, string connectionString)
    {
        string name = data.name;
        string details = data.details;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "INSERT INTO ubicaciones (nombre, detalles) VALUES (@name, @details)";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@details", details);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Location created successfully");

        return response;
    }

    private async Task<HttpResponseData> GetLocations(HttpRequestData req, string connectionString)
    {
        var locationList = new List<dynamic>();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "SELECT * FROM ubicaciones";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        locationList.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nombre"),
                            Details = reader.GetString("detalles")
                        });
                    }
                }
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(locationList);

        return response;
    }

    private async Task<HttpResponseData> UpdateLocation(dynamic data, HttpRequestData req, string connectionString)
    {
        int id = data.id;
        string name = data.name;
        string details = data.details;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var query = "UPDATE ubicaciones SET nombre = @name, detalles = @details WHERE id = @id";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@details", details);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Location updated successfully");

        return response;
    }

    private async Task<HttpResponseData> DeleteLocation(dynamic data, HttpRequestData req, string connectionString)
    {
        int id = data.id;

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            await conn.OpenAsync();

            // Eliminar los registros dependientes en botes_tapas_plasticas
            var deleteDependentsQuery = "DELETE FROM botes_tapas_plasticas WHERE ubicacion_id = @id";
            using (MySqlCommand cmd = new MySqlCommand(deleteDependentsQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }

            // Eliminar la ubicación
            var deleteLocationQuery = "DELETE FROM ubicaciones WHERE id = @id";
            using (MySqlCommand cmd = new MySqlCommand(deleteLocationQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync("Location and its dependents deleted successfully");

        return response;
    }
}
