## WebSocket.Portable

WebSocket.Portable is a PCL Profile 259 C# implementation of the [WebSocket protocol](https://tools.ietf.org/html/rfc6455).

This project is a binding library that makes use of native (Java / Objective-C) websockets to get around the limitations and jank of mono based websockets.


### Platforms

- Android
- iOS
- Universal
- Xamarin
- .Net Core 
- PCL


### NuGet
Coming Soon

### Setup

**Android**
- Include Websockets.Droid and Websockets  (PCL) library

**Ios**
- Include Websockets.Ios and Websockets  (PCL) library
- Include Square.SocketRocket

**Xamarin Forms**
- Include the Websockets (PCL) library in the main common app
- Include the platform specific stuff in the platform projects (Like Above)

**.Net Core (MVC, Console, ect)**
- Include Websockets.Net and Websockets  (PCL) library

- **Windows 10 Universal**
- Include Websockets.Universal and Websockets  (PCL) library

- **Windows 8 Phone**
- Cry (to me)


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

There is a XamarinApp.Droid example in the /Examples/ folder.

### TODO

- Support other platforms. Will implement as requested.


### Questions

Post onto the Github [issue system](https://github.com/NVentimiglia/WebSocket.PCL) or contact me via my [website](http://avariceonline.com)
