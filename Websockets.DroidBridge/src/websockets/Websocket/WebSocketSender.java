package websockets.Websocket;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.PrintStream;
import java.io.UnsupportedEncodingException;
import java.nio.ByteBuffer;
import java.security.SecureRandom;
import java.util.Queue;
import java.util.Random;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.LinkedBlockingQueue;

public class WebSocketSender {

    private static class Sender implements Runnable {
        private WebSocket connection;
        private PrintStream output;
        private final Random random = new SecureRandom();

        public Queue<Object> q = new LinkedBlockingQueue<Object>();

        public Sender send(byte opcode, boolean masking, byte[] data){
            q.add(data);
            q.add(opcode);
            q.add(masking);
            return this;
        }

        public Sender(PrintStream output, WebSocket connection) {
            this.connection = connection;
            this.output = output;
        }

        public void run() {
            if(q.size() > 0) {
                byte[] data = (byte[]) q.poll();
                byte opcode = (byte) q.poll();
                boolean masking = (boolean) q.poll();
                try {
                    sendAsync(opcode,masking,data);
                } catch (WebSocketException e) {

                }
            }
        }

        private void sendAsync(byte opcode, boolean masking, byte[] data) throws WebSocketException{
            if (!connection.isConnected()) {
                throw new WebSocketException(
                        "error while sending text data: not connected");
            }

            try {
                int headerLength = 2; // This is just an assumed headerLength, as we use a ByteArrayOutputStream
                if (masking) {
                    headerLength += 4;
                }
                ByteArrayOutputStream frame = new ByteArrayOutputStream(data.length + headerLength);

                byte fin = (byte) 0x80;
                byte startByte = (byte) (fin | opcode);
                frame.write(startByte);
                int length = data.length;
                int length_field = 0;

                if (length < 126) {
                    if (masking) {
                        length = 0x80 | length;
                    }
                    frame.write((byte) length);
                } else if (length <= 65535) {
                    length_field = 126;
                    if (masking) {
                        length_field = 0x80 | length_field;
                    }
                    frame.write((byte) length_field);
                    byte[] lengthBytes = intToByteArray(length);
                    frame.write(lengthBytes[2]);
                    frame.write(lengthBytes[3]);
                } else {
                    length_field = 127;
                    if (masking) {
                        length_field = 0x80 | length_field;
                    }
                    frame.write((byte) length_field);
                    // Since an integer occupies just 4 bytes we fill the 4 leading length bytes with zero
                    frame.write(new byte[]{0x0, 0x0, 0x0, 0x0});
                    frame.write(intToByteArray(length));
                }

                byte[] mask = null;
                if (masking) {
                    mask = generateMask();
                    frame.write(mask);

                    for (int i = 0; i < data.length; i++) {
                        data[i] ^= mask[i % 4];
                    }
                }

                frame.write(data);
                output.write(frame.toByteArray());
                output.flush();
            } catch (UnsupportedEncodingException uee) {
                throw new WebSocketException("error while sending text data: unsupported encoding", uee);
            } catch (IOException ioe) {
                throw new WebSocketException("error while sending text data", ioe);
            }
        }

        private byte[] generateMask()
        {
            final byte[] mask = new byte[4];
            random.nextBytes(mask);
            return mask;
        }

        private byte[] intToByteArray(int number)
        {
            byte[] bytes = ByteBuffer.allocate(4).putInt(number).array();
            return bytes;
        }
    }

    private Sender sender;
    private ExecutorService exec;

    public WebSocketSender(PrintStream output, WebSocket connection)  {
        exec = Executors.newSingleThreadExecutor();
        sender = new Sender(output, connection);
    }

    @Override
    protected void finalize() throws Throwable {
        super.finalize();
        if(exec != null && !exec.isShutdown() && !exec.isTerminated()){
            exec.shutdown();
        }
    }

    public void send(byte opcode, boolean masking, byte[] data) throws WebSocketException {
        if(exec != null && !exec.isShutdown() && !exec.isTerminated() && sender != null && data != null){
            exec.execute(sender.send(opcode, masking, data));
        }
    }
}
