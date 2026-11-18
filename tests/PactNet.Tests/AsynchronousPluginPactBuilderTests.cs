using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using PactNet.Drivers.Plugins;
using PactNet.Exceptions;
using Xunit;

namespace PactNet.Tests;

public class AsynchronousPluginPactBuilderTests
{
    private readonly Mock<IAsynchronousPluginInteractionDriver> driver = new();
    private readonly PactConfig config = new() { PactDir = "test-pacts" };
    private const string Reified = "{\"contents\":{\"content\":\"AP8B\",\"contentType\":\"avro/binary;record=User\",\"encoded\":\"base64\"},\"metadata\":{\"topic\":\"users\"}}";

    [Fact]
    public void Decode_PreservesBinaryBytesAndMetadata()
    {
        var message = ConfiguredPluginMessageVerifier.Decode(Reified);
        Assert.Equal(new byte[] { 0, 255, 1 }, message.Contents);
        Assert.Equal("avro/binary;record=User", message.ContentType);
        Assert.Equal("users", ((JsonElement)message.Metadata["topic"]).GetString());
    }

    [Fact]
    public void Contents_AreSingleShotAndFreezeMetadata()
    {
        var builder = new AsynchronousPluginMessageBuilder(this.driver.Object, this.config);
        var contents = new Dictionary<string, object>();
        builder.Given("available", "id", "1").WithMetadata("topic", "users").WithContent("avro/binary", contents);
        Assert.Throws<InvalidOperationException>(() => builder.WithContent("avro/binary", contents));
        Assert.Throws<InvalidOperationException>(() => builder.WithMetadata("topic", "other"));
        this.driver.Verify(value => value.WithPluginContent("avro/binary", contents), Times.Once);
        this.driver.Verify(value => value.GivenWithParam("available", "id", "1"), Times.Once);
    }

    [Fact]
    public async Task SuccessfulCallback_WritesThePact()
    {
        this.driver.Setup(value => value.Reify()).Returns(Reified);
        await new ConfiguredPluginMessageVerifier(this.driver.Object, this.config)
            .VerifyAsync(message => { Assert.Equal(3, message.Contents.Length); return Task.CompletedTask; });
        this.driver.Verify(value => value.WritePactFile(this.config.PactDir), Times.Once);
    }

    [Fact]
    public void Metadata_IsAppliedAfterPluginConfiguration()
    {
        var ordered = new Mock<IAsynchronousPluginInteractionDriver>(MockBehavior.Strict);
        var sequence = new MockSequence();
        ordered.InSequence(sequence).Setup(value => value.WithPluginContent("avro/binary", It.IsAny<Dictionary<string, object>>()));
        ordered.InSequence(sequence).Setup(value => value.WithMetadata("topic", "users"));
        new AsynchronousPluginMessageBuilder(ordered.Object, this.config).WithMetadata("topic", "users")
            .WithContent("avro/binary", new Dictionary<string, object>());
        ordered.Verify(value => value.WithMetadata("topic", "users"), Times.Once);
    }

    [Fact]
    public void Pact_CreatesMessagesAndDisposesItsDriver()
    {
        var pact = new Mock<IPluginPactDriver>();
        pact.Setup(value => value.NewAsynchronousPluginInteraction("a message")).Returns(this.driver.Object);
        using (var builder = new AsynchronousPluginPactBuilder(pact.Object, this.config))
        {
            builder.ExpectsToReceive("a message");
        }
        pact.Verify(value => value.Dispose(), Times.Once);
    }

    [Fact]
    public void FailedCallback_DoesNotWriteThePact()
    {
        this.driver.Setup(value => value.Reify()).Returns(Reified);
        Assert.Throws<PactMessageConsumerVerificationException>(() =>
            new ConfiguredPluginMessageVerifier(this.driver.Object, this.config)
                .Verify(_ => throw new InvalidOperationException("consumer rejected the record")));
        this.driver.Verify(value => value.WritePactFile(It.IsAny<string>()), Times.Never);
    }
}