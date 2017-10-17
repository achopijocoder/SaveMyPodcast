using System;
using System.Net;
using System.Threading;
using System.Net.WebSockets;
using System.IO;
using NAudio.Wave;
using System.Configuration;

namespace HttpListenerWebSocketEcho
{
    public class Program
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var server = new Server();
            string path = ConfigurationManager.AppSettings["savingPath"];
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            server.Start("http://localhost:11000/");
            log.Info("Servidor arrancó...");
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
            Program.log.Info("Servidor escuchando...");

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
                Program.log.Info("Conexiones recibidas: " + count.ToString());
            }
            catch (Exception e)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                Program.log.Error("Exception: " + e.Message);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            try
            {
                //Recibir datos
                byte[] receiveBuffer = new byte[5000];
                int totalBytes = 0;
                //Especifica nombre del fichero a guardar
                string path = ConfigurationManager.AppSettings["savingPath"];
                string fileName = String.Format("{0}{1}_{2}.wav",path, count.ToString(), DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                Program.log.Info("Generando archivo..."+fileName);

                //crea el fichero donde se irán escribiendo los bytes recibidos
                var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                //WaveFileWriter writer;
                //writer = new WaveFileWriter("file.wav", WaveFormat.CreateIeeeFloatWaveFormat(48000,1));

                // Mientras el websocket esté abierto, escucha los datos que van llegando
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                    //si el cliente cerró el socket
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                        Program.log.Info("Se terminó de recibir el archivo..." + fileName);

                        //se cierra el stream del fichero
                        fs.Close();
                        //writer.Close();
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None);
                        //se cierra el stream del fichero porque son text data y queremos binary data
                        fs.Close();
                        //writer.Close();
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
                Program.log.Fatal("Exception: " + e.Message);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
            }
        }
    }
}