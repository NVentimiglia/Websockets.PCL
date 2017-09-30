
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
import com.koushikdutta.async.ByteBufferList;
import com.koushikdutta.async.DataEmitter;
import com.koushikdutta.async.callback.CompletedCallback;
import com.koushikdutta.async.callback.DataCallback;
import com.koushikdutta.async.http.AsyncHttpClient;
import com.koushikdutta.async.http.AsyncHttpClientMiddleware;
import com.koushikdutta.async.http.AsyncHttpGet;
import com.koushikdutta.async.http.AsyncSSLEngineConfigurator;
import com.koushikdutta.async.http.WebSocket;
import com.koushikdutta.async.http.spdy.SpdyMiddleware;

import javax.net.ssl.SSLContext;
import javax.net.ssl.TrustManager;
import javax.net.ssl.X509TrustManager;
import java.security.cert.X509Certificate;
import java.util.Arrays;
import java.util.Map;
import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.SSLEngine;
import javax.net.ssl.SSLParameters;
import javax.net.ssl.SSLSession;


public class BridgeController {

    private WebSocket mConnection;
    private boolean mIsAllTrusted;
    private static final String TAG = "websockets";

    //MUST BE SET
    public BridgeProxy proxy;
    Handler mainHandler;


    public BridgeController() {
        Log.d(TAG, "ctor");
        mIsAllTrusted = false;
        mainHandler   = new Handler(Looper.getMainLooper());
    }

    // connect websocket
    public void Open(final String wsuri, final String protocol, final Map<String, String> headers) {
        Log.d(TAG, "BridgeController:Open");

        AsyncHttpClient client = AsyncHttpClient.getDefaultInstance();
        try {
            SSLContext sslContext = SSLContext.getInstance("TLS");
            TrustManager[] trustManagers = null;
            if (mIsAllTrusted) {
                trustManagers = new TrustManager[] {
                    new X509TrustManager() {
                        @Override public void checkClientTrusted(X509Certificate[] chain, String authType) {}
                        @Override public void checkServerTrusted(X509Certificate[] chain, String authType) {
                            for (X509Certificate c : chain) {
                                Log.d(TAG, "chain: " + c.getSubjectX500Principal().getName());
                            }
                        }
                        @Override public X509Certificate[] getAcceptedIssuers() { return new X509Certificate[]{}; }
                    }
                };
                Log.d(TAG, "override trustmanager");
            }
            sslContext.init(null, trustManagers, null);

            SpdyMiddleware middleware = client.getSSLSocketMiddleware();
            
            middleware.addEngineConfigurator(new AsyncSSLEngineConfigurator() {
                @Override
                public void configureEngine(SSLEngine ssle, AsyncHttpClientMiddleware.GetSocketData gsd, String string, int i) {
                    ssle.setEnabledProtocols(new String[]{"TLSv1", "TLSv1.1", "TLSv1.2"});
                }
            });
            middleware.setSSLContext(sslContext);
            if (mIsAllTrusted) {
                Log.d(TAG, "override trustmanager");
                middleware.setTrustManagers(trustManagers);
                middleware.setHostnameVerifier(new HostnameVerifier() {
                    @Override
                    public boolean verify(String s, SSLSession sslSession) {
                        return true;
                    }
                });
            }
            middleware.setSpdyEnabled(false);
            middleware.setConnectAllAddresses(true);
            
            Log.d(TAG, "SSLContext...");
            SSLParameters params = middleware.getSSLContext().getSupportedSSLParameters();
            for (String s : params.getProtocols()) {
                Log.d(TAG, "Context supported protocol: " + s);
            }
            for (String s : params.getCipherSuites()) {
                Log.d(TAG, "Context supported ciphers: " + s);
            }
        } catch (Exception e){
            Log.w("SSLCONFIG", e.toString(), e);
        }

        AsyncHttpGet get = new AsyncHttpGet(wsuri.replace("ws://", "http://").replace("wss://", "https://"));
        get.setFollowRedirect(true);
        get.setLogging(TAG, android.util.Log.ERROR);
        
        for (Map.Entry<String, String> entry : headers.entrySet()) {
            get.addHeader(entry.getKey(), entry.getValue());
        }
        
        client.websocket(get, protocol, new AsyncHttpClient.WebSocketConnectCallback() {
            @Override
            public void onCompleted(Exception ex, WebSocket webSocket) {
                if (ex != null) {
                    Log.e(TAG, String.format("onCompleted failed: %s", ex.getMessage()), ex);
                    RaiseError(ex);
                    return;
                }

                mConnection = webSocket;
                RaiseOpened();

                webSocket.setClosedCallback(new CompletedCallback() {
                    @Override
                    public void onCompleted(Exception e) {
                        mConnection = null;
                        RaiseClosed();
                    }
                });


                webSocket.setStringCallback(new WebSocket.StringCallback() {
                    @Override public void onStringAvailable(final String s) {
                        RaiseMessage(s);
                    }
                });
                
                webSocket.setDataCallback(new DataCallback() {
                    @Override public void onDataAvailable(DataEmitter de, ByteBufferList bbl) {
                        RaiseData(bbl.getAllByteArray());
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

    public void Send(final byte[] data) {
        try
        {
            if(mConnection == null)
                return;
            mConnection.send(data);
        }catch (Exception ex){
            RaiseError("Error Send - "+ex.getMessage());
        }
    }

    private void Log(final String args) {
        //Log.d(TAG, args);

        RaiseLog(args);
    }

    private void Error(final String args) {
        Log.e(TAG, args);

        RaiseError(String.format("Error: %s", args));
    }

    private void Error(Exception ex) {
        Log.e(TAG, String.format("Error: %s: %s: %s", ex.getClass().getName(), ex.getMessage(), Arrays.deepToString(ex.getStackTrace())));

        RaiseError(String.format("Error: %s: %s: %s", ex.getClass().getName(), ex.getMessage(), Arrays.deepToString(ex.getStackTrace())));
    }
    
    public void SetIsAllTrusted() {
        mIsAllTrusted = true;
    }

    private void RaiseOpened() {
      try{
          if(proxy != null)
              proxy.RaiseOpened();
      }catch(Exception ex){
          RaiseClosed();
          Error("Failed to Open");
      }
    }

    private void RaiseClosed() {
        try{
            if(proxy != null)
                proxy.RaiseClosed();
        }catch(Exception ex){
            // RaiseClosed();
            Error("Failed to Close");
        }
    }

    private void RaiseMessage(String message) {
        try{
            if(proxy != null)
                proxy.RaiseMessage(message);
        }catch(Exception ex){
            RaiseClosed();
            Error("Failed to Raise");
        }
    }

    private void RaiseData(byte[] data) {
        try{
            if(proxy != null)
                proxy.RaiseData(data);
        }catch(Exception ex){
            RaiseClosed();
            Error("Failed to Raise");
        }
    }

    private void RaiseLog(String message) {
        try{
            if(proxy != null)
                proxy.RaiseLog(message);
        }catch(Exception ex){
            RaiseClosed();
            Error("Failed to Log");
        }
    }

    private void RaiseError(String message) {
        try{
            if(proxy != null)
                proxy.RaiseError(message);
        }catch(Exception ex){
            RaiseClosed();
            Error("Failed to Error");
        }
    }

    private void RaiseError(Exception ex) {
        try{
            if(proxy != null)
                proxy.RaiseError(ex);
        }catch(Exception iex){
            RaiseClosed();
            Error(iex);
            Error("Failed to Error");
        }
    }
}
