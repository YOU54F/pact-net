# HTTP Plugin Interactions

Content plugins can configure binary HTTP request and response bodies without
providing their own transport. The public `WithHttpPluginInteractions` entry point
creates HTTP interactions on a plugin-loaded pact and keeps native handles private.

```csharp
using var pact = Pact.V4("consumer", "provider", config)
    .WithHttpPluginInteractions("avro", "0.1.3");

pact.UponReceiving("an Avro response")
    .WithRequest(HttpMethod.Get, "/avro")
    .WillRespondWithStatus(200)
    .WithHeader("Content-Type", "avro/binary;record=User")
    .WithContent("avro/binary", new Dictionary<string, object>
    {
        ["pact:avro"] = schemaPath,
        ["pact:record-name"] = "User",
        ["id"] = "matching(integer, 1)",
        ["username"] = "matching(equalTo, 'alice')"
    });
```

Use `Verify`/`VerifyAsync` with the real HTTP client and dispose the builder.
Request contents can be configured once before the response stage. Response
contents complete the interaction and can be configured once; later headers or
contents are rejected. The plugin owns its configuration schema and binary encoding.

PactNet still owns HTTP server startup, mismatches, logs, pact writing and plugin
cleanup. Provider verification uses the ordinary `PactVerifier`. This prototype
does not add asynchronous plugin message reification or expose FFI internals.