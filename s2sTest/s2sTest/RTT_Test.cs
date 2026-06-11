using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace s2sTest
{
    class RTT_Test
    {
        public enum RTTConnectionStatus
        {
            CONNECTED,
            DISCONNECTED,
            CONNECTING,
            DISCONNECTING
        }
        public enum WebsocketStatus
        {
            OPEN,
            CLOSED,
            MESSAGE,
            ERROR,
            NONE
        }
        static void Main(string[] args)
        {
            // Load ids.txt
            string s2sUrl = "";
            string appId = "";
            string serverName = "";
            string serverSecret = "";
            Dictionary<string, object> _rttHeaders;
            Dictionary<string, object> _endpoint;
            RTTConnectionStatus _rttConnectionStatus = RTTConnectionStatus.DISCONNECTED;
            WebsocketStatus _webSocketStatus = WebsocketStatus.NONE;

            using (var reader = new StreamReader("ids.txt"))
            {
                Console.WriteLine("Found ids.txt");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("s2sUrl="))
                    {
                        s2sUrl = line.Substring(("s2sUrl=").Length);
                        s2sUrl.Trim();
                    }
                    else if (line.StartsWith("appId="))
                    {
                        appId = line.Substring(("appId=").Length);
                        appId.Trim();
                    }
                    else if (line.StartsWith("serverSecret="))
                    {
                        serverSecret = line.Substring(("serverSecret=").Length);
                        serverSecret.Trim();
                    }
                    else if (line.StartsWith("serverName="))
                    {
                        serverName = line.Substring(("serverName=").Length);
                        serverName.Trim();
                    }
                }
            }

            BrainCloudS2S context = new BrainCloudS2S();
            context.Init(appId, serverName, serverSecret, false, s2sUrl);
            context.LoggingEnabled = true;

            void checkIfFail(string response)
            {
                try
                {
                    var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    if ((int)responseData["status"] != 200)
                    {
                        Console.WriteLine("ERROR: " + response);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            void onTestAuthenticationCallback(string response)
            {
                checkIfFail(response);

                Console.WriteLine("\n----- PASS -----");
                //once authenticated we want to enable RTT
                if(context.SessionId != null)
                {
                    Dictionary<string, object> requstInfo = new Dictionary<string, object>();
                    requstInfo = new Dictionary<string, object>();
                    requstInfo.Add("service", "rttRegistration");
                    requstInfo.Add("operation", "REQUEST_SYSTEM_CONNECTION");
                    string contextInfo = JsonWriter.Serialize(requstInfo);
                    context.Request(contextInfo, onRTTConnectRequestCallback);
                }
            }

            void onRTTConnectRequestCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                Array endpoints = (Array)data["endpoints"];
                _rttHeaders = (Dictionary<string, object>)data["auth"];
                Console.WriteLine("RTT Connect Request Callback");

                //   1st choice: websocket + ssl
                //   2nd: websocket
                _endpoint = getEndpointForType(endpoints, "ws", true);
                if (_endpoint == null)
                {
                    _endpoint = getEndpointForType(endpoints, "ws", false);
                }
                if (_rttConnectionStatus == RTTConnectionStatus.DISCONNECTED)
                {
                    bool sslEnabled = (bool)_endpoint["ssl"];
                    string url = (sslEnabled ? "wss://" : "ws://") + _endpoint["host"] as string + ":" + (int)_endpoint["port"] + getUrlQueryParameters();
                    setupWebSocket(url);
                }

            }

            //capture lobby id from user input for now
            Console.Write("Enter Lobby ID to connect to: ");
            string lobbyId = Console.ReadLine();

            context.Authenticate(onTestAuthenticationCallback);
            

        }

        private static Dictionary<string, object> getEndpointForType(Array endpoints, string type, bool in_bWantSsl)
        {
            Dictionary<string, object> toReturn = null;
            Dictionary<string, object> tempToReturn = null;
            for (int i = 0; i < endpoints.Length; ++i)
            {
                tempToReturn = endpoints.GetValue(i) as Dictionary<string, object>;
                if (tempToReturn["protocol"] as string == type)
                {
                    if (in_bWantSsl)
                    {
                        if ((bool)tempToReturn["ssl"])
                        {
                            toReturn = tempToReturn;
                            break;
                        }
                    }
                    else
                    {
                        toReturn = tempToReturn;
                        break;
                    }
                }
            }

            return toReturn;
        }

    }
}
