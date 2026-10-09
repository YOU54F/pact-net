# Asynchronous Plugin Messages

Use `WithAsynchronousPluginInteractions` for content plugins without a transport
server. Configure a single message, then exercise the real consumer with generated
binary contents and metadata:

```csharp
using var pact = Pact.V4("consumer", "provider", config)
    .WithAsynchronousPluginInteractions("avro", "0.1.3");

await pact.ExpectsToReceive("an Avro user")
    .WithMetadata("topic", "users")
    .WithContent("avro/binary", contents)
    .VerifyAsync(message => ConsumeUserAsync(message.Contents, message.Metadata));
```

The contents dictionary follows the plugin's schema; for Avro it includes
`pact:avro`, `pact:record-name` and record field matching expressions. Message
contents are terminal. User metadata is applied after plugin configuration so
it is not replaced by the plugin's generated metadata.

`PluginMessage` contains copied binary contents, the media type and cloned JSON
metadata values. PactNet decodes the V4 base64 envelope, owns the reified native
string, writes the pact only after a successful callback, and cleans up plugins
when the pact builder is disposed. The existing JSON message API is unchanged.

This does not start Kafka/RabbitMQ or claim broker-delivery coverage. Producer
verification uses the normal verifier with a message provider or HTTP message
adapter. Review multi-message/generator/error paths and platform coverage before
publishing this prototype API.

Avro 0.1.3's single unlabelled interaction does not populate a synchronous message's
request/response fields with FFI 0.4.27. An opt-in native capability test records
that limitation. Distinct sync parts require an Avro plugin change; transport-free
binary request/response consumer verification also needs a separate PactNet API.