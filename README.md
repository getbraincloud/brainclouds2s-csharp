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
  
