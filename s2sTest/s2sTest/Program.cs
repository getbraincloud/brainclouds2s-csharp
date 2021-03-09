using System;
using System.Collections;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;

namespace s2sTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int successCounter = 0;
            Int64 lastServerTime = 0;
            BrainCloudS2S context = new BrainCloudS2S();
            context.Init("20001", "TestServer", "2ddf8355-c516-48dd-a6b0-e35bd75fac80", false, "https://internal.braincloudservers.com/s2sdispatcher");
            context.LoggingEnabled = true;

            //Begin Tests
            //Test 1 - Test Auth
            Console.WriteLine("\n-----Test1 Authentication START-----");
            context.Authenticate(onTestAuthenticationCallback);


            while (true)
            {
                context.RunCallbacks();
            }

            void onTestAuthenticationCallback(string response)
            {
                successCounter++;
                if (successCounter == 1)
                {
                    Console.WriteLine("\n-----Test1 Authentication PASS-----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 2 - Test Multiple Auth
                    Console.WriteLine("\n-----Test2 Multi Auth START-----");
                    context.Authenticate(onTestMultiAuthCallback);
                    context.Authenticate(onTestMultiAuthCallback);
                }
            }

            void onTestMultiAuthCallback(string response)
            {
                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n-----Test2 Multi Auth PASS-----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 3 - Test Auth and Request
                    Console.WriteLine("\n-----Test3 Auth and Request START-----");
                    context.Authenticate(onAuthAndRequestCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onAuthAndRequestCallback);
                }
            }

            void onAuthAndRequestCallback(string response)
            {
                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n-----Test3 Auth and Request PASS-----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 4 - Test Empty Auth and Request
                    Console.WriteLine("\n-----Test4 Empty Auth and Request START-----");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onEmptyAuthAndRequestCallback);
                }
            }

            void onEmptyAuthAndRequestCallback(string response)
            {
                successCounter++;
                if (successCounter == 1)
                {
                    Console.WriteLine("\n-----Test4 Auth and Request PASS-----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 5 - Test Queue
                    Console.WriteLine("\n-----Test5 Queue START-----");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                }
            }

            void onTestQueueCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                Int64 serverTime = (Int64)data["server_time"];

                if(serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue 
                {
                    successCounter++;
                    lastServerTime = serverTime;
                }

                if (successCounter == 3)
                {
                    Console.WriteLine("\n-----Test5 Queue PASS-----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 6 - Test Queue with fail
                    Console.WriteLine("\n-----Test6 Queue With Fail START-----");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
                }
            }

            void onTestQueueWithFailCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                if (responseData.ContainsKey("data"))
                {
                    Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                    Int64 serverTime = (Int64)data["server_time"];

                    if (serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue 
                    {
                        successCounter++;
                        lastServerTime = serverTime;
                    }
                }

                if (successCounter == 2)
                {
                    Console.WriteLine("\n-----Test6 Queue With Fail PASS-----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 6 - Test Queue with fail
                    //expect 4 pass
                    Console.WriteLine("\n-----Test7 Queue With Random Fail START-----");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                }
            }

            void onTestQueueWithRandomFailCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                if (responseData.ContainsKey("data"))
                {
                    Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                    Int64 serverTime = (Int64)data["server_time"];

                    if (serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue 
                    {
                        successCounter++;
                        lastServerTime = serverTime;
                    }
                }

                if (successCounter == 4)
                {
                    Console.WriteLine("\n-----Test5 Queue With Random Fail PASS-----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;
                }
            }
        }

    }
}