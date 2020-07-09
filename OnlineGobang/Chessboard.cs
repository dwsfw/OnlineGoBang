using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OnlineGobang
{
    public class Chessboard
    {        

        Pen line = new Pen(Color.Black);//网格线画笔
        Brush BlackBrush = new SolidBrush(Color.Black);//画点
        private int length;//网格宽度
        private Piece[,] pieces;//落子点
        public Chessboard(int length)
        {
            pieces = new Piece[length, length];
            this.length = length;
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    pieces[i, j] = new Piece(i+1, j+1);
                }
            }
        }
        /// <summary>
        /// 画棋格
        /// </summary>
        /// <param name="g"></param>
        public void Draw(Graphics g)
        {
            int b = HelpConst.GridSpacing / 2;
            for (int i = 0; i < length; i++)
            {
                g.DrawLine(line, new Point(b, i * HelpConst.GridSpacing + b), new Point((length - 1) * HelpConst.GridSpacing + b, i * HelpConst.GridSpacing + b));
                g.DrawLine(line, new Point(i * HelpConst.GridSpacing + b, b), new Point(i * HelpConst.GridSpacing + b, (length - 1) * HelpConst.GridSpacing + b));
            }
            
            #region 画棋子中点
            g.FillEllipse(BlackBrush, (3) * HelpConst.GridSpacing - 3 + b, (3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (3) * HelpConst.GridSpacing - 3 + b, (this.length / 2) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (3) * HelpConst.GridSpacing - 3 + b, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length / 2) * HelpConst.GridSpacing - 3 + b, (3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length / 2) * HelpConst.GridSpacing - 3 + b, (this.length / 2) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length / 2) * HelpConst.GridSpacing - 3 + b, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, (this.length / 2) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, (3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            g.FillEllipse(BlackBrush, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, (this.length - 1 - 3) * HelpConst.GridSpacing - 3 + b, 6, 6);
            #endregion
        }
        /// <summary>
        /// 鼠标落子点
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Piece GetPiece(int x, int y)
        {
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (pieces[i, j].CanDrop(x, y))
                        return pieces[i, j];
                }
            }
            return null;
        }
        /// <summary>
        /// 棋子是否摆满棋盘
        /// </summary>
        /// <returns></returns>
        public bool isFillPiece()
        {
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    if (pieces[i, j].pieceType == PieceType.Empty)
                        return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断游戏是否结束
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public bool IsEndingGame(Piece piece)
        {
            int x, y;
            x = piece.X-1; y = piece.Y-1;
            int cnt = 0;
            PieceType type=piece.pieceType;
            //横向验证
            for (int i = x - 5; i < x + 5; i++)
            {
                if (cnt == 5) break;
                if (i < 0 || i >= length || pieces[i, y].pieceType == PieceType.Empty 
                    || pieces[i, y].pieceType !=type)
                {
                    cnt = 0;
                    continue;
                }
                else if (pieces[i, y].pieceType == type) cnt++;
            }
            //纵向验证
            if (cnt != 5) cnt=0;
            for (int i = y - 5; i < y + 5; i++)
            {
                if (cnt == 5) break;
                if (i < 0 || i >= length || pieces[x, i].pieceType == PieceType.Empty 
                    || pieces[x, i].pieceType != type)
                {
                    cnt = 0;
                    continue;
                }
                else if (pieces[x,i].pieceType == type) cnt++;
            }
            //斜向验证
            int j = y - 5;
            if (cnt != 5) cnt = 0;
            for (int i = x - 5; i < x + 5; i++)
            {
                if (cnt == 5) break;
                if (i < 0 || i >= length || j < 0 || j >= length ||
                    pieces[i, j].pieceType == PieceType.Empty || pieces[i, j].pieceType != type)
                {
                    cnt = 0;
                    j++;
                    continue;
                }
                else if (pieces[i, j].pieceType == type) cnt++;
                j++;
            }
            j = y + 5;
            if (cnt != 5) cnt = 0;
            for (int i = x - 5; i < x + 5; i++)
            {
                if (cnt == 5) break;
                if (i < 0 || i >= length || j < 0 || j >= length ||
                    pieces[i, j].pieceType == PieceType.Empty || pieces[i, j].pieceType != type)
                {
                    cnt = 0;
                    j--;
                    continue;
                }
                else if (pieces[i, j].pieceType == type) cnt++;
                j--;
            }
            if (cnt == 5) return true;
            else return false;
        }
    }
}
