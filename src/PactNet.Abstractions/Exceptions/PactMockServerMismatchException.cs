using System;

namespace PactNet.Exceptions
{
    /// <summary>
    /// The mock server received requests which didn't match the configured interactions, or didn't receive the
    /// expected requests
    /// </summary>
    [Serializable]
    public class PactMockServerMismatchException : PactFailureException
    {
        /// <summary>
        /// Mismatches reported by the mock server, as a JSON array (the format produced by <c>pactffi_mock_server_mismatches</c>)
        /// </summary>
        public string Mismatches { get; }

        /// <summary>
        /// Log lines captured for this mock server (the format produced by <c>pactffi_mock_server_logs</c>)
        /// </summary>
        /// <remarks>
        /// The level of these logs is set by <see cref="PactConfig.LogLevel"/> the first time a pact is created
        /// in the process, because the native logger can only be initialised once
        /// </remarks>
        public string MockServerLogs { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="PactMockServerMismatchException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="mismatches">Mismatches reported by the mock server, as a JSON array</param>
        /// <param name="mockServerLogs">Log lines captured for this mock server</param>
        public PactMockServerMismatchException(string message, string mismatches, string mockServerLogs)
            : base(message)
        {
            this.Mismatches = mismatches;
            this.MockServerLogs = mockServerLogs;
        }
    }
}
