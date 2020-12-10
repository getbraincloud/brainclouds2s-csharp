﻿using System;
using System.Collections;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;

namespace s2sTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BrainCloudS2S context = new BrainCloudS2S();
            context.Init("", "TestServer", "", "https://internal.braincloudservers.com/s2sdispatcher");
            //context.Init("", "", "");
            context.LoggingEnabled = true;

            //SEND AS DICTIONARY
            Dictionary<string, object> request = new Dictionary<string, object>();
            request.Add("service", "lobby");
            request.Add("operation", "GET_LOBBY_DATA");
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("lobbyId", "1283712");
            //data.Add("context", "test");
            request.Add("data", data);
            //context.Request(request, testCallback);
            //context.Request(request, testCallback);
            //context.Request(request, null);
            //context.Request(request, testCallback);

            //SEND AS STRING
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", testCallback);
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", testCallback);
            //context.request("{\"service\":\"time\",\"operation\":\"READ\"}", null);


            while (true)
            {
                context.RunCallbacks();
            }
        }
        static void testCallback(string response)
        {
            Console.WriteLine("CALLBACK SUCCESS");
        }
    }
}