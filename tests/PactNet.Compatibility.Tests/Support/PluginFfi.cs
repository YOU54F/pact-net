using System;
using System.Runtime.InteropServices;
using System.Text;
using PactNet.Interop;

namespace PactNet.Compatibility.Tests.Support
{
    /// <summary>
    /// Plugin FFI calls PactNet does not bind (or binds only internally). Used where the PactNet DSL has no equivalent;
    /// every call site records an "ffi" route in <see cref="PluginWorld"/>.
    /// </summary>
    internal static class PluginFfi
    {
        private const string Dll = "pact_ffi";

        [DllImport(Dll, EntryPoint = "pactffi_using_plugin")]
        public static extern uint UsingPlugin(PactHandle pact, string name, string version);

        [DllImport(Dll, EntryPoint = "pactffi_cleanup_plugins")]
        public static extern void CleanupPlugins(PactHandle pact);

        [DllImport(Dll, EntryPoint = "pactffi_interaction_contents")]
        public static extern uint InteractionContents(InteractionHandle interaction, InteractionPart part, string contentType, string contents);

        [DllImport(Dll, EntryPoint = "pactffi_new_sync_message_interaction")]
        public static extern InteractionHandle NewSyncMessageInteraction(PactHandle pact, string description);

        [DllImport(Dll, EntryPoint = "pactffi_mock_server_matched")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool MockServerMatched(int port);

        [DllImport(Dll, EntryPoint = "pactffi_write_pact_file")]
        public static extern int WritePactFileForPort(int port, string directory, [MarshalAs(UnmanagedType.U1)] bool overwrite);

        [DllImport(Dll, EntryPoint = "pactffi_free_pact_handle")]
        public static extern uint FreePactHandle(PactHandle pact);

        [DllImport(Dll, EntryPoint = "pactffi_get_error_message")]
        private static extern int GetErrorMessage(byte[] buffer, int length);

        [DllImport(Dll, EntryPoint = "pactffi_verifier_json")]
        public static extern IntPtr VerifierJson(IntPtr handle);

        public static string LastError()
        {
            byte[] buffer = new byte[4096];
            int len = GetErrorMessage(buffer, buffer.Length);
            if (len <= 0)
            {
                return string.Empty;
            }

            int end = Array.IndexOf(buffer, (byte)0);
            return Encoding.UTF8.GetString(buffer, 0, end < 0 ? len : end);
        }

        /// <summary>Read and free an FFI-owned string</summary>
        public static string TakeString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string s = Marshal.PtrToStringUTF8(ptr);
            NativeInterop.FreeString(ptr);
            return s;
        }
    }
}
