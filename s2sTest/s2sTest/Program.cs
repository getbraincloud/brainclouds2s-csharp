using System;

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

            context.request("{\"service\":\"time\",\"operation\":\"READ\"}");

            while(true)
            {
                context.runCallbacks();
            }
        }
    }
}