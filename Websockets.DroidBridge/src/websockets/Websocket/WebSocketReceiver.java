/*
 *  Copyright (C) 2012 Roderick Baier
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  	http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License. 
 */

package websockets.Websocket;

import java.io.DataInputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;


public class WebSocketReceiver extends Thread
{
	private DataInputStream input = null;
	private WebSocket websocket = null;
	private WebSocketEventHandler eventHandler = null;

	private volatile boolean stop = false;


	public WebSocketReceiver(DataInputStream input, WebSocket websocket)
	{
		this.input = input;
		this.websocket = websocket;
		this.eventHandler = websocket.getEventHandler();
	}

	public void run()
	{
		List<Byte> messageBytes = new ArrayList<Byte>();

		while (!stop) {
			try {
				byte b = input.readByte();
				byte opcode = (byte) (b & 0xf);
				byte length = input.readByte();
				long payload_length = 0;
				if (length < 126) {
					payload_length = length;
				} else if (length == 126) {
					payload_length = ((0xff & input.readByte()) << 8) | (0xff & input.readByte());
				} else if (length == 127) {
					// Does work up to MAX_VALUE of long (2^63-1) after that minus values are returned.
					// However frames with such a high payload length are vastly unrealistic.
					// TODO: add Limit for WebSocket Payload Length.
					payload_length = input.readLong();
				}
				for (int i = 0; i < payload_length; i++) {
					messageBytes.add(input.readByte());
				}
				Byte[] message = messageBytes.toArray(new Byte[messageBytes.size()]);
				WebSocketMessage ws_message = new WebSocketMessage(message);
				eventHandler.onMessage(ws_message);
				messageBytes.clear();
			} catch (IOException ioe) {
				handleError();
			}
		}
	}

	public void stopit()
	{
		stop = true;
	}

	public boolean isRunning()
	{
		return !stop;
	}

	private void handleError()
	{
		stopit();
		websocket.handleReceiverError();
	}
}