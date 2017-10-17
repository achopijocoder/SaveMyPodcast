using System;
using System.Net;
using System.Threading;
using System.Net.WebSockets;
using System.IO;
using NAudio.Wave;

namespace HttpListenerWebSocketEcho
{         
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.Start("http://localhost:11000/");
            Console.WriteLine("Pulsa cualquier tecla para salir ...");
            Console.ReadKey();
        }
    }
  
    class Server
    {
        //contador de streamings recibidos
        private int count = 0;

        /// <summary>
        /// Espera un WebSocket connection y entonces lo procesa `ProcessRequest`, en otro caso devuelve 400
        /// </summary>
        /// <param name="listenerPrefix"></param>
        public async void Start(string listenerPrefix)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }

        /// <summary>
        /// Procesa la peticion del socket
        /// </summary>
        /// <param name="listenerContext"></param>        
        private async void ProcessRequest(HttpListenerContext listenerContext)
        {

            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref count);
                Console.WriteLine("Recibidos: {0}", count);
            }
            catch (Exception e)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            try
            {
                //Recibir datos
                byte[] receiveBuffer = new byte[5000];

                //Especifica nombre del fichero a guardar
                string fileName = String.Format("{0}_{1}.wav",count.ToString(), DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                //crea el fichero donde se irán escribiendo los bytes recibidos
                var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                WaveFileWriter writer;
                //writer = new WaveFileWriter("file.wav", new WaveFormat(48000, 16, 1));

                // Mientras el websocket esté abierto, escucha los datos que van llegando
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    //si el cliente cerró el socket
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                        //se cierra el stream del fichero
                        fs.Close();
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);
                        //se cierra el stream del fichero porque son text data y queremos binary data
                        fs.Close();
                    }
                    else
                    {
                        //escribimos a fichero los bytes recibidos
                        fs.Write(receiveBuffer, 0, receiveResult.Count);
                        //writer.Write(receiveBuffer, 0, receiveResult.Count);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }
    }
}