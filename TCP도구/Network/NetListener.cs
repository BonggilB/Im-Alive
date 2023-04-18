using Action;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MizeGSDK.Network
{
    public class NetListener
    {
        #region 변수
        private TcpListener listener;

        private List<NetServerClient> clients = new List<NetServerClient> ();

        public Action<NetServerClient, byte[], int> onReceiveData;

        public Action<NetServerClient> onClientDisconnect;

        private object closeObj = new object ();
        #endregion


        #region Methods
        /// <summary>
        /// Start
        /// </summary>
        public void start (int port, Action<NetServerClient, byte[], int> onDataReceived, Action<NetServerClient> onClientDisconnect)
        {
            this.onReceiveData = onDataReceived;

            this.onClientDisconnect = onClientDisconnect;
            // 처음 start 하는데 문제가 생길시 아래stop() 주석
            stop ();

            IPEndPoint endPoint = new IPEndPoint (IPAddress.Any, port);


            listener = new TcpListener (endPoint);

            startListening ();
        }


        /// <summary>
        /// Start Listening
        /// </summary>
        private void
        startListening ()
        {
            System.Action action = () =>
            {
                try
                {
                    listener?.Start ();

                    while (listener != null)
                    {
                        TcpClient client = null;

                        try
                        {
                            client = listener?.AcceptTcpClient ();
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        NetServerClient netClient = new NetServerClient (client,
                                                                         onClientDataReceived,
                                                                         onClientDisconnected);

                        clients.Add (netClient);
                    }
                }
                finally
                {
                    stop ();
                }
            };


            Thread thread = new Thread (() => action.Invoke ());

            thread.Start ();
        }


        /// <summary>
        /// On Client disconnected
        /// </summary>
        /// <param name="sender"></param>
        private void
        onClientDisconnected (NetServerClient sender)
        {
            onClientDisconnect?.Invoke (sender);

            clients.RemoveAll (x => x == sender);
        }


        /// <summary>
        /// Data recieved
        /// </summary>
        private void
        onClientDataReceived (NetServerClient sender,
                              byte[] data,
                              int len)
        {
            onReceiveData?.Invoke (sender,
                                   data,
                                   len);

            parse(sender,
                                        data,
                                        len);
        }


        /// <summary>
        /// Stop
        /// </summary>
        public void
        stop ()
        {
            lock (closeObj)
            {
                if (null == listener)
                {
                    return;
                }


                disconnectAllClients ();


                listener?.Server?.Close ();

                listener?.Stop ();

                listener = null;
            }
        }


        /// <summary>
        /// Disconnect all clients
        /// </summary>
        private void
        disconnectAllClients ()
        {
            if (null != clients)
            {
                clients.ForEach (x => x.disconnect ());

                clients.Clear ();
            }
        }
        #endregion

        public static void
        parse(NetServerClient client,
               byte[] data,
               int len)
        {
            /// Parse the command
        }
    }
}
