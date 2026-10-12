using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PactNet
{
    /// <summary>Asynchronous plugin messages with transport abstracted away.</summary>
    public interface IAsynchronousPluginPactBuilderV4 : IDisposable
    {
        /// <summary>Start an asynchronous message.</summary>
        IAsynchronousPluginMessageBuilderV4 ExpectsToReceive(string description);
    }

    /// <summary>Configure a message before consumer verification.</summary>
    public interface IAsynchronousPluginMessageBuilderV4
    {
        /// <summary>Add a provider state.</summary>
        IAsynchronousPluginMessageBuilderV4 Given(string description);
        /// <summary>Add a parameterized provider state.</summary>
        IAsynchronousPluginMessageBuilderV4 Given(string description, string name, string value);
        /// <summary>Add message metadata.</summary>
        IAsynchronousPluginMessageBuilderV4 WithMetadata(string name, string value);
        /// <summary>Set plugin-defined contents once and move to verification.</summary>
        IConfiguredPluginMessageVerifier WithContent(string contentType, Dictionary<string, object> content);
    }

    /// <summary>Run the real consumer with a generated plugin message.</summary>
    public interface IConfiguredPluginMessageVerifier
    {
        /// <summary>Verify consumption, then write the pact on success.</summary>
        void Verify(Action<PluginMessage> consumer);
        /// <summary>Verify asynchronous consumption, then write the pact on success.</summary>
        Task VerifyAsync(Func<PluginMessage, Task> consumer);
    }

    /// <summary>Generated binary contents and metadata for a plugin message.</summary>
    public sealed class PluginMessage
    {
        /// <summary>Binary payload, not a JSON serialization of the record.</summary>
        public byte[] Contents { get; }
        /// <summary>Payload content type.</summary>
        public string ContentType { get; }
        /// <summary>Message metadata.</summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>Create a generated message context.</summary>
        public PluginMessage(byte[] contents, string contentType, IReadOnlyDictionary<string, object> metadata)
        {
            this.Contents = contents ?? throw new ArgumentNullException(nameof(contents));
            this.ContentType = contentType;
            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
    }
}