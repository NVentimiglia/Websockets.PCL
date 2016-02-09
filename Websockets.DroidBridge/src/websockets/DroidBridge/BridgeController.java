
/**
 * Created by Nicholas Ventimiglia on 11/27/2015.
 * nick@avariceonline.com
 * <p/>
 * Android Websocket bridge application. Beacause Mono Networking sucks.
 * Unity talks with BridgeClient (java) and uses a C Bridge to raise events.
 * .NET Methods <-->  BridgeClient (Java / NDK) <--->  Websocket (Java)
 */
package websockets.DroidBridge;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;

//https://github.com/koush/AndroidAsync
import com.koushikdutta.async.callback.CompletedCallback;
import com.koushikdutta.async.http.AsyncHttpClient;
import com.koushikdutta.async.http.WebSocket;


public class BridgeController {

    private WebSocket mConnection;
    private static String TAG = "websockets";

    //MUST BE SET
    public BridgeProxy proxy;
    Handler mainHandler;


    public BridgeController() {
        Log.d(TAG, "ctor");
        mainHandler   = new Handler(Looper.getMainLooper());
    }

    // connect websocket
    public void Open(final String wsuri, final String protocol) {
        Log("BridgeController:Open");

        AsyncHttpClient.getDefaultInstance().websocket(wsuri, protocol, new AsyncHttpClient
                .WebSocketConnectCallback()
        {
            @Override
            public void onCompleted(Exception ex, WebSocket webSocket)
            {
                if (ex != null)
                {
                    Error(ex.toString());
                    return;
                }

                mConnection = webSocket;
                RaiseOpened();

                webSocket.setClosedCallback(new CompletedCallback()
                {
                    @Override
                    public void onCompleted(Exception e)
                    {
                        mConnection = null;
                        RaiseClosed();
                    }
                });


                webSocket.setStringCallback(new WebSocket.StringCallback()
                {
                    public void onStringAvailable(final String s)
                    {
                        RaiseMessage(s);
                    }
                });
            }
        });
    }

    public void Close() {

        try
        {
            if(mConnection == null)
                return;
            mConnection.close();

        }catch (Exception ex){
            RaiseError("Error Close - "+ex.getMessage());
        }
    }

    // send a message
    public void Send(final String message) {
        try
        {
            if(mConnection == null)
                return;
            mConnection.send(message);
        }catch (Exception ex){
            RaiseError("Error Send - "+ex.getMessage());
        }
    }

    private void Log(final String args) {
        Log.d(TAG, args);

        RaiseLog(args);
    }

    private void Error(final String args) {
        Log.e(TAG, args);

        RaiseError(String.format("Error: %s", args));
    }

    private void RaiseOpened() {
        if(proxy != null)
            proxy.RaiseOpened();
    }

    private void RaiseClosed() {
        if(proxy != null)
            proxy.RaiseClosed();
    }

    private void RaiseMessage(String message) {
        if(proxy != null)
            proxy.RaiseMessage(message);
    }

    private void RaiseLog(String message) {
        if(proxy != null)
            proxy.RaiseLog(message);
    }

    private void RaiseError(String message) {
        if(proxy != null)
            proxy.RaiseError(message);
    }
}
