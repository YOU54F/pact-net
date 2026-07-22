using System;
using System.Runtime.InteropServices;

namespace PactNet.Interop
{
    /// <summary>
    /// Interop definitions to the Pact FFI library
    /// </summary>
    internal static class NativeInterop
    {
        private const string DllName = "pact_ffi";

        [DllImport(DllName, EntryPoint = "pactffi_log_to_buffer")]
        public static extern int LogToBuffer(LevelFilter levelFilter);

        #region Http Interop Support

        [DllImport(DllName, EntryPoint = "pactffi_create_mock_server_for_transport")]
        public static extern int CreateMockServerForTransport(PactHandle pact, string addrStr, ushort port, string transport, string transportConfig);

        [DllImport(DllName, EntryPoint = "pactffi_mock_server_mismatches")]
        public static extern IntPtr MockServerMismatches(int mockServerPort);

        [DllImport(DllName, EntryPoint = "pactffi_mock_server_logs")]
        public static extern IntPtr MockServerLogs(int mockServerPort);

        [DllImport(DllName, EntryPoint = "pactffi_cleanup_mock_server")]
        public static extern bool CleanupMockServer(int mockServerPort);

        [DllImport(DllName, EntryPoint = "pactffi_pact_handle_write_file")]
        public static extern int WritePactFile(PactHandle pact, string directory, bool overwrite);

        [DllImport(DllName, EntryPoint = "pactffi_fetch_log_buffer")]
        public static extern string FetchLogBuffer(string logId);

        [DllImport(DllName, EntryPoint = "pactffi_new_pact")]
        public static extern PactHandle NewPact(string consumerName, string providerName);

        [DllImport(DllName, EntryPoint = "pactffi_with_specification")]
        public static extern bool WithSpecification(PactHandle pact, PactSpecification version);

        [DllImport(DllName, EntryPoint = "pactffi_new_interaction")]
        public static extern InteractionHandle NewInteraction(PactHandle pact, string description);

        [DllImport(DllName, EntryPoint = "pactffi_given")]
        public static extern bool Given(InteractionHandle interaction, string description);

        [DllImport(DllName, EntryPoint = "pactffi_given_with_param")]
        public static extern bool GivenWithParam(InteractionHandle interaction, string description, string name, string value);

        [DllImport(DllName, EntryPoint = "pactffi_with_request")]
        public static extern bool WithRequest(InteractionHandle interaction, string method, string path);

        [DllImport(DllName, EntryPoint = "pactffi_with_query_parameter_v2")]
        public static extern bool WithQueryParameter(InteractionHandle interaction, string name, UIntPtr index, string value);

        [DllImport(DllName, EntryPoint = "pactffi_with_header_v2")]
        public static extern bool WithHeader(InteractionHandle interaction, InteractionPart part, string name, UIntPtr index, string value);

        [DllImport(DllName, EntryPoint = "pactffi_response_status")]
        public static extern bool ResponseStatus(InteractionHandle interaction, ushort status);

        [DllImport(DllName, EntryPoint = "pactffi_with_body")]
        public static extern bool WithBody(InteractionHandle interaction, InteractionPart part, string contentType, string body);

        [DllImport(DllName, EntryPoint = "pactffi_free_string")]
        public static extern void FreeString(IntPtr s);

        [DllImport(DllName, EntryPoint = "pactffi_verify")]
        public static extern int Verify(string args);

        #endregion Http Interop Support

        #region Messaging Interop Support

        [DllImport(DllName, EntryPoint = "pactffi_with_message_pact_metadata")]
        public static extern void WithMessagePactMetadata(PactHandle pact, string @namespace, string name, string value);

        [DllImport(DllName, EntryPoint = "pactffi_new_message_interaction")]
        public static extern InteractionHandle NewMessageInteraction(PactHandle pact, string description);

        [DllImport(DllName, EntryPoint = "pactffi_new_sync_message_interaction")]
        public static extern InteractionHandle NewSyncMessageInteraction(PactHandle pact, string description);

        [DllImport(DllName, EntryPoint = "pactffi_message_expects_to_receive")]
        public static extern void MessageExpectsToReceive(InteractionHandle message, string description);

        [DllImport(DllName, EntryPoint = "pactffi_message_with_metadata")]
        public static extern void MessageWithMetadata(InteractionHandle message, string key, string value);

        [DllImport(DllName, EntryPoint = "pactffi_message_with_contents")]
        public static extern void MessageWithContents(InteractionHandle message, string contentType, string body, UIntPtr size);

        [DllImport(DllName, EntryPoint = "pactffi_message_reify")]
        public static extern IntPtr MessageReify(InteractionHandle message);

        #endregion Messaging Interop Support

        #region Message Model Interop Support

        [DllImport(DllName, EntryPoint = "pactffi_pact_interaction_as_asynchronous_message")]
        public static extern IntPtr PactInteractionAsAsynchronousMessage(IntPtr interaction);

        [DllImport(DllName, EntryPoint = "pactffi_pact_interaction_as_synchronous_message")]
        public static extern IntPtr PactInteractionAsSynchronousMessage(IntPtr interaction);

        [DllImport(DllName, EntryPoint = "pactffi_pact_handle_get_async_message_iter")]
        public static extern IntPtr PactHandleGetAsyncMessageIter(PactHandle pact);

        [DllImport(DllName, EntryPoint = "pactffi_pact_async_message_iter_next")]
        public static extern IntPtr PactAsyncMessageIterNext(IntPtr iterator);

        [DllImport(DllName, EntryPoint = "pactffi_pact_async_message_iter_delete")]
        public static extern void PactAsyncMessageIterDelete(IntPtr iterator);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_delete")]
        public static extern void AsyncMessageDelete(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_contents_str")]
        public static extern IntPtr AsyncMessageGetContentsStr(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_set_contents_str")]
        public static extern void AsyncMessageSetContentsStr(IntPtr message, string contents, string contentType);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_contents_length")]
        public static extern UIntPtr AsyncMessageGetContentsLength(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_contents_bin")]
        public static extern IntPtr AsyncMessageGetContentsBin(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_contents")]
        public static extern IntPtr AsyncMessageGetContents(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_generate_contents")]
        public static extern IntPtr AsyncMessageGenerateContents(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_description")]
        public static extern IntPtr AsyncMessageGetDescription(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_provider_state")]
        public static extern IntPtr AsyncMessageGetProviderState(IntPtr message, uint index);

        [DllImport(DllName, EntryPoint = "pactffi_async_message_get_provider_state_iter")]
        public static extern IntPtr AsyncMessageGetProviderStateIter(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_pact_handle_get_sync_message_iter")]
        public static extern IntPtr PactHandleGetSyncMessageIter(PactHandle pact);

        [DllImport(DllName, EntryPoint = "pactffi_pact_sync_message_iter_next")]
        public static extern IntPtr PactSyncMessageIterNext(IntPtr iterator);

        [DllImport(DllName, EntryPoint = "pactffi_pact_sync_message_iter_delete")]
        public static extern void PactSyncMessageIterDelete(IntPtr iterator);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_delete")]
        public static extern void SyncMessageDelete(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_request_contents_str")]
        public static extern IntPtr SyncMessageGetRequestContentsStr(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_set_request_contents_str")]
        public static extern void SyncMessageSetRequestContentsStr(IntPtr message, string contents, string contentType);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_request_contents_length")]
        public static extern UIntPtr SyncMessageGetRequestContentsLength(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_request_contents_bin")]
        public static extern IntPtr SyncMessageGetRequestContentsBin(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_request_contents")]
        public static extern IntPtr SyncMessageGetRequestContents(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_generate_request_contents")]
        public static extern IntPtr SyncMessageGenerateRequestContents(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_number_responses")]
        public static extern UIntPtr SyncMessageGetNumberResponses(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_response_contents_str")]
        public static extern IntPtr SyncMessageGetResponseContentsStr(IntPtr message, UIntPtr index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_set_response_contents_str")]
        public static extern void SyncMessageSetResponseContentsStr(IntPtr message, UIntPtr index, string contents, string contentType);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_response_contents_length")]
        public static extern UIntPtr SyncMessageGetResponseContentsLength(IntPtr message, UIntPtr index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_response_contents_bin")]
        public static extern IntPtr SyncMessageGetResponseContentsBin(IntPtr message, UIntPtr index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_response_contents")]
        public static extern IntPtr SyncMessageGetResponseContents(IntPtr message, UIntPtr index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_generate_response_contents")]
        public static extern IntPtr SyncMessageGenerateResponseContents(IntPtr message, UIntPtr index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_description")]
        public static extern IntPtr SyncMessageGetDescription(IntPtr message);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_provider_state")]
        public static extern IntPtr SyncMessageGetProviderState(IntPtr message, uint index);

        [DllImport(DllName, EntryPoint = "pactffi_sync_message_get_provider_state_iter")]
        public static extern IntPtr SyncMessageGetProviderStateIter(IntPtr message);

        #endregion Message Model Interop Support

        #region Verifier Support

        [DllImport(DllName, EntryPoint = "pactffi_verifier_new_for_application")]
        public static extern IntPtr VerifierNewForApplication(string name, string version);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_shutdown")]
        public static extern IntPtr VerifierShutdown(IntPtr handle);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_provider_info")]
        public static extern void VerifierSetProviderInfo(IntPtr handle, string name, string scheme, string host, ushort port, string path);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_add_provider_transport")]
        public static extern void AddProviderTransport(IntPtr handle, string protocol, ushort port, string path, string scheme);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_filter_info")]
        public static extern void VerifierSetFilterInfo(IntPtr handle, string description, string state, byte noState);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_provider_state")]
        public static extern void VerifierSetProviderState(IntPtr handle, string url, byte teardown, byte body);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_verification_options")]
        public static extern void VerifierSetVerificationOptions(IntPtr handle,
                                                                 byte disableSslVerification,
                                                                 uint requestTimeout);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_publish_options")]
        public static extern void VerifierSetPublishOptions(IntPtr handle,
                                                            string providerVersion,
                                                            string buildUrl,
                                                            string[] providerTags,
                                                            ushort providerTagsLength,
                                                            string providerBranch);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_set_consumer_filters")]
        public static extern void VerifierSetConsumerFilters(IntPtr handle, string[] consumerFilters, ushort consumerFiltersLength);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_add_custom_header")]
        public static extern void AddCustomHeader(IntPtr handle, string name, string value);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_add_file_source")]
        public static extern void VerifierAddFileSource(IntPtr handle, string file);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_add_directory_source")]
        public static extern void VerifierAddDirectorySource(IntPtr handle, string directory);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_url_source")]
        public static extern void VerifierUrlSource(IntPtr handle, string url, string username, string password, string token);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_broker_source_with_selectors")]
        public static extern void VerifierBrokerSourceWithSelectors(IntPtr handle,
                                                                    string url,
                                                                    string username,
                                                                    string password,
                                                                    string token,
                                                                    byte enablePending,
                                                                    string includeWipPactsSince,
                                                                    string[] providerTags,
                                                                    ushort providerTagsLength,
                                                                    string providerBranch,
                                                                    string[] consumerVersionSelectors,
                                                                    ushort consumerVersionSelectorsLength,
                                                                    string[] consumerVersionTags,
                                                                    ushort consumerVersionTagsLength);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_execute")]
        public static extern int VerifierExecute(IntPtr handle);

        [DllImport(DllName, EntryPoint = "pactffi_verifier_logs")]
        public static extern IntPtr VerifierLogs(IntPtr handle);


        [DllImport(DllName, EntryPoint = "pactffi_verifier_output")]
        public static extern IntPtr VerifierOutput(IntPtr handle, byte stripAnsi);

        #endregion
    }
}
