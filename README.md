## WebSockets.Pcl

WebSockets.PCL is a portable class library, profile 259, C# WebSocket implementation.

The motivation for this project was three part :

1) Having a completely different websocket implementation on every platform is a pain.

2) OkHttp.Ws Crashes with a fatal signal 11 if you loose internet

3) My original Mono implementation could not support TLS or SSL (Because Mono is janky)

This project is a binding library that makes use of native websockets to get around the limitations and jank of mono based websockets. On IOS I use SocketRocket because it works fine. On Android I wrote a custom binding library in Java and use AndroidAsync. On the WP8 we wrap around Websockets4Net. On the other platforms we wrap the default MSDN implementation. Really the most valuable part is the android implementation... but why not unify ?


https://www.nuget.org/packages/Websockets.Pcl/

### Platforms

- Android
- iOS
- WP8
- Universal
- Xamarin
- .Net Core 
- PCL


### NuGet

https://www.nuget.org/packages/Websockets.Pcl/

### Setup

**Android**
- Include Websockets.Droid and Websockets (PCL) library

**Ios**
- Include Websockets.Ios and Websockets (PCL) library
- Include Square.SocketRocket

**Xamarin Forms**
- Include the Websockets (PCL) library in the main common app
- Include the platform specific stuff in the platform projects (Like Above)

**.Net Core (MVC, Console, ect)**
- Include Websockets.Net and Websockets (PCL) library

**Windows 10 Universal**
- Include Websockets.Universal and Websockets (PCL) library

**Windows 8 Phone**
- Include Websockets.WP8 and Websockets (PCL) library
- Include Websockets4Net


### Usage

`````
        void Configure()
        {
            // Call in your platform (non-pcl) startup            
            // 1) Link in your main activity or AppDelegate or whatever
            Websockets.Droid.WebsocketConnection.Link();
        }
        
        
        void Connect()
        {
            // 2) Get a websocket from your PCL library via the factory
            connection = Websockets.WebSocketFactory.Create();
            connection.OnOpened += Connection_OnOpened;
            connection.OnMessage += Connection_OnMessage;
        }

        void Send()
        {            
            connection.Open("http://echo.websocket.org");
            connection.Send("Hello World");
        }

        private void Connection_OnOpened()
        {
            Debug.WriteLine("Opened !");
        }

        private void Connection_OnMessage(string obj)
        {
            Echo = obj == "Hello World";
        }
`````

### Example

There are a few 'test' examples (projects with the Tests suffix). Take a look there. The relivent code is in a standalone test file.

### TODO

- Support other platforms. Will implement as requested.


### Questions

Post onto the Github [issue system](https://github.com/NVentimiglia/WebSocket.PCL) or contact me via my [website](http://avariceonline.com)
