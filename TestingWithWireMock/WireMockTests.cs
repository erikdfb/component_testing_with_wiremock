using System.Net;
using System.Text;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

public class WireMockTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly WireMockServer _wireMockServer;

    public WireMockTests()
    {
        // Create a new instance of the WireMock server
        _wireMockServer = WireMockServer.Start();

        // Create a new HttpClient instance with the base URL of our WireMock server
        _httpClient = new HttpClient { BaseAddress = new Uri(_wireMockServer.Urls[0]) };
    }

    [Fact]
    public async Task Get_Endpoint_Returns_Success()
    {
        // Arrange
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/api/users")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"Users\":[]}"));

        // Act (use a HttpClient which connects to the URL where WireMock.Net is running)
        var response = await _httpClient.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"Users\":[]}", responseContent);
    }

    [Fact]
    public async Task CreateNewUser_Returns_CreatedUser()
    {
        // Arrange
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/api/users")
                .UsingPost()
                .WithBody("{\"Name\":\"John Doe\",\"Email\":\"johndoe@example.com\"}"))
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithBody("{\"Id\":1,\"Name\":\"John Doe\",\"Email\":\"johndoe@example.com\"}"));

        var newUser = new
        {
            Name = "John Doe",
            Email = "johndoe@example.com"
        };

        var json = JsonConvert.SerializeObject(newUser);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/users", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdUser = JsonConvert.DeserializeObject<User>(responseContent);

        Assert.Equal(1, createdUser.Id);
        Assert.Equal("John Doe", createdUser.Name);
        Assert.Equal("johndoe@example.com", createdUser.Email);
    }

    public void Dispose()
    {
        // Stop the WireMock server
        _wireMockServer.Stop();
    }
}