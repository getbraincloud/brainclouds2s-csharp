using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;

namespace s2sTest
{
    class RTT_Test
    {
        // Run RTT tests against an already-authenticated context.
        // Invokes onDone on success or onFail(message) on any failure.
        public static void RunTests(BrainCloudS2S context, Action onDone, Action<string> onFail)
        {
            // EnableRTT sends REQUEST_SYSTEM_CONNECTION, opens the WebSocket, and performs
            // the RTT CONNECT handshake — the callback fires once the connection is live.
            context.EnableRTT(onRTTConnectedCallback);

            void onRTTConnectedCallback(string response)
            {
                if (!context.IsRTTEnabled())
                {
                    onFail("RTT not connected after EnableRTT callback — " + response);
                    return;
                }
                Console.WriteLine("\n----- PASS -----");
                context.DisableRTT();
                onDone();
            }
        }
    }
}
