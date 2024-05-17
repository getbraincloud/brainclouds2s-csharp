# brainclouds2s-csharp
s2s libs for csharp and unity

# how to use
## step 1
Add the `s2s_src/` folder to your project. You will need the JsonFx library that's included. 

## step 2
Instantiate the BrainCloudS2S class and call init.
  
ex.
```
BrainCloudS2S context = new BrainCloudS2S();
context.Init("appId", "serverName", "serverSecret", "serverURL", false);
```

## step 3
Either call authenticate, then call your request.
  
ex.
```
context.Authenticate();
context.Request("", null);
```

## step 4
Call runcallbacks in your update loop to keep handle your requests and keep the connection alive. It will heartbeat for you unless you disconnect. 
  
ex. 
```
update()
{
    context.RunCallbacks();
}
```
      
## step 5
If you want to make your own S2SCallback to retrieve data, make a static function that takes a string as the parameter.

ex. 
```
static void testCallback(string response){}
```

## Writing a request
You can either send the request as a json string, or a Dictionary<string,object>
  
string example:
```
context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", null);
```

Dictionary example:
```
Dictionary<string, object> request = new Dictionary<string, object>();
request.Add("service", "time");
request.Add("operation", "READ");            
context.request(request, null);
```
  
## Enable RTT
To use RTT features you must enable RTT which will connect to brainClouds WebSocket RTT server. 
```
brainCloudS2SRef.EnableRTT(responseString =>
{
    Debug.Log("Response: " + responseString);
});
```
###Register an RTT callback
This is how you can register a callback that will be triggered whenever you receive an RTT message from the brainCloud WebSocket server
```
brainCloudS2SRef.RegisterRTTRawCallback(responseString =>
{
    Debug.Log("Received a raw callback: " + responseString);
});
```

## Connect to a channel and send raw RTT Packets
After enabling and registering RTT callbacks, you will be able to connect to a channel and send raw RTT packets to this channel which will be received by others who are also connected to this channel.
```
brainCloudS2SRef.ConnectToChannel(channelId, responseString =>
{
    Debug.Log("Response for connecting to channel: " + responseString);
});
```
Once you have been connected to the channel, you can send raw RTT packets like this
```
Dictionary<string, object> jsonInfo = new Dictionary<string, object>();
var playerName = "braincloudTester";
jsonInfo["playerName"] = playerName;
jsonInfo["playerLevel"] = 5;
brainCloudS2SRef.SendRawRTTPacket(jsonInfo);
```

## Disconnecting RTT 
In order to disconnect from RTT you will need to call the following functions:
```
brainCloudS2SRef.DisableRTT();
brainCloudS2SRef.DeregisterRTTRawCallback();
```
For logging out the user and shutting down RTT, use this function:
```
brainCloudS2SRef.Disconnect();
```