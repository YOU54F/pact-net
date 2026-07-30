using System;
using FluentAssertions;
using Moq;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PactNet.Tests
{
    public class XunitOutputTests
    {
        [Fact]
        public void WriteLine_WhenOutputIsActive_WritesToTestOutput()
        {
            var mock = new Mock<ITestOutputHelper>();
            var output = new XunitOutput(mock.Object);

            output.WriteLine("hello");

            mock.Verify(x => x.WriteLine("hello"), Times.Once);
        }

        [Fact]
        public void WriteLine_WhenNoActiveTest_DoesNotThrow()
        {
            var mock = new Mock<ITestOutputHelper>();
            mock
                .Setup(x => x.WriteLine(It.IsAny<string>()))
                .Throws(new InvalidOperationException("There is no currently active test."));

            var output = new XunitOutput(mock.Object);

            Action act = () => output.WriteLine("hello");

            act.Should().NotThrow();
        }

        [Fact]
        public void WriteLine_WhenOutputIsDisposed_DoesNotThrow()
        {
            var mock = new Mock<ITestOutputHelper>();
            mock
                .Setup(x => x.WriteLine(It.IsAny<string>()))
                .Throws(new ObjectDisposedException("ITestOutputHelper"));

            var output = new XunitOutput(mock.Object);

            Action act = () => output.WriteLine("hello");

            act.Should().NotThrow();
        }
    }
}
