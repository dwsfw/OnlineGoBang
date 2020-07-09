using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace GoBangServer
{
    class User
    {
        public TcpClient client { get; private set; }
        public BinaryReader br { get; private set; }
        public BinaryWriter bw { get; private set; }
        public string userName { get; set; }
        public bool isFight { get; set; }//是否为对战状态
        public User(TcpClient client)
        {
            this.client = client;
            NetworkStream networkStream = client.GetStream();
            br = new BinaryReader(networkStream);
            bw = new BinaryWriter(networkStream);
            isFight = false;
        }

        public void Close()
        {
            br.Close();
            bw.Close();
            client.Close();
        }
    }
}
