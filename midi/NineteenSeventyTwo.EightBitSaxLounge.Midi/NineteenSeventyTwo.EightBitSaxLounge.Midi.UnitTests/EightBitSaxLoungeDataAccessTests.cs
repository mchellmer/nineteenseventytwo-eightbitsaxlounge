namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using Library.DataAccess;

public class EightbitSaxLoungeDataAccessTests
{
    // Simple DTO used only for tests
    private class EightBitSaxLoungeDataObject
    {
        public string ObjectParamA { get; set; } = string.Empty;
        public string ObjectParamB { get; set; } = string.Empty;
    }

    private IConfiguration BuildConfig(string baseUrl)
    {
        var dict = new Dictionary<string, string?>
        {
            // Put the connection string under ConnectionStrings so GetConnectionString finds it
            { $"ConnectionStrings:EightBitSaxLoungeDataLayer", baseUrl }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task LoadDataAsync_Get_ReturnsList()
    {
        var sample = new[] { new EightBitSaxLoungeDataObject { ObjectParamA = "A", ObjectParamB = "B" } };
        var json = JsonSerializer.Serialize(sample);
        var expectedUri = new Uri("http://example.test/test/route");

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
          {
              Content = new StringContent(json, Encoding.UTF8, "application/json")
          })
          .Verifiable();

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://example.test")
        };

        var cfg = BuildConfig("http://example.test");
        var da = new EightbitSaxLoungeDataAccess(cfg, client);

        var parameters = new EightBitSaxLoungeDataRequest { RequestRoute = "/test/route" };
        var result = await da.LoadDataAsync<EightBitSaxLoungeDataObject, EightBitSaxLoungeDataRequest>(
            "GET",
            parameters,
            "EightBitSaxLoungeDataLayer");

        Assert.Single(result);
        Assert.Equal("A", result[0].ObjectParamA);
        Assert.Equal("B", result[0].ObjectParamB);

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LoadDataAsync_Post_ReturnsSingleWrappedAsList()
    {
        var sample = new EightBitSaxLoungeDataObject { ObjectParamA = "C", ObjectParamB = "D" };
        var jsonResponse = JsonSerializer.Serialize(sample);
        var expectedUri = new Uri("http://example.test/test/query");
        var expectedRequestJson = JsonSerializer.Serialize(new { ObjectParamA = "filterA" });

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        string? capturedRequestContent = null;
        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
          .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
          {
              if (req.Content != null)
                  capturedRequestContent = await req.Content.ReadAsStringAsync();

              return new HttpResponseMessage(HttpStatusCode.OK)
              {
                  Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
              };
          })
          .Verifiable();

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://example.test") };

        var cfg = BuildConfig("http://example.test");
        var da = new EightbitSaxLoungeDataAccess(cfg, client);

        var parameters = new EightBitSaxLoungeDataRequest { RequestRoute = "/test/query", RequestBody = new { ObjectParamA = "filterA" } };
        var result = await da.LoadDataAsync<EightBitSaxLoungeDataObject, EightBitSaxLoungeDataRequest>("POST", parameters, "EightBitSaxLoungeDataLayer");

        Assert.Single(result);
        Assert.Equal("C", result[0].ObjectParamA);
        Assert.Equal("D", result[0].ObjectParamB);

        // Verify the request was sent and the body matched the expected JSON
        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());

        Assert.Equal(expectedRequestJson, capturedRequestContent);
    }

    [Fact]
    public async Task SaveDataAsync_Post_Succeeds()
    {
        var expectedUri = new Uri("http://example.test/test/save");
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created))
          .Verifiable();

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://example.test") };

        var cfg = BuildConfig("http://example.test");
        var da = new EightbitSaxLoungeDataAccess(cfg, client);

        var parameters = new EightBitSaxLoungeDataRequest
        {
            RequestRoute = "/test/save",
            RequestBody = new EightBitSaxLoungeDataObject { ObjectParamA = "E", ObjectParamB = "F" }
        };
        await da.SaveDataAsync("POST", parameters, "EightBitSaxLoungeDataLayer");

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }
    
    [Fact]
    public async Task SaveDataAsync_Put_Succeeds()
    {
        var expectedUri = new Uri("http://example.test/test/save");
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent))
          .Verifiable();

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://example.test") };

        var cfg = BuildConfig("http://example.test");
        var da = new EightbitSaxLoungeDataAccess(cfg, client);

        var parameters = new EightBitSaxLoungeDataRequest
        {
            RequestRoute = "/test/save",
            RequestBody = new EightBitSaxLoungeDataObject { ObjectParamA = "E", ObjectParamB = "F" }
        };
        await da.SaveDataAsync("PUT", parameters, "EightBitSaxLoungeDataLayer");

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

}
