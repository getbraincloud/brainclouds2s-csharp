using System;

namespace s2sTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BrainClouds2s context = new BrainClouds2s();
            context.Init("23730", "s2sTest", "24a3f6d6-2305-4e1d-9bc1-b39102d3be1b");
            
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