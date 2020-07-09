using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace OnlineGobang
{
    class Music
    {
         #region 音乐播放器
        [DllImport("winmm.dll")]
        private static extern int mciSendString
                (
                        string lpstrCommand,
                        string lpstrReturnString,
                        int uReturnLength,
                        int hwndCallback
                );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName
                (
                          [MarshalAs(UnmanagedType.LPTStr)]         string path,
                          [MarshalAs(UnmanagedType.LPTStr)]         StringBuilder shortPath,
                          int shortPathLength
                );

        /// <summary>
        /// 背景音乐播放
        /// </summary>
        /// <param name="FileName"></param>
        public static void PlaySong(string FileName)
        {
            StringBuilder shortPathTemp = new StringBuilder(255);
            int result = GetShortPathName(FileName, shortPathTemp, shortPathTemp.Capacity);
            string ShortPath = shortPathTemp.ToString();

            mciSendString("close song", null, 0, 0);
            mciSendString("open   " + ShortPath + "   alias   song", "", 0, 0);
            mciSendString("play   song", "", 0, 0);
        }
        /// <summary>
        /// 背景音乐播放
        /// </summary>
        /// <param name="FileName"></param>
        public static void StopSong()
        {
            mciSendString("close song", null, 0, 0);
        }
         /// <summary>   
        /// 设置声音大小(1-1000)
        /// </summary>  
        /// <returns></returns>   
        private static void SetVolume(int Volume)
        {
            string MciCommand = string.Format("setaudio song volume to {0}", Volume);
            mciSendString(MciCommand, null, 0, 0);
        }
        #endregion

        private static SoundPlayer soundPlayer = new SoundPlayer();

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="path"></param>
        public static void play(string command) {
            switch (command)
            {
                case "drop":
                    soundPlayer.Stream = global::OnlineGobang.Properties.Resources.drop;
                    soundPlayer.Play();
                    break;
                case "win":
                    soundPlayer.Stream = global::OnlineGobang.Properties.Resources.win;
                    soundPlayer.Play();
                    break;
                case "lose":
                    soundPlayer.Stream = global::OnlineGobang.Properties.Resources.lose;
                    soundPlayer.Play();
                    break;
            }
            soundPlayer.Dispose();
        }
    }
}
