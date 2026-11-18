using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using PactNet.Drivers.Plugins;
using PactNet.Interop;
using Xunit;

namespace PactNet.Tests;

public class HttpPluginPactBuilderTests
{
    private readonly Mock<IHttpPluginInteractionDriver> driver = new();

    [Fact]
    public void Contents_AreSentToTheCorrectHttpPart()
    {
        var request = new HttpPluginRequestBuilder(this.driver.Object);
        var contents = new Dictionary<string, object>();
        request.WithRequest(HttpMethod.Post, "/avro")
            .WithContent("avro/binary", contents)
            .WillRespondWithStatus(201).WithContent("avro/binary", contents);
        this.driver.Verify(value => value.WithRequest("POST", "/avro"), Times.Once);
        this.driver.Verify(value => value.WithResponseStatus(201), Times.Once);
        this.driver.Verify(value => value.WithPluginContent(InteractionPart.Request, "avro/binary", contents), Times.Once);
        this.driver.Verify(value => value.WithPluginContent(InteractionPart.Response, "avro/binary", contents), Times.Once);
    }

    [Fact]
    public void Response_RejectsRepeatedContentsAndHeadersAfterCompletion()
    {
        var response = new HttpPluginResponseBuilder(this.driver.Object);
        var contents = new Dictionary<string, object>();
        response.WithContent("avro/binary", contents);
        Assert.Throws<InvalidOperationException>(() => response.WithContent("avro/binary", contents));
        Assert.Throws<InvalidOperationException>(() => response.WithHeader("Content-Type", "avro/binary"));
    }

    [Fact]
    public void Request_RejectsReuseAndInvalidOrdering()
    {
        var request = new HttpPluginRequestBuilder(this.driver.Object);
        Assert.Throws<InvalidOperationException>(() => request.WillRespondWithStatus(200));
        request.WithRequest(HttpMethod.Get, "/avro");
        Assert.Throws<InvalidOperationException>(() => request.WithRequest(HttpMethod.Post, "/avro"));
        Assert.Throws<ArgumentOutOfRangeException>(() => request.WillRespondWithStatus(600));
        request.WithContent("avro/binary", new Dictionary<string, object>());
        Assert.Throws<InvalidOperationException>(() => request.WithContent("avro/binary", new Dictionary<string, object>()));
        request.WillRespondWithStatus(200);
        Assert.Throws<InvalidOperationException>(() => request.WillRespondWithStatus(200));
        Assert.Throws<InvalidOperationException>(() => request.Given("late state"));
    }

    [Fact]
    public void RepeatedHeadersAndQueries_UseIndependentIndexes()
    {
        var request = new HttpPluginRequestBuilder(this.driver.Object);
        request.WithHeader("Accept", "one").WithHeader("accept", "two")
            .WithQuery("id", "one").WithQuery("id", "two");
        var response = new HttpPluginResponseBuilder(this.driver.Object);
        response.WithHeader("X-Test", "one").WithHeader("x-test", "two");
        this.driver.Verify(value => value.WithRequestHeader("accept", "two", 1), Times.Once);
        this.driver.Verify(value => value.WithQueryParameter("id", "two", 1), Times.Once);
        this.driver.Verify(value => value.WithResponseHeader("x-test", "two", 1), Times.Once);
    }

    [Fact]
    public void Pact_UsesHttpInteractionFactoryAndDelegatesDisposal()
    {
        var pact = new Mock<IPluginPactDriver>();
        pact.Setup(value => value.NewHttpPluginInteraction("an avro request")).Returns(this.driver.Object);
        using (var builder = new HttpPluginPactBuilder(pact.Object, new PactConfig()))
        {
            builder.UponReceiving("an avro request");
        }
        pact.Verify(value => value.NewHttpPluginInteraction("an avro request"), Times.Once);
        pact.Verify(value => value.Dispose(), Times.Once);
    }
}