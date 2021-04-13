// [dsl] This needs to be rewritten.
// I had to hack it so it fails on the first fail. and also exit on success so it
// doesn't get stuck in infinite loop

using System;
using System.Collections;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace s2sTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load ids.txt
            string s2sUrl = "";
            string appId = "";
            string serverName = "";
            string serverSecret = "";
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

            string currentTestName = "";
            int successCounter = 0;
            Int64 lastServerTime = 0;
            BrainCloudS2S context = new BrainCloudS2S();
            context.Init(appId, serverName, serverSecret, false, s2sUrl);
            context.LoggingEnabled = true;
            Stopwatch stopwatch = new Stopwatch();


            void startTest(string testName)
            {
                currentTestName = testName;
                Console.WriteLine("\n-----" + currentTestName + " START-----");

                stopwatch.Restart();
            }


            //Begin Tests
            //Test 1 - Test Auth
            stopwatch.Start();
            startTest("TestAuthentication");
            context.Authenticate(onTestAuthenticationCallback);


            // Infinite loop, check for timeouts
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > 20000) // 20 sec. If a call takes more than that, something is clearly wrong and should fail
                {
                    testFail("Timedout");
                }

                context.RunCallbacks();
                Thread.Sleep(16); // 60 fps
            }


            void testFail(string message)
            {
                Console.WriteLine("\nTESTS FAIL - " + currentTestName + " - " + message);
                Environment.Exit(1);
            }

            void checkIfFail(string response)
            {
                try
                {
                    var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    if ((int)responseData["status"] != 200)
                    {
                        testFail("status != 200");
                    }
                }
                catch (Exception e)
                {
                    testFail(e.Message);
                }
            }

            void checkExpectFail(string response)
            {
                try
                {
                    var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    if ((int)responseData["status"] == 200)
                    {
                        testFail("status == 200. Expected fail");
                    }
                }
                catch (Exception e)
                {
                    testFail(e.Message);
                }
            }

            void onTestAuthenticationCallback(string response)
            {
                checkIfFail(response);

                Console.WriteLine("\n----- PASS -----");
                context.Disconnect();
                successCounter = 0;

                //Test 2 - Test Multiple Auth
                startTest("TestMultiAuth");
                context.Authenticate(onTestMultiAuthCallback);
                context.Authenticate(onTestMultiAuthCallback);
            }

            void onTestMultiAuthCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 3 - Test Auth and Request
                    startTest("AuthAndRequest");
                    context.Authenticate(onAuthAndRequestCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onAuthAndRequestCallback);
                }
            }

            void onAuthAndRequestCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 4 - Test Empty Auth and Request
                    startTest("EmptyAuthAndRequest");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onEmptyAuthAndRequestCallback);
                }
            }

            void onEmptyAuthAndRequestCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 1)
                {
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    successCounter = 0;

                    //Test 5 - Test Queue
                    startTest("TestQueue");
                    context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                }
            }

            void onTestQueueCallback(string response)
            {
                checkIfFail(response);

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
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 6 - Test Queue with fail
                    startTest("TestQueueWithFail");
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
                    checkIfFail(response);

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
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 6 - Test Queue with fail
                    //expect 4 pass
                    startTest("TestQueueWithRandomFail");
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
                    checkIfFail(response);

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
                    Console.WriteLine("\n----- PASS -----");
                    context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    Console.WriteLine("\nALL TESTS PASS! :tada:");
                    Environment.Exit(0);
                }
            }
        }
    }
}
