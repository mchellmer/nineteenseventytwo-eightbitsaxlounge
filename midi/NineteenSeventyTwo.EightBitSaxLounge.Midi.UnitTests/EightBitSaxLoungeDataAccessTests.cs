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
    private IConfiguration BuildConfig(string baseUrl)
    {
        var dict = new System.Collections.Generic.Dictionary<string, string?>
        {
            // Place the connection string under the ConnectionStrings section so
            // IConfiguration.GetConnectionString(name) will find it.
            { $"ConnectionStrings:eightBitSaxLoungeDataLayerConnectionString", baseUrl }
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task LoadDataAsync_Get_ReturnsList()
    {
        var sample = new[] { new { Address = 1, Value = 127 } };
        var json = JsonSerializer.Serialize(sample);
        var expectedUri = new Uri("http://example.test/midi/cc/db/docid");

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

        var parameters = new { RequestRoute = "midi/cc/db/docid" };
        var result = await da.LoadDataAsync<dynamic, object>("GET", parameters, "eightBitSaxLoungeDataLayerConnectionString");

        Assert.Single(result);
        Assert.Equal(1, (int)result[0].Address);

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LoadDataAsync_Post_ReturnsSingleWrappedAsList()
    {
        var sample = new { Address = 2, Value = 64 };
        var jsonResponse = JsonSerializer.Serialize(sample);
        var expectedUri = new Uri("http://example.test/midi/cc/query");
        var expectedRequestJson = JsonSerializer.Serialize(new { Address = 2 });

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri && (req.Content == null ? false : req.Content.ReadAsStringAsync().GetAwaiter().GetResult() == expectedRequestJson)),
            ItExpr.IsAny<CancellationToken>())
          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
          {
              Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
          })
          .Verifiable();

        var client = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://example.test") };

        var cfg = BuildConfig("http://example.test");
        var da = new EightbitSaxLoungeDataAccess(cfg, client);

        var parameters = new { RequestRoute = "midi/cc/query", RequestBody = new { Address = 2 } };
        var result = await da.LoadDataAsync<dynamic, object>("POST", parameters, "eightBitSaxLoungeDataLayerConnectionString");

        Assert.Single(result);
        Assert.Equal(2, (int)result[0].Address);

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SaveDataAsync_Post_Succeeds()
    {
        var expectedUri = new Uri("http://example.test/midi/cc");
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

        var parameters = new { RequestRoute = "midi/cc", RequestBody = new { Address = 3, Value = 32 } };
        await da.SaveDataAsync("POST", parameters, "eightBitSaxLoungeDataLayerConnectionString");

        handlerMock.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
    }
}
