
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

import websockets.Websocket.WebSocket;
import websockets.Websocket.WebSocketEventHandler;
import websockets.Websocket.WebSocketException;
import websockets.Websocket.WebSocketMessage;

import javax.net.ssl.SSLContext;
import java.net.URI;
import java.net.URISyntaxException;
import java.security.KeyManagementException;
import java.security.NoSuchAlgorithmException;

public class BridgeController {

    private WebSocket mConnection;
    private static String TAG = "websockets";

    //MUST BE SET
    public BridgeProxy proxy;
    Handler mainHandler;


    public BridgeController() {
        Log.d(TAG, "ctor");
        mainHandler = new Handler(Looper.getMainLooper());
    }

    // connect websocket
    public void Open(final String wsuri, final String protocol) {
        Log("BridgeController:Open");

        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                try {
                    URI connectionUri = new URI(wsuri);
                    if (protocol == null || protocol.isEmpty())
                        mConnection = new WebSocket(connectionUri);
                    else
                        mConnection = new WebSocket(connectionUri, protocol);
                    addSocketEventsListener();
                    mConnection.connect();
                } catch (WebSocketException e) {
                    Error("BridgeController:Open:Exception " + e.getMessage());
                    Error(e.getMessage());
                } catch (URISyntaxException e) {
                    Error("BridgeController:Open:Exception " + e.getMessage() + " " + e.getReason());
                } catch (Exception e) {
                    Error("BridgeController:Open:Exception " + e.getMessage());
                }
            }
        });

        thread.start();
    }

    public void Close() {

        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (mConnection == null)
                    return;
                try {
                    mConnection.close(true);
                } catch (Exception ex) {
                    Error(ex.getMessage());
                }
            }
        });

        thread.start();


    }

    // send a message
    public void Send(final String message) {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (mConnection == null)
                    return;
                try {
                    mConnection.send(message);
                } catch (Exception ex) {
                    Error(ex.getMessage());
                }
            }
        });

        thread.start();

    }


    private void addSocketEventsListener() {
        mConnection.setEventHandler(new WebSocketEventHandler() {

            @Override
            public void onOpen() {
                RaiseOpened();
            }

            @Override
            public void onMessage(WebSocketMessage socketMessage) {
                try {
                    RaiseMessage(socketMessage.getText());
                } catch (Exception e) {
                    RaiseError(e.getMessage());
                }
            }

            @Override
            public void onClose() {
                RaiseClosed();
                mConnection = null;
            }

            @Override
            public void onForcedClose() {
                RaiseClosed();
                mConnection = null;
            }

            @Override
            public void onPing() {

            }

            @Override
            public void onPong() {

            }

            @Override
            public void onException(Exception error) {
                RaiseError(error.getMessage());
            }
        });
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
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (proxy == null)
                    return;
                try {
                    if (proxy != null)
                        proxy.RaiseOpened();
                } catch (Exception ex) {

                }
            }
        });
        thread.start();

    }

    private void RaiseClosed() {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (proxy == null)
                    return;
                try {
                    if (proxy != null)
                        proxy.RaiseClosed();
                } catch (Exception ex) {

                }
            }
        });
        thread.start();

    }

    private void RaiseMessage(final String message) {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (proxy == null)
                    return;
                try {
                    if (proxy != null)
                        proxy.RaiseMessage(message);
                } catch (Exception ex) {

                }

            }
        });
        thread.start();

    }

    private void RaiseLog(final String message) {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (proxy == null)
                    return;
                try {
                    if (proxy != null)
                        proxy.RaiseLog(message);
                } catch (Exception ex) {

                }

            }
        });
        thread.start();

    }

    private void RaiseError(final String message) {
        Thread thread = new Thread(new Runnable() {
            @Override
            public void run() {
                if (proxy == null)
                    return;
                try {
                    if (proxy != null)
                        proxy.RaiseError(message);
                } catch (Exception ex) {

                }

            }
        });
        thread.start();

    }
}
