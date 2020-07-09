using OnlineGobang.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace OnlineGobang
{
    public partial class Form1 : Form
    {

        //是否正常退出
        private bool isExit = false;
        private TcpClient client;
        private BinaryReader br;
        private BinaryWriter bw;
        BackgroundWorker connectWork = new BackgroundWorker();
        private string serverIP = "192.168.1.179";
        private bool isSingle = true;
        private bool isConnect = false;
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Random r = new Random((int)DateTime.Now.Ticks);
            txt_UserName.Text = "user" + r.Next(100, 999);//随机生成一个用户名
            lst_OnlineUser.HorizontalScrollbar = true;
            Music.PlaySong(@"../../Resources/background1.mp3");
            connectWork.DoWork += new DoWorkEventHandler(connectWork_DoWork);
            connectWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(connectWork_RunWorkerCompleted);

            foreach (Control cc in this.Controls)
            {
                if (cc == this.pictureBox5 || cc == this.btn_Contect || cc == this.btn_Single || cc == this.btn_close || cc == this.panel4)
                    continue;
                cc.Visible = false;
            }
            SetDouble(this.rtf_StatusInfo);
        }

        public static void SetDouble(Control cc)
        {
            cc.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.NonPublic).SetValue(cc, true, null);
        }

        /// <summary>
        /// 异步方式与服务器进行连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void connectWork_DoWork(object sender, DoWorkEventArgs e)
        {
            client = new TcpClient();
            IAsyncResult result = client.BeginConnect(serverIP, 8889, null, null);//连接服务器
            while (!result.IsCompleted)
            {
                Thread.Sleep(100);
                AddStatus(".");
            }
            try
            {
                client.EndConnect(result);
                e.Result = "success";
                isConnect = true;
            }
            catch (Exception ex)
            {
                e.Result = ex.Message;
                client = null;
                result = null;
                return;
            }
        }

        /// <summary>
        /// 异步方式与服务器完成连接操作后的处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void connectWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result.ToString() == "success")
            {
                AddStatus("连接成功\n");
                //获取网络流
                NetworkStream networkStream = client.GetStream();
                //将网络流作为二进制读写对象
                br = new BinaryReader(networkStream);
                bw = new BinaryWriter(networkStream);
                AsyncSendMessage("Login," + txt_UserName.Text);
                Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
                threadReceive.IsBackground = true;//随主线程结束
                threadReceive.Start();
            }
            else
            {
                AddStatus("连接失败:" + e.Result + "\n");
                btn_Login.Enabled = true;
            }
        }

        /// <summary>
        /// 处理接收的服务器收据
        /// </summary>
        private void ReceiveData()
        {
            string receiveString = null;
            while (!isExit)
            {
                ReceiveMessageDelegate d = new ReceiveMessageDelegate(receiveMessage);
                IAsyncResult result = d.BeginInvoke(out receiveString, null, null);
                //使用轮询方式来盘点异步操作是否完成
                while (!result.IsCompleted)
                {
                    if (isExit)
                        break;
                    Thread.Sleep(250);
                }
                //获取Begin方法的返回值所有输入/输出参数
                d.EndInvoke(out receiveString, result);
                if (receiveString == null)
                {
                    if (!isExit)
                        MessageBox.Show("与服务器失去联系");
                    break;
                }
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                switch (command)
                {
                    case "login":   //格式： login,用户名
                        AddOnline(splitString[1]);
                        break;
                    case "logout":  //格式： logout,用户名
                        RemoveUserName(splitString[1]);
                        break;
                    case "talk":    //格式： talk,用户名,对话信息
                        AddTalkMessage(splitString[1] + "：\r\n");
                        AddTalkMessage(receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        break;
                    case "drop":    //格式：drop,key,x坐标,y坐标
                        Drop(splitString);
                        break;
                    case "fight":    //格式： fight,发起者,对战者
                        Fight(splitString);
                        break;
                    case "ready":   //格式：ready
                        isOtherReady = true;
                        if (isMyReady == true && isOtherReady == true)
                        {
                            AsyncSendMessage("AllReady," + Opponent);//双方都准备好发送请求
                        }
                        LableText2(Opponent + "已准备");
                        break;
                    case "allready"://启动计时器，开始倒计时
                        if (!ThisPieceTypeIsWhite)
                        {
                            MyTimer.Start();
                        }
                        else OtherTimer.Start();
                        break;
                    case "requestresult":  //对战请求回复
                        RequestResult(splitString);
                        break;
                    case "angin":  //Again,发起者,离开方式
                        Aagin(splitString);
                        break;
                    case "win":
                        winPiece = ThisPieceTypeIsWhite;
                        Finish();
                        break;
                }
            }
            Application.Exit();
        }

        #region 数据处理方法
        private void Aagin(string[] splitString)
        {
            switch (splitString[2])
            {
                case "Leave":
                    MessageBox.Show("你的对手已离开，请重新选择对弈者");
                    ReadyNewGame();
                    Init();
                    Opponent = null;
                    ShowUserName();
                    TrueFight();
                    FalseStart();
                    break;
                case "FalseLeave":
                    MessageBox.Show("你的对手异常离开，请重新选择对弈者");
                    ReadyNewGame();
                    Init();
                    Opponent = null;
                    ShowUserName();
                    TrueFight();
                    FalseStart();
                    FalseOther();
                    ThisPieceTypeIsWhite = true;
                    break;
            }
        }

        private void RequestResult(string[] splitString)
        {
            switch (splitString[2])
            {
                case "Yes":
                    Opponent = splitString[1];
                    isTrueLeave = false;

                    HideUserName();
                    FalseFight();
                    TrueStart();
                    TrueOther();
                    LableText1(this.txt_UserName.Text);
                    LableText2(Opponent);
                    ChangePicture1(Resources.w);
                    ChangePicture2(Resources.b);

                    MessageBox.Show(Opponent + "接受你的对战请求");
                    break;
                case "No":
                    TrueFight();
                    MessageBox.Show(splitString[1] + "拒绝你的对战请求,请重新选择对弈对象");
                    break;
                case "NoFind":
                    TrueFight();
                    MessageBox.Show("没有找到名为" + splitString[1] + "的对象");
                    break;
                case "Fail":
                    TrueFight();
                    MessageBox.Show(splitString[1] + splitString[3]);
                    break;
            }
        }

        private void Fight(string[] splitString)
        {
            if (MessageBox.Show(splitString[1] + splitString[2], "对战请求", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                bool flag = false;
                for (int i = 0; i < this.lst_OnlineUser.Items.Count; i++)
                {
                    try
                    {
                        if (this.lst_OnlineUser.Items[i].ToString() == splitString[1])
                        {
                            flag = true;
                            Opponent = splitString[1];
                            isTrueLeave = false;
                            isWhite = false;
                            ThisPieceTypeIsWhite = false;

                            ChangeIndexUserName(i);
                            HideUserName();
                            FalseFight();
                            TrueStart();
                            TrueOther();
                            LableText1(this.txt_UserName.Text);
                            LableText2(Opponent);
                            ChangePicture1(Resources.b);
                            ChangePicture2(Resources.w);
                            AsyncSendMessage("RequestResult," + splitString[1] + ",Yes");
                        }
                    }
                    catch (Exception ex)
                    {
                        AsyncSendMessage("Error," + ex);

                    }

                }
                if (flag == false) AsyncSendMessage("RequestResult," + splitString[1] + ",NoFind");
            }
            else
                AsyncSendMessage("RequestResult," + splitString[1] + ",No");
        }

        private void Drop(string[] splitString)
        {
            try
            {
                Piece piece = board.GetPiece(int.Parse(splitString[2]), int.Parse(splitString[3]));
                if (int.Parse(splitString[1]) == 1)
                {
                    piece.pieceType = PieceType.White;
                    isWhite = false;
                }
                else
                {
                    piece.pieceType = PieceType.Black;
                    isWhite = true;
                }
                //绘制棋子
                piece.DrawPiece(this.gImage);
                ChangePicture(bmp);

                Music.play("drop");

                OtherTimer.Stop();
                MyTimer.Start();

                if (ThisPieceTypeIsWhite == isWhite) PictureCursorDefault();
                if (board.IsEndingGame(piece))
                {
                    winPiece = !ThisPieceTypeIsWhite;
                    Finish();
                }
                else if (board.isFillPiece()) Tie();
            }
            catch (Exception ex)
            {
                AsyncSendMessage("Error," + ex);
            }
        }
        #endregion

        /// <summary>
        /// 发送信息状态的数据结构
        /// </summary>
        private struct SendMessageStates
        {
            public SendMessageDelegate d;
            public IAsyncResult result;
        }

        /// <summary>
        /// 异步向服务器发送数据
        /// </summary>
        /// <param name="message"></param>
        private void AsyncSendMessage(string message)
        {
            SendMessageDelegate d = new SendMessageDelegate(SendMessage);
            IAsyncResult result = d.BeginInvoke(message, null, null);
            while (!result.IsCompleted)
            {
                if (isExit)
                    return;
                Thread.Sleep(50);
            }
            SendMessageStates states = new SendMessageStates();
            states.d = d;
            states.result = result;
            Thread t = new Thread(FinishAsyncSendMessage);
            t.IsBackground = true;
            t.Start(states);
        }

        /// <summary>
        /// 处理接收的服务端数据
        /// </summary>
        /// <param name="obj"></param>
        private void FinishAsyncSendMessage(object obj)
        {
            SendMessageStates states = (SendMessageStates)obj;
            states.d.EndInvoke(states.result);
        }

        delegate void SendMessageDelegate(string message);
        /// <summary>
        /// 向服务端发送数据
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(string message)
        {
            try
            {
                bw.Write(message);
                bw.Flush();
            }
            catch
            {
                AddStatus("发送失败\n");
            }
        }

        delegate void ReceiveMessageDelegate(out string receiveMessage);
        /// <summary>
        /// 读取服务器发过来的信息
        /// </summary>
        /// <param name="receiveMessage"></param>
        private void receiveMessage(out string receiveMessage)
        {
            receiveMessage = null;
            try
            {
                receiveMessage = br.ReadString();
            }
            catch (Exception ex)
            {
                AddStatus(ex.Message + "\n");
            }
        }

        #region 控件委托
        private delegate void AddTalkMessageDelegate(string message);
        /// <summary>
        /// 向 rtf 中添加聊天记录
        /// </summary>
        /// <param name="message"></param>
        private void AddTalkMessage(string message)
        {
            if (rtf_MessageInfo.InvokeRequired)
            {
                AddTalkMessageDelegate d = new AddTalkMessageDelegate(AddTalkMessage);
                rtf_MessageInfo.Invoke(d, new object[] { message });
            }
            else
            {
                rtf_MessageInfo.AppendText(message);
                rtf_MessageInfo.ScrollToCaret();
            }
        }

        private delegate void AddStatusDelegate(string message);
        /// <summary>
        /// 向 rtf 中添加状态信息
        /// </summary>
        /// <param name="message"></param>
        private void AddStatus(string message)
        {
            if (rtf_StatusInfo.InvokeRequired)
            {
                AddStatusDelegate d = new AddStatusDelegate(AddStatus);
                rtf_StatusInfo.Invoke(d, new object[] { message });
            }
            else
            {
                rtf_StatusInfo.AppendText(message);
            }
        }

        private delegate void AddOnlineDelegate(string message);
        /// <summary>
        /// 向 lst_Online 添加在线用户
        /// </summary>
        /// <param name="message"></param>
        private void AddOnline(string message)
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                AddOnlineDelegate d = new AddOnlineDelegate(AddOnline);
                lst_OnlineUser.Invoke(d, new object[] { message });
            }
            else
            {
                lst_OnlineUser.Items.Add(message);
                lst_OnlineUser.SelectedIndex = lst_OnlineUser.Items.Count - 1;
                lst_OnlineUser.ClearSelected();
            }
        }

        private delegate void RemoveUserNameDelegate(string userName);
        /// <summary>
        /// 从 listBoxOnline 删除离线用户
        /// </summary>
        /// <param name="userName"></param>
        private void RemoveUserName(string userName)
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                RemoveUserNameDelegate d = RemoveUserName;
                lst_OnlineUser.Invoke(d, userName);
            }
            else
            {
                lst_OnlineUser.Items.Remove(userName);
                lst_OnlineUser.SelectedIndex = lst_OnlineUser.Items.Count - 1;
                lst_OnlineUser.ClearSelected();
            }
        }
        private delegate void HideUserNameDelegate();
        /// <summary>
        /// 隐藏listbox
        /// </summary>
        /// <param name="userName"></param>
        private void HideUserName()
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                HideUserNameDelegate d = HideUserName;
                lst_OnlineUser.Invoke(d);
            }
            else
            {
                lst_OnlineUser.Hide();
            }
        }
        private delegate void ShowUserNameDelegate();
        /// <summary>
        /// 显示listbox
        /// </summary>
        /// <param name="userName"></param>
        private void ShowUserName()
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                HideUserNameDelegate d = ShowUserName;
                lst_OnlineUser.Invoke(d);
            }
            else
            {
                lst_OnlineUser.Show();
            }
        }
        private delegate void ChangeIndexUserNameDelegate(int i);
        /// <summary>
        /// 显示listbox
        /// </summary>
        /// <param name="userName"></param>
        private void ChangeIndexUserName(int i)
        {
            if (lst_OnlineUser.InvokeRequired)
            {
                ChangeIndexUserNameDelegate d = ChangeIndexUserName;
                lst_OnlineUser.Invoke(d, i);
            }
            else
            {
                lst_OnlineUser.SelectedIndex = i;
            }
        }
        /// <summary>
        /// 控件变灰
        /// </summary>
        private delegate void EnableFalseDelegate();
        private void FalseFight()
        {
            if (btn_Fight.InvokeRequired)
            {
                EnableFalseDelegate d = FalseFight;
                btn_Fight.Invoke(d);
            }
            else
            {
                btn_Fight.Enabled = false;
            }
        }
        private void FalseStart()
        {
            if (btn_start.InvokeRequired)
            {
                EnableFalseDelegate d = FalseStart;
                btn_start.Invoke(d);
            }
            else
            {
                btn_start.Enabled = false;
            }
        }
        private void FalseFail()
        {
            if (btn_fail.InvokeRequired)
            {
                EnableFalseDelegate d = FalseFail;
                btn_fail.Invoke(d);
            }
            else
            {
                btn_fail.Enabled = false;
            }
        }
        private void FalseOther()
        {
            if (btn_Other.InvokeRequired)
            {
                EnableFalseDelegate d = FalseOther;
                btn_Other.Invoke(d);
            }
            else
            {
                btn_Other.Enabled = false;
            }
        }
        /// <summary>
        /// 恢复控件
        /// </summary>
        private delegate void EnableTrueDelegate();
        private void TrueFight()
        {
            if (btn_Fight.InvokeRequired)
            {
                EnableTrueDelegate d = TrueFight;
                btn_Fight.Invoke(d);
            }
            else
            {
                btn_Fight.Enabled = true;
            }
        }
        private void TrueStart()
        {
            if (btn_start.InvokeRequired)
            {
                EnableTrueDelegate d = TrueStart;
                btn_start.Invoke(d);
            }
            else
            {
                btn_start.Enabled = true;
            }
        }
        private void TrueFail()
        {
            if (btn_fail.InvokeRequired)
            {
                EnableTrueDelegate d = TrueFail;
                btn_fail.Invoke(d);
            }
            else
            {
                btn_fail.Enabled = true;
            }
        }
        private void TrueOther()
        {
            if (btn_Other.InvokeRequired)
            {
                EnableTrueDelegate d = TrueOther;
                btn_Other.Invoke(d);
            }
            else
            {
                btn_Other.Enabled = true;
            }
        }
        private delegate void LableDelegate(string text);
        private void LableText1(string text)
        {
            if (this.label2.InvokeRequired)
            {
                LableDelegate d = LableText1;
                label2.Invoke(d, text);
            }
            else
            {
                label2.Text = text;
            }
        }
        private void LableText2(string text)
        {
            if (this.label3.InvokeRequired)
            {
                LableDelegate d = LableText2;
                label3.Invoke(d, text);
            }
            else
            {
                label3.Text = text;
            }
        }

        private delegate void PictureDelegate(Bitmap bmp);
        private void ChangePicture(Bitmap bmp)
        {
            if (this.pictureBox1.InvokeRequired)
            {
                PictureDelegate d = ChangePicture;
                pictureBox1.Invoke(d, bmp);
            }
            else
            {
                pictureBox1.Image = bmp;
            }
        }
        private void ChangePicture1(Image img)
        {
            if (this.pictureBox2.InvokeRequired)
            {
                PictureDelegate d = ChangePicture1;
                pictureBox2.Invoke(d, img);
            }
            else
            {
                pictureBox2.BackgroundImage = img;
            }
        }
        private void ChangePicture2(Image img)
        {
            if (this.pictureBox3.InvokeRequired)
            {
                PictureDelegate d = ChangePicture2;
                pictureBox3.Invoke(d, img);
            }
            else
            {
                pictureBox3.BackgroundImage = img;
            }
        }
        private delegate void TextBoxDelegate(string time);
        private void ChangeTextBox1(string time)
        {
            if (this.textBox1.InvokeRequired)
            {
                TextBoxDelegate d = ChangeTextBox1;
                textBox1.Invoke(d, time);
            }
            else
            {
                textBox1.Text = time;
            }
        }
        private void ChangeTextBox2(string time)
        {
            if (this.textBox2.InvokeRequired)
            {
                TextBoxDelegate d = ChangeTextBox2;
                textBox2.Invoke(d, time);
            }
            else
            {
                textBox2.Text = time;
            }
        }
        private delegate void PictureCursorDelegate();
        private void PictureCursorDefault()
        {
            if (this.pictureBox1.InvokeRequired)
            {
                PictureCursorDelegate d = PictureCursorDefault;
                textBox1.Invoke(d);
            }
            else
            {
                this.pictureBox1.Cursor = Cursors.Default;
            }
        }

        #endregion

        bool ThisPieceTypeIsWhite = true;//当前用户的棋子类型
        string Opponent = null;//对弈用户名字
        Chessboard board;//棋盘
        Graphics gImage = null;//画布
        Bitmap bmp = null;
        bool isWhite = true, winPiece = true;//isWhite切换当前下棋的棋子，winPiece胜利方棋子
        bool isMyReady = false, isOtherReady = false;//对弈双方是否准备
        bool isTrueLeave = true;//是否正常退出
        private void Form1_Load(object sender, EventArgs e)
        {
            Image img = Resources.t;
            SetGifBackground(img);

            //释放内存
            System.Timers.Timer clearTime = new System.Timers.Timer(5000);
            clearTime.AutoReset = true;
            clearTime.Elapsed += new System.Timers.ElapsedEventHandler(ClearTimer_tick);
            clearTime.Start();

            Win32.AnimateWindow(this.Handle, 2000, Win32.AW_BLEND);//淡入启动

        }

        private void ClearTimer_tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            Program.ClearMemory();
        }


        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.pictureBox1.Cursor == Cursors.No) return;
            ConnectDrop(e);
        }

        private void ConnectDrop(MouseEventArgs e)
        {
            try
            {
                if (Opponent != null && isMyReady == true && isOtherReady == true)
                {
                    Piece piece = board.GetPiece(e.X, e.Y);
                    if (piece == null) return;
                    if (piece.pieceType != PieceType.Empty)
                    {
                        return;
                    }
                    if (isWhite == ThisPieceTypeIsWhite)
                    {
                        //确定落子种类
                        piece.pieceType = ThisPieceTypeIsWhite ? PieceType.White : PieceType.Black;
                        //绘制棋子
                        piece.DrawPiece(this.gImage);
                        this.pictureBox1.Image = bmp;
                        //发生棋子类型
                        if (isWhite)
                        {
                            AsyncSendMessage("Drop," + Opponent + ",1" + "," + e.X + "," + e.Y);
                        }
                        else
                        {
                            AsyncSendMessage("Drop," + Opponent + ",0" + "," + e.X + "," + e.Y);
                        }

                        this.pictureBox1.Cursor = Cursors.No;
                    }

                    Music.play("drop");

                    //计时器切换
                    OtherTimer.Start();
                    MyTimer.Stop();

                    if (board.IsEndingGame(piece))
                    {
                        winPiece = ThisPieceTypeIsWhite;
                        Finish();
                    }
                    else if (board.isFillPiece())
                        Tie();
                    else
                        this.isWhite = !isWhite;

                }
            }
            catch (Exception ex)
            {
                AsyncSendMessage("Error," + ex);
            }
        }

        #region 联机模式自定义方法
        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (bmp != null) bmp.Dispose();
            if (gImage != null) gImage.Dispose();
            if (MyTimer != null || OtherTimer != null)
            {
                MyTimer.Dispose();
                OtherTimer.Dispose();
            }
            if (Opponent == null)
            {
                isWhite = true;
                ThisPieceTypeIsWhite = true;
            }

            isMyReady = false;
            isOtherReady = false;

            board = new Chessboard(18);
            bmp = new Bitmap(this.pictureBox1.Width, this.pictureBox1.Height);
            gImage = Graphics.FromImage(bmp);
            board.Draw(gImage);
            ChangePicture(bmp);//委托修改picturebox的image
            PictureCursorDefault();//委托修改鼠标样式

            TimerInit();//倒计时初始化
        }

        private void InitControl()
        {
            this.btn_fail.Enabled = true;
            this.btn_Fight.Enabled = true;
            this.btn_Login.Enabled = true;
            this.btn_Other.Enabled = false;
            this.btn_SendeMessage.Enabled = true;
            this.btn_SorH.Enabled = true;
            this.btn_start.Enabled = false;
            this.pictureBox4.Hide();
            this.txt_UserName.Enabled = true;

        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        private void Finish()
        {
            if (winPiece == ThisPieceTypeIsWhite)
            {
                Music.play("win");
                MessageBox.Show("你赢了");
            }
            else
            {
                Music.play("lose");
                MessageBox.Show("你输了");
            }
            isTrueLeave = true;
            //控件修改
            LableText1(this.txt_UserName.Text);
            LableText2(Opponent);
            ReadyNewGame();
            //
            StartAgain();
        }

        /// <summary>
        /// 平局
        /// </summary>
        private void Tie()
        {
            MessageBox.Show("平局");
            ReadyNewGame();
            StartAgain();
        }

        /// <summary>
        /// 初始化计时器
        /// </summary>
        private void ReadyNewGame()
        {
            MyTimer.Dispose();
            OtherTimer.Dispose();
            ChangeTextBox1("00:10:00");
            ChangeTextBox2("00:10:00");
        }

        /// <summary>
        /// 是否重开一局
        /// </summary>
        public void StartAgain()
        {

            Init();
            if (MessageBox.Show("是否继续", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                TrueStart();
                TrueOther();
                isTrueLeave = false;
            }
            else
            {
                AsyncSendMessage("Angin," + Opponent + ",Leave");
                this.Close();
            }
        }
        /// <summary>
        /// gif背景图片
        /// </summary>
        /// <param name="gif"></param>
        private void SetGifBackground(Image gif)
        {
            System.Drawing.Imaging.FrameDimension fd = new System.Drawing.Imaging.FrameDimension(gif.FrameDimensionsList[0]);
            int count = gif.GetFrameCount(fd); //获取帧数(gif图片可能包含多帧，其它格式图片一般仅一帧)
            System.Windows.Forms.Timer giftimer = new System.Windows.Forms.Timer();
            giftimer.Interval = 100;
            int i = 0;
            Image bgImg = null;
            giftimer.Tick += (s, e) =>
            {
                if (i >= count) { i = 0; }
                gif.SelectActiveFrame(fd, i);
                System.IO.Stream stream = new System.IO.MemoryStream();
                gif.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                if (bgImg != null) { bgImg.Dispose(); }
                bgImg = Image.FromStream(stream);
                this.BackgroundImage = bgImg;
                i++;
            };
            giftimer.Start();
        }
        #endregion
        /// <summary>
        /// 中间分割线
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int x = -1;
            int y = this.Height / 2 - 1;
        }

        #region 控件代码
        /// <summary>
        /// 发起对战
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Fight_Click(object sender, EventArgs e)
        {
            if (this.lst_OnlineUser.SelectedIndex != -1)
            {
                if (this.txt_UserName.Text.ToString() != this.lst_OnlineUser.SelectedItem.ToString())
                {
                    AsyncSendMessage("Fight," + this.lst_OnlineUser.SelectedItem);
                    this.btn_Fight.Enabled = false;
                }
                else MessageBox.Show("请不要选择自己作为对奕者");
            }
            else
                MessageBox.Show("请先在[当前在线]中选择一个对奕者");
        }
        /// <summary>
        /// 准备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_start_Click(object sender, EventArgs e)
        {
            if (Opponent != null)
            {
                AsyncSendMessage("Ready," + Opponent);
                isMyReady = true;
                this.label2.Text = this.txt_UserName.Text + "已准备";
                this.btn_start.Enabled = false;
                this.btn_Other.Enabled = false;
                this.Focus();
                if (ThisPieceTypeIsWhite) this.pictureBox1.Cursor = Cursors.No;
                else this.pictureBox1.Cursor = Cursors.Default;
            }
            else MessageBox.Show("请选择对弈者！！");
        }
        /// <summary>
        /// 换对手
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Other_Click(object sender, EventArgs e)
        {
            if (Opponent != null)
            {
                AsyncSendMessage("Angin," + Opponent + ",Leave");
                Opponent = null;
                this.lst_OnlineUser.Show();
                this.btn_Fight.Enabled = true;
                this.btn_start.Enabled = false;
                this.btn_Other.Enabled = false;
            }
        }
        /// <summary>
        /// 认输
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_fail_Click(object sender, EventArgs e)
        {
            if (Opponent != null)
            {
                AsyncSendMessage("Fail," + Opponent);
                winPiece = !ThisPieceTypeIsWhite;
                Finish();
            }
        }
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Login_Click(object sender, EventArgs e)
        {
            btn_Login.Enabled = false;
            AddStatus("开始连接.");
            lst_OnlineUser.Visible = true;
            this.btn_SorH.Text = "隐藏列表";
            connectWork.RunWorkerAsync();
        }
        /// <summary>
        /// 聊天
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SendeMessage_Click(object sender, EventArgs e)
        {
            if (lst_OnlineUser.SelectedIndex != -1)
            {
                AsyncSendMessage("Talk," + lst_OnlineUser.SelectedItem + "," + rtf_MessageInfo.Text + "\r\n");
                rtf_MessageInfo.AppendText(this.txt_UserName.Text + "：\r\n");
                rtf_MessageInfo.AppendText(rtf_MessageInfo.Text + "\r\n");
                rtf_MessageInfo.Clear();
                rtf_MessageInfo.ScrollToCaret();
            }
            else if (Opponent != null)
            {
                AsyncSendMessage("Talk," + Opponent + "," + rtf_MessageInfo.Text + "\r\n");
                rtf_MessageInfo.AppendText(this.txt_UserName.Text + "：\r\n");
                rtf_MessageInfo.AppendText(rtf_MessageInfo.Text + "\r\n");
                rtf_MessageInfo.Clear();
                rtf_MessageInfo.ScrollToCaret();
            }
            else
                MessageBox.Show("请先在[当前在线]中选择一个对话者");
        }

        private void btn_Colse_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否退出", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (Control cc in this.Controls)
                {
                    //SetDouble(cc);
                    if (cc == this.pictureBox5 || cc == this.btn_Contect || cc == this.btn_Single || cc == this.btn_close)
                        continue;
                    cc.Visible = false;
                }
                this.Close();
            }
        }
        /// <summary>
        /// 显示隐藏用户列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SorH_Click(object sender, EventArgs e)
        {
            if (lst_OnlineUser.Visible == false)
            {
                lst_OnlineUser.Visible = true;
                this.btn_SorH.Text = "隐藏列表";
            }
            else
            {
                lst_OnlineUser.Visible = false;
                this.btn_SorH.Text = "显示列表";
            }
        }

        //音乐播放暂停
        bool isPlay = true;
        private void button1_Click(object sender, EventArgs e)
        {
            if (isPlay)
            {
                Music.StopSong();
                button1.BackgroundImage = Resources.j;
            }
            else
            {
                Music.PlaySong(@"../../Resources/background1.mp3");
                button1.BackgroundImage = Resources.q;
            }
            isPlay = !isPlay;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null && isConnect == true)
            {
                if (!isTrueLeave && Opponent != null)
                    AsyncSendMessage("Angin," + Opponent + ",FalseLeave");
                AsyncSendMessage("Logout," + txt_UserName.Text);
                isExit = true;
                br.Close();
                bw.Close();
                client.Close();
            }
            Win32.AnimateWindow(this.Handle, 2000, Win32.AW_SLIDE | Win32.AW_HIDE | Win32.AW_BLEND);
            System.Environment.Exit(0);

        }

        #endregion

        #region 局时计时器
        System.Timers.Timer MyTimer, OtherTimer;//我方、对方计时器
        long MyTimeCount, OtherTimerCount;//我方、对方剩余时间
        //定义委托
        public delegate void SetControlValue(long value);
        private void TimerInit()
        {

            //设置时间间隔ms
            int interval = 1000;
            MyTimer = new System.Timers.Timer(interval);
            MyTimeCount = 600;//局时/秒
            //设置重复计时
            MyTimer.AutoReset = true;
            //设置执行System.Timers.Timer.Elapsed事件
            MyTimer.Elapsed += new System.Timers.ElapsedEventHandler(MyTimer_tick);
            OtherTimerCount = 600;
            OtherTimer = new System.Timers.Timer(interval);
            OtherTimer.AutoReset = true;
            OtherTimer.Elapsed += new System.Timers.ElapsedEventHandler(OtherTimer_tick);
        }

        private void MyTimer_tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Invoke(new SetControlValue(ShowMyTime), MyTimeCount);
            MyTimeCount--;
            //时间为零游戏结束
            if (MyTimeCount == 0)
            {
                winPiece = !ThisPieceTypeIsWhite;
                Finish();
                MyTimer.Stop();
            }
        }
        private void OtherTimer_tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Invoke(new SetControlValue(ShowOhterTime), OtherTimerCount);
            OtherTimerCount--;
            if (OtherTimerCount == 0)
            {
                winPiece = ThisPieceTypeIsWhite;
                Finish();
                OtherTimer.Stop();
            }
        }
        /// <summary>
        /// 修改文本框
        /// </summary>
        /// <param name="t"></param>
        private void ShowMyTime(long t)
        {
            TimeSpan temp = new TimeSpan(0, 0, (int)t);
            this.textBox1.Text = string.Format("{0:00}:{1:00}:{2:00}", temp.Hours, temp.Minutes, temp.Seconds);
            this.textBox1.Select(0, 0);
            this.pictureBox1.Focus();
        }
        private void ShowOhterTime(long t)
        {
            TimeSpan temp = new TimeSpan(0, 0, (int)t);
            this.textBox2.Text = string.Format("{0:00}:{1:00}:{2:00}", temp.Hours, temp.Minutes, temp.Seconds);
            this.textBox2.Select(0, 0);
            this.pictureBox1.Focus();
        }
        #endregion

        #region 控制游戏界面在屏幕中的移动
        bool isMove = false;//是否可以移动
        private Point FormLocation;     //form的location
        private Point mouseOffset;   //mousedown的location
        private void panel4_MouseDown(object sender, MouseEventArgs e)
        {
            isMove = true;
            FormLocation = this.Location;
            //用e.Location会出现屏幕晃动问题，e.Location是相对form的坐标，Control.MousePosition是对于屏幕的坐标
            mouseOffset = Control.MousePosition;
        }
        private void panel4_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove)
            {
                Point pt = Control.MousePosition;
                int x = mouseOffset.X - pt.X;//鼠标移动量
                int y = mouseOffset.Y - pt.Y;
                this.Location = new Point(FormLocation.X - x, FormLocation.Y - y);
            }
        }
        private void panel4_MouseUp(object sender, MouseEventArgs e)
        {
            isMove = false;
        }
        #endregion

        #region 单机模式
        private void SingleInit()//单机模式初始化
        {
            if (bmp != null) bmp.Dispose();
            if (gImage != null) gImage.Dispose();

            isWhite = false;

            board = new Chessboard(18);
            bmp = new Bitmap(this.pictureBox4.Width, this.pictureBox4.Height);
            gImage = Graphics.FromImage(bmp);
            board.Draw(gImage);
            this.pictureBox4.Image = bmp;
        }
        private void InitSingleControl()//初始化控件
        {
            this.btn_fail.Enabled = false;
            this.btn_Fight.Enabled = false;
            this.btn_Login.Enabled = false;
            this.btn_Other.Enabled = false;
            this.btn_SendeMessage.Enabled = false;
            this.btn_SorH.Enabled = false;
            this.btn_start.Enabled = false;
            this.pictureBox4.Show();
            this.txt_UserName.Enabled = false;
            this.lst_OnlineUser.Visible = false;
            this.btn_SorH.Text = "显示列表";
        }
        private void pictureBox4_MouseClick(object sender, MouseEventArgs e)
        {
            if (isSingle == true)
            {
                Piece piece = board.GetPiece(e.X, e.Y);
                if (piece == null) return;
                if (piece.pieceType != PieceType.Empty)
                {
                    return;
                }

                //确定落子种类
                piece.pieceType = isWhite ? PieceType.White : PieceType.Black;
                //绘制棋子
                piece.DrawPiece(this.gImage);
                this.pictureBox4.Image = bmp;

                Music.play("drop");

                if (board.IsEndingGame(piece))
                {
                    SingleFinish();
                }
                else if (board.isFillPiece())
                    SingleTie();
                else
                    this.isWhite = !isWhite;
            }
        }
        private void SingleFinish()
        {
            if (isWhite)
            {
                MessageBox.Show("白棋赢了");
            }
            else
            {
                MessageBox.Show("黑棋赢了");
            }
            StartSingleAgain();
        }

        /// <summary>
        /// 平局
        /// </summary>
        private void SingleTie()
        {
            MessageBox.Show("平局");
            StartSingleAgain();
        }
        /// <summary>
        /// 是否重开一局
        /// </summary>
        public void StartSingleAgain()
        {

            if (MessageBox.Show("是否继续", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SingleInit();
            }
            else
            {
                InitMode();
            }
        }
        #endregion

        #region 模式选择
        bool isFirst = true;
        private void InitFirst()//是否第一次启动
        {
            if (isFirst)
            {
                isFirst = false;
                foreach (Control cc in this.Controls)
                {
                    if (cc == this.lst_OnlineUser) continue;
                    if (cc.Visible == false) cc.Visible = true;
                }
            }
        }

        private void InitMode()//调取显示和隐藏模式选择
        {
            if (this.pictureBox5.Visible == true)
            {
                this.btn_close.Visible = false;
                this.btn_Contect.Visible = false;
                this.btn_Single.Visible = false;
                this.pictureBox5.Visible = false;
            }
            else
            {
                this.btn_close.Visible = true;
                this.btn_Contect.Visible = true;
                this.btn_Single.Visible = true;
                this.pictureBox5.Visible = true;
            }
        }
        private void btn_Single_Click(object sender, EventArgs e)
        {
            isSingle = true;
            InitFirst();
            SingleInit();
            InitSingleControl();
            InitMode();
        }
        private void btn_Contect_Click(object sender, EventArgs e)
        {
            isSingle = false;
            InitFirst();
            Init();
            InitControl();
            InitMode();
        }

        private void btn_Mode_Click(object sender, EventArgs e)
        {
            InitMode();
            if (Opponent != null)
            {
                AsyncSendMessage("Angin," + Opponent + ",Leave");
                Opponent = null;
            }
        }
        #endregion


    }
}
