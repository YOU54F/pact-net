using System;
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace PactNet.Output.Xunit
{
    /// <summary>
    /// Output to an xUnit test output helper
    /// </summary>
    public class XunitOutput : IOutput
    {
        private readonly ITestOutputHelper output;

        /// <summary>
        /// Initialises a new instance of the <see cref="XunitOutput"/> class.
        /// </summary>
        /// <param name="output">xUnit test output helper</param>
        public XunitOutput(ITestOutputHelper output)
        {
            this.output = output;
        }

        /// <summary>
        /// Write a line to the output
        /// </summary>
        /// <param name="line">Line to write</param>
        public void WriteLine(string line)
        {
            try
            {
                this.output.WriteLine(line);
            }
            catch (ObjectDisposedException)
            {
                // xUnit output helper may also be disposed during teardown.
            }
            catch (InvalidOperationException)
            {
                // xUnit can throw when asynchronous/background work logs after the test has been torn down.
                // Logging should never fail the verifier flow.
            }
        }
    }
}
