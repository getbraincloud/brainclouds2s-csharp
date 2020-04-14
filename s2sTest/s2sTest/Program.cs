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
            BrainClouds2s context = new BrainClouds2s();
            context.Init("20001", "TestServer", "2ddf8355-c516-48dd-a6b0-e35bd75fac80");
            context.LoggingEnabled = true;
            
            Console.WriteLine(
                "\nIsInitialised: " + context.IsInitialized +
                "\nServerURL: " + context.ServerURL +
                "\nAppId: " + context.AppId + 
                "\nServerSecret: " + context.ServerSecret + 
                "\nServerName: " + context.ServerName + 
                "\nSessionID: " + context.SessionId
                );

            //Dictionary<string, object> request = new Dictionary<string, object>();
            //request.Add("service", "time");
            //request.Add("operation", "READ");
            //Dictionary<string, object> data = new Dictionary<string, object>();
            //request.Add("data", data);

            //string json = JsonWriter.Serialize(request);
            //context.request(json);

            //context.request("{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}");
            context.request("{\"service\":\"time\",\"operation\":\"READ\",\"data\":\"{}\"}");
            context.request("{\"service\":\"time\",\"operation\":\"READ\"}");

            while (true)
            {
                context.runCallbacks();
            }
        }
    }
}