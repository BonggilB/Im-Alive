using System;
using System.Net.Sockets;


namespace MizeGSDK.Network
{
    public class NetServerClient
    {
        #region 변수
        private TcpClient client;

        private Action<NetServerClient, byte[], int> dataReceiveCallback = null;

        private Action<NetServerClient> clientDisconnectedCallback = null;

        private const int C_BUFFER_SIZE = 131072;
        #endregion


        #region Methods
        /// <summary>
        /// Ctr
        /// </summary>
        /// <param name="client"></param>
        public
        NetServerClient(TcpClient client,
                         Action<NetServerClient, byte[], int> dataReceiveCallback,
                         Action<NetServerClient> clientDisconnectedCallback)
        {
            this.client = client;

            this.dataReceiveCallback = dataReceiveCallback;

            this.clientDisconnectedCallback = clientDisconnectedCallback;


            startListening();
        }


        /// <summary>
        /// Start Listening to server
        /// </summary>
        private void startListening()
        {
            //try
            //{
            while ((client != null) && client.Connected && client.Available != 0 )
            {
                byte[] data = new byte[C_BUFFER_SIZE];
                var ㅋㅋ = client.Available;
                
                if (null == client?.GetStream())
                {
                    break;
                }
                try
                {
                    int len = client.GetStream()
                                    .Read(data,
                                           0,
                                           data.Length);

                    if (len == 0)
                    {
                        break;
                    }

                    byte[] pureData = new byte[len];

                    Array.Copy(data,
                                pureData,
                                len);

                    dataReceiveCallback?.Invoke(this,
                                                 pureData,
                                                 len);
                }
                catch
                {
                    //ㅁㄴㅇㄹ
                }
            }
            //}
            //finally
            //{
            //    disconnect();
            //    //client.Dispose();
            //    //client = new TcpClient ();
            //}

            MizeGSDK.listenerReload();
        }


        /// <summary>
        /// Write byte
        /// </summary>
        public void
        write(string data)
        {
            byte[] bData = System.Text.Encoding.UTF8.GetBytes(data);

            write(bData);
        }


        /// <summary>
        /// Write byte
        /// </summary>
        /// <param name="data"></param>
        public void
        write(byte[] data)
        {
            try
            {
                client?.GetStream()
                       .Write(data,
                               0,
                               data.Length);
            }
            catch (Exception)
            {

            }
        }



        /// <summary>
        /// Disconnect
        /// </summary>
        public void
        disconnect()
        {
            //write("DISCONNECT");

            if (client != null)
            {
                try
                {
                    client.GetStream()?.Close();
                }
                catch
                {
                    client.Dispose();
                }
                client?.Close();
            }

            clientDisconnectedCallback?.Invoke(this);
        }
        #endregion
    }
}
