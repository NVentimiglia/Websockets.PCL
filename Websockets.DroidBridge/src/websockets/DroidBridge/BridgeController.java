
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

import com.squareup.okhttp.*;
import com.squareup.okhttp.ws.WebSocket;
import com.squareup.okhttp.ws.WebSocketCall;
import com.squareup.okhttp.ws.WebSocketListener;
import okio.Buffer;

import java.io.IOException;

import static com.squareup.okhttp.ws.WebSocket.TEXT;


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

        try {
            OkHttpClient client = new OkHttpClient();
            Request request = new Request.Builder()
                    .url(wsuri)
                    .build();
            WebSocketCall call = WebSocketCall.create(client, request);
            call.enqueue(new WebSocketListener() {
                @Override
                public void onOpen(WebSocket webSocket, Response response) {
                    RaiseOpened();
                    mConnection = webSocket;
                }

                @Override
                public void onFailure(IOException e, Response response) {
                    RaiseError(e.getMessage());
                    RaiseClosed();
                    mConnection = null;
                }


                @Override
                public void onMessage(ResponseBody payload) throws IOException {
                 try
                 {
                     String mahString = payload.string();
                     payload.close();
                     RaiseMessage(mahString);

                 }catch (Exception ex){
                     RaiseError("Error onMessage - "+ex.getMessage());
                 }
                }

                @Override
                public void onPong(okio.Buffer buffer) {

                }


                @Override
                public void onClose(int i, String s) {
                    RaiseClosed();
                    mConnection = null;
                }
            });

            // Trigger shutdown of the dispatcher's executor so this process can exit cleanly.
            client.getDispatcher().getExecutorService().shutdown();

        }catch (Exception ex){
            Error("Open "+ex.getMessage());
        }

    }

    public void Close() {

        try
        {
            if(mConnection == null)
                return;
            mConnection.close(1000,"CLOSE_NORMAL");

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
            Log.d(TAG, message);
            RequestBody body = RequestBody.create(TEXT, message);

            mConnection.sendMessage(body);
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
