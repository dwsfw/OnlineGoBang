using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GoBangServer
{
        public partial class Form1 : Form
        {
            /// <summary>
            /// 保存连接的所有用户
            /// </summary>
            private List<User> userList = new List<User>();
            /// <summary>
            /// 使用的本机IP地址
            /// </summary>
            IPAddress localAddress = IPAddress.Any;
            /// <summary>
            /// 监听端口
            /// </summary>
            private const int port = 8889;
            private TcpListener myListener;
            /// <summary>
            /// 是否正常退出所有接收线程
            /// </summary>
            bool isExit = false;
            public Form1()
            {
                InitializeComponent();
                lst_Status.HorizontalScrollbar = true;
                btn_Stop.Enabled = false;
            }
            /// <summary>
            /// 开始监听
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void btn_Start_Click(object sender, EventArgs e)
            {
                myListener = new TcpListener(localAddress, port);
                myListener.Start();
                AddItemToListBox(string.Format("开始在{0}:{1}监听客户端", localAddress, port));
                Thread myThread = new Thread(ListenClientConnect);
                myThread.Start();
                btn_Start.Enabled = false;
                btn_Stop.Enabled = true;
            }

            /// <summary>
            /// 监听客户端请求
            /// </summary>
            private void ListenClientConnect()
            {
                TcpClient newClient = null;
                while (true)
                {
                    ListenClientDelegate d = new ListenClientDelegate(ListenClient);
                    IAsyncResult result = d.BeginInvoke(out newClient, null, null);
                    //使用轮询方式来判断异步操作是否完成
                    while (result.IsCompleted == false)
                    {
                        if (isExit)
                            break;
                        Thread.Sleep(250);
                    }
                    //获取Begin 方法的返回值和所有输入/输出参数
                    d.EndInvoke(out newClient, result);
                    if (newClient != null)
                    {
                        //每接受一个客户端连接，就创建一个对应的线程循环接收该客户端发来的信息
                        User user = new User(newClient);
                        Thread threadReceive = new Thread(ReceiveData);
                        threadReceive.Start(user);
                        userList.Add(user);
                        AddItemToListBox(string.Format("[{0}]进入", newClient.Client.RemoteEndPoint));
                        AddItemToListBox(string.Format("当前连接用户数：{0}", userList.Count));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void ReceiveData(object userState)
            {
                User user = (User)userState;
                TcpClient client = user.client;
                while (!isExit)
                {
                    string receiveString = null;
                    ReceiveMessageDelegate d = new ReceiveMessageDelegate(ReceiveMessage);
                    IAsyncResult result = d.BeginInvoke(user, out receiveString, null, null);
                    //使用轮询方式来判断异步操作是否完成
                    while (!result.IsCompleted)
                    {
                        if (isExit)
                            break;
                        Thread.Sleep(250);
                    }
                    //获取Begin方法的返回值和所有输入/输出参数
                    d.EndInvoke(out receiveString, result);
                    if (receiveString == null)
                    {
                        if (!isExit)
                        {
                            AddItemToListBox(string.Format("与{0}失去联系，已终止接收该用户信息", client.Client.RemoteEndPoint));
                            RemoveUser(user);
                        }
                        break;
                    }
                    AddItemToListBox(string.Format("来自[{0}]:{1}", user.client.Client.RemoteEndPoint, receiveString));
                    string[] splitString = receiveString.Split(',');
                    switch (splitString[0])
                    {
                        case "Login":
                            user.userName = splitString[1];
                            AsyncSendToAllClient(user, receiveString);
                            break;
                        case "Logout":
                            AsyncSendToAllClient(user, receiveString);
                            RemoveUser(user);
                            return;
                        case "Talk":
                            string talkString = receiveString.Substring(splitString[0].Length + splitString[1].Length + 2);
                            AddItemToListBox(string.Format("[{0}]对[{1}]说：{2}", user.userName, splitString[1], talkString));
                            foreach (User target in userList)
                            {
                                if (target.userName == splitString[1])
                                {
                                    AsyncSendToClient(target, "talk," + user.userName + "," + talkString);
                                    break;
                                }
                            }
                            break;
                        case "Drop":
                            Drop(user, splitString);
                            break;
                        case "Error":
                            AddItemToListBox(string.Format("[{0}]出现错误{1}", user.client.Client.RemoteEndPoint, splitString[1]));
                            break;
                        case "Fight":
                            Fight(user, splitString);
                            break;
                        case "Ready":
                            Ready(user, splitString);
                            break;
                        case "AllReady":
                            AllReady(user, splitString);
                            break;
                        case "RequestResult":
                            RequestResult(user, splitString);
                            break;
                        case "Angin":
                            Again(user, splitString);
                            break;
                        case "Fail":
                            Fail(user, splitString);
                            break;
                        default:
                            AddItemToListBox("什么意思啊:" + receiveString);
                            break;
                    }
                }
            }

            private void Drop(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0}]下在({1},{2})", user.userName, splitString[3], splitString[4]));
                foreach (User target in userList)
                {
                    if (target.userName == splitString[1])
                    {
                        AsyncSendToClient(target, "Drop," + splitString[2] + "," + splitString[3] + "," + splitString[4]);
                        break;
                    }
                }
            }

            private void Fight(User user, string[] splitString)
            {
                foreach (User target in userList)
                {
                    if (target.userName == splitString[1])
                    {
                        if (target.isFight == true)
                        {
                            AsyncSendToClient(user, "RequestResult," + splitString[1] + ",Fail,对方已在对弈中");
                            return;
                        }
                        break;
                    }
                }
                AddItemToListBox(string.Format("[{0}]向[{1}]发出对战请求)", user.userName, splitString[1]));
                foreach (User target in userList)
                {
                    if (target.userName == splitString[1])
                    {
                        AsyncSendToClient(target, "Fight," + user.userName + ",向你发出对弈请求");
                        break;
                    }
                }
            }

            private void Ready(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0}]已准备)", user.userName));
                foreach (User target in userList)
                {
                    if (target.userName == splitString[1])
                    {
                        AsyncSendToClient(target, "Ready," + user.userName + ",已准备");
                        break;
                    }
                }
            }

            private void AllReady(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0},{1}]都已准备)", user.userName, splitString[1]));
                int cnt3 = 0;
                for (int i = 0; i < userList.Count; i++)
                {
                    if (cnt3 == 2) break;
                    if (userList[i].userName == splitString[1])
                    {
                        AsyncSendToClient(userList[i], "AllReady");
                        cnt3++;
                    }
                    else if (userList[i].userName == user.userName)
                    {
                        AsyncSendToClient(userList[i], "AllReady");
                        cnt3++;
                    }
                }
            }

            private void RequestResult(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0}]向[{1}]发出回答{2})", user.userName,
                    splitString[1], splitString[2]));
                int cnt = 0;
                for (int i = 0; i < userList.Count; i++)
                {
                    if (cnt == 2) break;
                    if (userList[i].userName == splitString[1])
                    {
                        AsyncSendToClient(userList[i], "RequestResult," + user.userName + "," + splitString[2]);
                        userList[i].isFight = true;
                        cnt++;
                    }
                    else if (userList[i].userName == user.userName)
                    {
                        userList[i].isFight = true;
                        cnt++;
                    }
                }
            }

            private void Again(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0}]向[{1}]发出离开)", user.userName, splitString[1]));
                int cnt1 = 0;
                for (int i = 0; i < userList.Count; i++)
                {
                    if (cnt1 == 2) break;
                    if (userList[i].userName == splitString[1])
                    {
                        AsyncSendToClient(userList[i], "Angin," + user.userName + "," + splitString[2]);
                        userList[i].isFight = false;
                        cnt1++;
                    }
                    else if (userList[i].userName == user.userName)
                    {
                        userList[i].isFight = false;
                        cnt1++;
                    }
                }
            }

            private void Fail(User user, string[] splitString)
            {
                AddItemToListBox(string.Format("[{0}]向[{1}]认输)", user.userName, splitString[1]));
                int cnt2 = 0;
                for (int i = 0; i < userList.Count; i++)
                {
                    if (cnt2 == 2) break;
                    if (userList[i].userName == splitString[1])
                    {
                        AsyncSendToClient(userList[i], "Win," + user.userName + ",向你认输");
                        userList[i].isFight = false;
                        cnt2++;
                    }
                    else if (userList[i].userName == user.userName)
                    {
                        userList[i].isFight = false;
                        cnt2++;
                    }
                }
            }

            /// <summary>
            /// 异步发送信息给所有客户
            /// </summary>
            /// <param name="user"></param>
            /// <param name="message"></param>
            private void AsyncSendToAllClient(User user, string message)
            {
                string command = message.Split(',')[0].ToLower();
                if (command == "login")
                {
                    for (int i = 0; i < userList.Count; i++)
                    {
                        AsyncSendToClient(userList[i], message);
                        if (userList[i].userName != user.userName)
                            AsyncSendToClient(user, "login," + userList[i].userName);
                    }
                }
                else if (command == "logout")
                {
                    for (int i = 0; i < userList.Count; i++)
                    {
                        if (userList[i].userName != user.userName)
                            AsyncSendToClient(userList[i], message);
                    }
                }
            }

            /// <summary>
            /// 异步发送message给user
            /// </summary>
            /// <param name="user"></param>
            /// <param name="message"></param>
            private void AsyncSendToClient(User user, string message)
            {
                SendToClientDelegate d = new SendToClientDelegate(SendToClient);
                IAsyncResult result = d.BeginInvoke(user, message, null, null);
                while (result.IsCompleted == false)
                {
                    if (isExit)
                        break;
                    Thread.Sleep(250);
                }
                d.EndInvoke(result);
            }

            private delegate void SendToClientDelegate(User user, string message);
            /// <summary>
            /// 发送message给user
            /// </summary>
            /// <param name="user"></param>
            /// <param name="message"></param>
            private void SendToClient(User user, string message)
            {
                try
                {
                    //将字符串写入网络流，此方法会自动附加字符串长度前缀
                    user.bw.Write(message);
                    user.bw.Flush();
                    AddItemToListBox(string.Format("向[{0}]发送：{1}", user.userName, message));
                }
                catch
                {
                    AddItemToListBox(string.Format("向[{0}]发送信息失败", user.userName));
                }
            }

            /// <summary>
            /// 移除用户
            /// </summary>
            /// <param name="user"></param>
            private void RemoveUser(User user)
            {
                userList.Remove(user);
                user.Close();
                AddItemToListBox(string.Format("当前连接用户数：{0}", userList.Count));
            }

            delegate void ReceiveMessageDelegate(User user, out string receiveMessage);
            /// <summary>
            /// 接收客户端发来的信息
            /// </summary>
            /// <param name="user"></param>
            /// <param name="receiveMessage"></param>
            private void ReceiveMessage(User user, out string receiveMessage)
            {
                try
                {
                    receiveMessage = user.br.ReadString();
                }
                catch (Exception ex)
                {
                    AddItemToListBox(ex.Message);
                    receiveMessage = null;
                }
            }

            private delegate void ListenClientDelegate(out TcpClient client);
            /// <summary>
            /// 接受挂起的客户端连接请求
            /// </summary>
            /// <param name="newClient"></param>
            private void ListenClient(out TcpClient newClient)
            {
                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    newClient = null;
                }
            }

            delegate void AddItemToListBoxDelegate(string str);
            /// <summary>
            /// 在ListBox中追加状态信息
            /// </summary>
            /// <param name="str">要追加的信息</param>
            private void AddItemToListBox(string str)
            {
                if (lst_Status.InvokeRequired)
                {
                    AddItemToListBoxDelegate d = AddItemToListBox;
                    lst_Status.Invoke(d, str);
                }
                else
                {
                    lst_Status.Items.Add(str);
                    lst_Status.SelectedIndex = lst_Status.Items.Count - 1;
                    lst_Status.ClearSelected();
                }
            }

            private void btn_Stop_Click(object sender, EventArgs e)
            {
                AddItemToListBox("开始停止服务，并依次使用户退出！");
                isExit = true;
                for (int i = userList.Count - 1; i >= 0; i--)
                {
                    RemoveUser(userList[i]);
                }
                //通过停止监听让myListener.AcceptTcpClient()产生异常退出监听线程
                myListener.Stop();
                btn_Start.Enabled = true;
                btn_Stop.Enabled = false;
            }

            private void Form1_FormClosing(object sender, FormClosingEventArgs e)
            {
                if (myListener != null)
                {
                    btn_Stop.PerformClick();
                }
            }

            private void groupBox1_Enter(object sender, EventArgs e)
            {

            }
        }
    
}
