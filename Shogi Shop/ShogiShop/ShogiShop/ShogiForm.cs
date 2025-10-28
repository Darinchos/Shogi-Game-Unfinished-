using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ShogiShop
{
    public partial class ShogiForm : Form
    {
        private const int BOARD_SIZE = 9;
        private const int CELL_SIZE = 56;
        private const int MARGIN = 30;

        private Piece[,] board = new Piece[BOARD_SIZE, BOARD_SIZE];
        private List<Piece> blackHand = new();
        private List<Piece> whiteHand = new();

        private Piece? selectedPiece = null;
        private (int x, int y) selectedPos = (-1, -1);
        private bool isBlackTurn = true;
        private bool isDropping = false;

        private Bitmap boardBitmap = null!;
        private Graphics gfx = null!;

        public ShogiForm()
        {
            InitializeComponent();
            ClientSize = new Size(BOARD_SIZE * CELL_SIZE + 2 * MARGIN + 200, BOARD_SIZE * CELL_SIZE + 2 * MARGIN);
            DoubleBuffered = true;
            boardBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            gfx = Graphics.FromImage(boardBitmap);
            SetupBoard();
        }

        private void SetupBoard()
        {
            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                    board[x, y] = null;

            // BLACK (bottom)
            board[0, 0] = new(PieceType.Lance, false);
            board[1, 0] = new(PieceType.Knight, false);
            board[2, 0] = new(PieceType.Silver, false);
            board[3, 0] = new(PieceType.Gold, false);
            board[4, 0] = new(PieceType.King, false);
            board[5, 0] = new(PieceType.Gold, false);
            board[6, 0] = new(PieceType.Silver, false);
            board[7, 0] = new(PieceType.Knight, false);
            board[8, 0] = new(PieceType.Lance, false);
            board[1, 1] = new(PieceType.Rook, false);
            board[7, 1] = new(PieceType.Bishop, false);
            for (int x = 0; x < BOARD_SIZE; x++) board[x, 2] = new(PieceType.Pawn, false);

            // WHITE (top)
            board[0, 8] = new(PieceType.Lance, true);
            board[1, 8] = new(PieceType.Knight, true);
            board[2, 8] = new(PieceType.Silver, true);
            board[3, 8] = new(PieceType.Gold, true);
            board[4, 8] = new(PieceType.King, true);
            board[5, 8] = new(PieceType.Gold, true);
            board[6, 8] = new(PieceType.Silver, true);
            board[7, 8] = new(PieceType.Knight, true);
            board[8, 8] = new(PieceType.Lance, true);
            board[1, 7] = new(PieceType.Bishop, true);
            board[7, 7] = new(PieceType.Rook, true);
            for (int x = 0; x < BOARD_SIZE; x++) board[x, 6] = new(PieceType.Pawn, true);
        }

        private void ShogiForm_Paint(object? sender, PaintEventArgs e)
        {
            gfx.Clear(Color.BurlyWood);
            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    int px = MARGIN + x * CELL_SIZE;
                    int py = MARGIN + y * CELL_SIZE;
                    gfx.DrawRectangle(Pens.Black, px, py, CELL_SIZE, CELL_SIZE);
                }

            if (selectedPos.x >= 0)
            {
                int px = MARGIN + selectedPos.x * CELL_SIZE;
                int py = MARGIN + selectedPos.y * CELL_SIZE;
                gfx.FillRectangle(new SolidBrush(Color.FromArgb(80, 0, 255, 0)), px, py, CELL_SIZE, CELL_SIZE);
            }

            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                    board[x, y]?.Draw(gfx, MARGIN + x * CELL_SIZE, MARGIN + y * CELL_SIZE, CELL_SIZE);

            DrawHand(gfx, blackHand, false, "Black Hand");
            DrawHand(gfx, whiteHand, true, "White Hand");
            e.Graphics.DrawImage(boardBitmap, 0, 0);
        }

        private void DrawHand(Graphics g, List<Piece> hand, bool isWhite, string label)
        {
            int startY = isWhite ? MARGIN + 9 * CELL_SIZE + 20 : MARGIN - 80;
            int x = MARGIN + BOARD_SIZE * CELL_SIZE + 20;
            int y = startY;
            g.DrawString(label, new Font("Arial", 12, FontStyle.Bold), Brushes.Black, x, y - 20);
            y += 10;
            foreach (var p in hand)
            {
                p.Draw(g, x, y, CELL_SIZE / 2);
                y += CELL_SIZE / 2 + 5;
            }
        }

        private void ShogiForm_MouseClick(object? sender, MouseEventArgs e)
        {
            int boardLeft = MARGIN;
            int boardTop = MARGIN;
            int boardRight = boardLeft + BOARD_SIZE * CELL_SIZE;
            int boardBottom = boardTop + BOARD_SIZE * CELL_SIZE;

            if (e.X >= boardLeft && e.X < boardRight && e.Y >= boardTop && e.Y < boardBottom)
            {
                int tx = (e.X - boardLeft) / CELL_SIZE;
                int ty = (e.Y - boardTop) / CELL_SIZE;
                HandleBoardClick(tx, ty);
            }
            else if (e.X >= boardRight + 20)
            {
                HandleHandClick(e.X, e.Y);
            }
            Invalidate();
        }

        private void HandleBoardClick(int tx, int ty)
        {
            Piece? target = board[tx, ty];

            // SELECT PIECE
            if (selectedPiece == null && !isDropping)
            {
                if (target != null && target.IsBlack == isBlackTurn)
                {
                    selectedPiece = target;
                    selectedPos = (tx, ty);
                    Text = $"Selected {target.Type}";
                    return;
                }
                Text = "Click your piece";
                return;
            }

            // DROP FROM HAND
            if (isDropping && target == null && IsValidDrop(tx, ty))
            {
                board[tx, ty] = selectedPiece!;
                selectedPiece = null;
                selectedPos = (-1, -1);
                isDropping = false;
                isBlackTurn = !isBlackTurn;
                CheckGameState();
                return;
            }

            // MOVE PIECE
            if (selectedPiece != null && !isDropping && selectedPiece.CanMove(selectedPos.x, selectedPos.y, tx, ty, board))
            {
                // BLOCK KING CAPTURE
                if (target?.Type == PieceType.King)
                {
                    MessageBox.Show($"CHECKMATE! {(isBlackTurn ? "Black" : "White")} wins!");
                    Application.Exit();
                    return;
                }

                bool promote = false;
                if (selectedPiece.CanPromote(selectedPos.y, ty, isBlackTurn))
                    promote = MessageBox.Show("Promote?", "Promotion", MessageBoxButtons.YesNo) == DialogResult.Yes;

                board[tx, ty] = selectedPiece;
                board[selectedPos.x, selectedPos.y] = null;
                if (promote) selectedPiece.Promote();

                // CAPTURE → HAND
                if (target != null)
                {
                    target.IsBlack = isBlackTurn;
                    target.Demote();
                    (isBlackTurn ? whiteHand : blackHand).Add(target);
                }

                selectedPiece = null;
                selectedPos = (-1, -1);
                isBlackTurn = !isBlackTurn;
                CheckGameState();
                return;
            }

            selectedPiece = null;
            selectedPos = (-1, -1);
            isDropping = false;
            Text = "Invalid";
        }

        private void HandleHandClick(int mx, int my)
        {
            if (isDropping) return;
            var hand = isBlackTurn ? blackHand : whiteHand;
            int baseX = MARGIN + BOARD_SIZE * CELL_SIZE + 20;
            int baseY = isBlackTurn ? MARGIN - 80 : MARGIN + 9 * CELL_SIZE + 20;

            for (int i = 0; i < hand.Count; i++)
            {
                int py = baseY + 20 + i * (CELL_SIZE / 2 + 5);
                if (mx >= baseX && mx < baseX + CELL_SIZE / 2 && my >= py && my < py + CELL_SIZE / 2)
                {
                    selectedPiece = hand[i];
                    hand.RemoveAt(i);
                    isDropping = true;
                    Text = $"Drop {selectedPiece.Type}";
                    return;
                }
            }
        }

        private bool IsValidDrop(int tx, int ty)
        {
            if (selectedPiece == null || board[tx, ty] != null) return false;
            if (selectedPiece.Type is PieceType.Pawn or PieceType.PawnPromoted)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                    if (board[tx, y]?.IsBlack == isBlackTurn && board[tx, y].Type is PieceType.Pawn or PieceType.PawnPromoted)
                        return false;
                if ((isBlackTurn && ty <= 0) || (!isBlackTurn && ty >= 8)) return false;
            }
            return true;
        }

        private void CheckGameState()
        {
            var (kx, ky) = FindKing(!isBlackTurn);
            if (kx == -1) return;

            bool inCheck = IsKingInCheck(kx, ky, isBlackTurn);
            if (inCheck)
            {
                Text = "CHECK!";
                if (IsCheckmate(kx, ky))
                {
                    MessageBox.Show($"CHECKMATE! {(isBlackTurn ? "Black" : "White")} wins!");
                    Application.Exit();
                }
            }
            else
            {
                Text = $"{(isBlackTurn ? "Black" : "White")} to play";
            }
        }

        private bool IsKingInCheck(int kx, int ky, bool attackerIsBlack)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    var p = board[x, y];
                    if (p != null && p.IsBlack == attackerIsBlack && p.CanMove(x, y, kx, ky, board))
                        return true;
                }
            return false;
        }

        private (int x, int y) FindKing(bool whiteKing)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                    if (board[x, y]?.Type == PieceType.King && board[x, y].IsBlack != whiteKing)
                        return (x, y);
            return (-1, -1);
        }

        private bool IsCheckmate(int kx, int ky)
        {
            bool defenderIsBlack = !isBlackTurn;

            // Try moving king
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = kx + dx, ny = ky + dy;
                    if (nx < 0 || nx >= 9 || ny < 0 || ny >= 9) continue;
                    if (board[nx, ny]?.IsBlack == defenderIsBlack) continue;

                    var king = board[kx, ky];
                    var dest = board[nx, ny];
                    board[kx, ky] = null;
                    board[nx, ny] = king;

                    bool stillInCheck = IsKingInCheck(nx, ny, isBlackTurn);

                    board[kx, ky] = king;
                    board[nx, ny] = dest;

                    if (!stillInCheck) return false;
                }

            // Try blocking or capturing attacker
            for (int x = 0; x < BOARD_SIZE; x++)
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    var p = board[x, y];
                    if (p != null && p.IsBlack == defenderIsBlack)
                    {
                        for (int tx = 0; tx < BOARD_SIZE; tx++)
                            for (int ty = 0; ty < BOARD_SIZE; ty++)
                            {
                                if (p.CanMove(x, y, tx, ty, board))
                                {
                                    var target = board[tx, ty];
                                    board[tx, ty] = p;
                                    board[x, y] = null;

                                    bool stillInCheck = IsKingInCheck(kx, ky, isBlackTurn);

                                    board[x, y] = p;
                                    board[tx, ty] = target;

                                    if (!stillInCheck) return false;
                                }
                            }
                    }
                }

            return true;
        }
    }

    internal enum PieceType
    {
        King, Gold, Silver, Knight, Lance, Bishop, Rook, Pawn,
        PromotedSilver, PromotedKnight, PromotedLance, PromotedBishop, PromotedRook, PawnPromoted
    }

    internal sealed class Piece
    {
        public PieceType Type { get; private set; }
        public bool IsBlack { get; set; }

        private static readonly Dictionary<PieceType, string> Kanji = new()
        {
            {PieceType.King, "王"}, {PieceType.Gold, "金"}, {PieceType.Silver, "銀"},
            {PieceType.Knight, "桂"}, {PieceType.Lance, "香"}, {PieceType.Bishop, "角"},
            {PieceType.Rook, "飛"}, {PieceType.Pawn, "歩"},
            {PieceType.PromotedSilver, "全"}, {PieceType.PromotedKnight, "圭"},
            {PieceType.PromotedLance, "仝"}, {PieceType.PromotedBishop, "馬"},
            {PieceType.PromotedRook, "龍"}, {PieceType.PawnPromoted, "と"}
        };

        public Piece(PieceType type, bool isBlack) { Type = type; IsBlack = isBlack; }

        public void Promote() => Type = Type switch
        {
            PieceType.Silver => PieceType.PromotedSilver,
            PieceType.Knight => PieceType.PromotedKnight,
            PieceType.Lance  => PieceType.PromotedLance,
            PieceType.Bishop => PieceType.PromotedBishop,
            PieceType.Rook   => PieceType.PromotedRook,
            PieceType.Pawn   => PieceType.PawnPromoted,
            _ => Type
        };

        public void Demote() => Type = Type switch
        {
            PieceType.PromotedSilver => PieceType.Silver,
            PieceType.PromotedKnight => PieceType.Knight,
            PieceType.PromotedLance  => PieceType.Lance,
            PieceType.PromotedBishop => PieceType.Bishop,
            PieceType.PromotedRook   => PieceType.Rook,
            PieceType.PawnPromoted   => PieceType.Pawn,
            _ => Type
        };

        public bool IsPromoted => Type >= PieceType.PromotedSilver;

        public bool CanPromote(int fromY, int toY, bool isBlack)
        {
            if (IsPromoted) return false;
            return isBlack ? (fromY <= 2 || toY <= 2) : (fromY >= 6 || toY >= 6);
        }

        public bool CanMove(int fx, int fy, int tx, int ty, Piece[,] b)
        {
            if (tx < 0 || tx > 8 || ty < 0 || ty > 8) return false;
            var dest = b[tx, ty];
            if (dest != null && dest.IsBlack == IsBlack) return false;

            int dx = tx - fx;
            int dy = ty - fy;
            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);
            bool forward = IsBlack ? dy < 0 : dy > 0;

            return Type switch
            {
                PieceType.King => adx <= 1 && ady <= 1,

                PieceType.Gold or PieceType.PromotedSilver or PieceType.PromotedKnight
                    or PieceType.PromotedLance or PieceType.PawnPromoted =>
                    (adx <= 1 && ady <= 1) || (forward && adx == 0 && ady == 1),

                PieceType.Silver =>
                    (adx <= 1 && ady <= 1 && (ady == 1 || adx == 1)) && (forward || adx == 1),

                PieceType.Knight =>
                    forward && ((adx == 1 && ady == 2) || (adx == 2 && ady == 1)),

                PieceType.Lance =>
                    dx == 0 && forward && ClearPath(b, fx, fy, tx, ty),

                PieceType.Pawn =>
                    dx == 0 && (IsBlack ? dy == -1 : dy == +1),

                PieceType.PromotedBishop =>
                    (adx == ady && ClearPath(b, fx, fy, tx, ty)) ||
                    (dx == 0 && ady == 1) || (dy == 0 && adx == 1),

                PieceType.PromotedRook =>
                    ((dx == 0 || dy == 0) && ClearPath(b, fx, fy, tx, ty)) ||
                    (adx == 1 && ady == 1),

                PieceType.Bishop => adx == ady && ClearPath(b, fx, fy, tx, ty),
                PieceType.Rook => (dx == 0 || dy == 0) && ClearPath(b, fx, fy, tx, ty),

                _ => false
            };
        }

        public static bool ClearPath(Piece[,] b, int fx, int fy, int tx, int ty)
        {
            int sx = Math.Sign(tx - fx);
            int sy = Math.Sign(ty - fy);
            int x = fx + sx;
            int y = fy + sy;
            while (x != tx || y != ty)
            {
                if (b[x, y] != null) return false;
                x += sx; y += sy;
            }
            return true;
        }

        public void Draw(Graphics g, int px, int py, int size)
        {
            string txt = Kanji[Type];
            var f = new Font("MS Gothic", size * 0.45f, FontStyle.Bold);
            var brush = IsBlack ? Brushes.Black : Brushes.White;
            var bg = IsBlack ? Brushes.SandyBrown : Brushes.DarkRed;
            g.FillEllipse(bg, px, py, size, size);
            var ts = g.MeasureString(txt, f);
            g.DrawString(txt, f, brush, px + (size - ts.Width) / 2, py + (size - ts.Height) / 2);
        }
    }
}