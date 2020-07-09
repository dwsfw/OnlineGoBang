using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OnlineGobang
{
    public class Piece
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Piece(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public PieceType pieceType { get; set; }
        Brush whitePiece = new SolidBrush(Color.White);//白棋画刷
        Brush blackPiece = new SolidBrush(Color.Black);//黑棋画刷
        /// <summary>
        /// 获取当前坐标点
        /// </summary>
        /// <returns></returns>
        public Point GetPieceXY()
        {
            int x = (this.X - 1) * HelpConst.GridSpacing + HelpConst.GridSpacing / 2;
            return new Point(x, (this.Y - 1) * HelpConst.GridSpacing + HelpConst.GridSpacing / 2);
        }
        /// <summary>
        /// 画出棋子
        /// </summary>
        /// <param name="g"></param>
        public void DrawPiece(Graphics g)
        {
            if(pieceType!=PieceType.Empty)
            {
                Brush brush;
                if (pieceType == PieceType.White) brush = whitePiece;
                else brush = blackPiece;
                int x = GetPieceXY().X - HelpConst.PieceRadius;
                int y = GetPieceXY().Y - HelpConst.PieceRadius;
                g.FillEllipse(brush, x, y, HelpConst.PieceRadius * 2, HelpConst.PieceRadius * 2);
            }
        }
        /// <summary>
        /// 落子是否有效
        /// </summary>
        /// <returns></returns>
        public bool CanDrop(int x,int y)
        {
            if(GetPieceXY().X-HelpConst.DropRadius<=x&&GetPieceXY().X+HelpConst.DropRadius>=x)
                if(GetPieceXY().Y-HelpConst.DropRadius<=y&&GetPieceXY().Y+HelpConst.DropRadius>=y)
                    return true;
            return false;
        }
    }
}
