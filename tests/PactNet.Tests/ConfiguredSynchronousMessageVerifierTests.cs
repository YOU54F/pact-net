using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PactNet.Drivers;
using PactNet.Exceptions;
using Xunit;

namespace PactNet.Tests
{
    public class ConfiguredSynchronousMessageVerifierTests
    {
        private readonly Mock<ISynchronousMessageInteractionDriver> mockDriver;
        private readonly PactConfig config;

        public ConfiguredSynchronousMessageVerifierTests()
        {
            this.mockDriver = new Mock<ISynchronousMessageInteractionDriver>();
            this.config = new PactConfig
            {
                PactDir = "/path/to/pacts",
                DefaultJsonSettings = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };
        }

        [Fact]
        public void VerifyWithResponse_ResponseMatches_WritesPactFile()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response""}");

            verifier.VerifyWithResponse<Message, Message>(_ => new Message { IntValue = 2, StringValue = "response" });

            this.mockDriver.Verify(s => s.WritePactFile(this.config.PactDir), Times.Once);
        }

        [Fact]
        public void VerifyWithResponse_ResponseMismatches_ThrowsVerificationException()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response""}");

            Action action = () => verifier.VerifyWithResponse<Message, Message>(_ => new Message { IntValue = 99, StringValue = "wrong" });

            action.Should().Throw<PactMessageConsumerVerificationException>()
                .WithMessage("*response did not match*");
            this.mockDriver.Verify(s => s.WritePactFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyWithResponseAsync_ResponseMatches_WritesPactFile()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response""}");

            await verifier.VerifyWithResponseAsync<Message, Message>(_ =>
                Task.FromResult(new Message { IntValue = 2, StringValue = "response" }));

            this.mockDriver.Verify(s => s.WritePactFile(this.config.PactDir), Times.Once);
        }

        [Fact]
        public async Task VerifyWithResponseAsync_ResponseMismatches_ThrowsVerificationException()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response""}");

            Func<Task> action = () => verifier.VerifyWithResponseAsync<Message, Message>(_ =>
                Task.FromResult(new Message { IntValue = 9, StringValue = "wrong" }));

            await action.Should().ThrowAsync<PactMessageConsumerVerificationException>();
            this.mockDriver.Verify(s => s.WritePactFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Verify_WithContext_ProvidesRequestAndMultipleResponses()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response-1""}");

            verifier.WithResponseJsonContent(new Message { IntValue = 3, StringValue = "response-2" });

            verifier.Verify<Message, Message>(message =>
            {
                message.Request.IntValue.Should().Be(1);
                message.Response.Should().HaveCount(2);
                message.Response[0].StringValue.Should().Be("response-1");
                message.Response[1].StringValue.Should().Be("response-2");
            });

            this.mockDriver.Verify(s => s.WritePactFile(this.config.PactDir), Times.Once);
        }

        [Fact]
        public void WithResponseMetadata_AfterResponseContent_UpdatesLatestResponseMetadataAndDriver()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response-1""}");

            verifier.WithResponseMetadata("contentType", "application/json");

            verifier.Verify<Message, Message>(message =>
            {
                message.ResponseMetadata.Should().HaveCount(1);
                message.ResponseMetadata[0].Should().ContainKey("contentType");
                message.ResponseMetadata[0]["contentType"].Should().Be("application/json");
            });

            this.mockDriver.Verify(d => d.WithResponseMetadata("contentType", "application/json"), Times.Once);
        }

        [Fact]
        public void VerifyWithResponse_WhenMultipleResponsesConfigured_ThrowsHelpfulException()
        {
            var verifier = new ConfiguredSynchronousMessageVerifier(
                this.mockDriver.Object,
                this.config,
                @"{""intValue"":1,""stringValue"":""request""}",
                @"{""intValue"":2,""stringValue"":""response-1""}");

            verifier.WithResponseJsonContent(new Message { IntValue = 3, StringValue = "response-2" });

            Action action = () => verifier.VerifyWithResponse<Message, Message>(_ => new Message { IntValue = 2, StringValue = "response-1" });

            action.Should().Throw<PactMessageConsumerVerificationException>()
                .Which.InnerException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Contain("exactly one configured response body");
            this.mockDriver.Verify(s => s.WritePactFile(It.IsAny<string>()), Times.Never);
        }

        private class Message
        {
            public int IntValue { get; set; }

            public string StringValue { get; set; }
        }
    }
}
