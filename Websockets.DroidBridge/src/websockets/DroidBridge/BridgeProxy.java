package websockets.DroidBridge;

/**
 * Created by Nicholas on 1/23/2016.
 * Defines the C# callback handler
 */
public class BridgeProxy {

    public void RaiseOpened     (){    }
    public void RaiseClosed     (){    }
    public void RaiseMessage    (String message){    }
    public void RaiseData       (byte[] data){    }
    public void RaiseLog        (String message){    }
    public void RaiseError      (String message){    }
    public void RaiseError      (Exception ex){    }
}
