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
            BrainCloudS2S context = new BrainCloudS2S();
            context.init("20001", "TestServer", "2ddf8355-c516-48dd-a6b0-e35bd75fac80", "https://internal.braincloudservers.com/s2sdispatcher");
            context.LoggingEnabled = true;

            //SEND AS DICTIONARY
            Dictionary<string, object> request = new Dictionary<string, object>();
            request.Add("service", "log");
            request.Add("operation", "LOG_INFO");
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("errorMessage", "test");
            data.Add("context", "test");
            request.Add("data", data);
            context.request(request, testCallback);
            //context.request(request, null);

            //SEND AS STRING

            context.request("{\"service\":\"time\",\"operation\":\"READ\"}", testCallback);
            //context.request("{\"service\":\"time\",\"operation\":\"READ\"}", null);


            while (true)
            {
                context.runCallbacks();
            }
        }
        static void testCallback(Dictionary<string, object> response)
        {
            Console.WriteLine("CALLBACK SUCCESS");
        }
    }
}