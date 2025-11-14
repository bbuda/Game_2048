using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;

namespace Game2048;

public partial class MainForm : Form
{
    private const int GridSize = 4;
    private const int TileMargin = 10;
    private const int TileSize = 100;

    private const int PanelSize = GridSize * (TileSize + TileMargin) + TileMargin;

    private int[,] _grid = new int[GridSize, GridSize];
    private int _score = 0;
    private bool _gameOver = false;
    private bool _gameWon = false;
    private Random _rand = new Random();

    private Panel gamePanel;
    private Label scoreLabel;
    private Button newGameButton;

    private readonly Color _gridColor = Color.FromArgb(187, 173, 160);
    private readonly Color _emptyTileColor = Color.FromArgb(205, 192, 180);

    private readonly Dictionary<int, Color> TileColors =
        new Dictionary<int, Color>() {
        { 0, Color.FromArgb(205, 192, 180) },
        { 2, Color.FromArgb(238, 228, 218) },
        { 4, Color.FromArgb(237, 224, 200) },
        { 8, Color.FromArgb(242, 177, 121) },
        { 16, Color.FromArgb(245, 149, 99) },
        { 32, Color.FromArgb(246, 124, 95) },
        { 64, Color.FromArgb(246, 94, 59) },
        { 128, Color.FromArgb(237, 207, 114) },
        { 256, Color.FromArgb(237, 204, 97) },
        { 512, Color.FromArgb(237, 200, 80) },
        { 1024, Color.FromArgb(237, 197, 63) },
        { 2048, Color.FromArgb(237, 194, 46) },

        };

    private Color GetTileTextColor(int value)
    {
        if (value <= 4)
            return Color.FromArgb(119, 110, 101);
        else
            return Color.White;
    }

    public MainForm()
    {
        InitializeCustomComponents();
        SetDoubleBuffered(gamePanel);
        NewGame();
    }

    private void InitializeCustomComponents()
    {
        this.SuspendLayout();
        this.Text = "Игра 2048 (WinForms)";
        this.ClientSize = new Size(PanelSize + 50, PanelSize + 150);
        this.MinimumSize = this.ClientSize;
        this.MaximumSize = this.ClientSize;
        this.BackColor = Color.FromArgb(250, 248, 239);
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;

        scoreLabel = new Label
        {
            Text = "Счет: 0",
            Font = new Font("Arial", 16, FontStyle.Bold),
            Location = new Point(TileMargin, TileMargin),
            AutoSize = true,
            ForeColor = Color.FromArgb(119, 110, 101)
        };
        this.Controls.Add(scoreLabel);

        newGameButton =
            new Button
            {
                Text = "Новая игра",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(PanelSize - 130, TileMargin),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(245, 124, 95),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
        newGameButton.FlatAppearance.BorderSize = 0;
        newGameButton.Click += (s, e) => NewGame();
        this.Controls.Add(newGameButton);

        gamePanel =
            new Panel
            {
                Location = new Point(TileMargin, 80),
                Size = new Size(PanelSize, PanelSize),
                BackColor = _gridColor,
                BorderStyle = BorderStyle.None
            };
        gamePanel.Paint += GamePanel_Paint;
        this.Controls.Add(gamePanel);

        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private void SetDoubleBuffered(Panel panel)
    {
        if (panel == null)
            return;

        PropertyInfo? pi = typeof(Panel).GetProperty(
            "DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);

        if (pi != null)
        {
            pi.SetValue(panel, true, null);
        }
    }

    private void NewGame()
    {
        _grid = new int[GridSize, GridSize];
        _score = 0;
        _gameOver = false;
        _gameWon = false;
        scoreLabel.Text = "Счет: 0";
        GenerateNewTile();
        GenerateNewTile();
        gamePanel.Invalidate();
    }

    private void GenerateNewTile()
    {
        if (IsGameOver())
            return;

        var emptyCells = new List<Point>();

        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                if (_grid[i, j] == 0)
                {
                    emptyCells.Add(new Point(i, j));
                }
            }
        }

        if (emptyCells.Count > 0)
        {
            Point p = emptyCells[_rand.Next(emptyCells.Count)];

            _grid[p.X, p.Y] = (_rand.Next(10) == 0) ? 4 : 2;
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_gameOver || _gameWon)
            return;

        bool moved = false;

        switch (e.KeyCode)
        {
            case Keys.W:
                moved = Move(0);
                break;
            case Keys.S:
                moved = Move(1);
                break;
            case Keys.A:
                moved = Move(2);
                break;
            case Keys.D:
                moved = Move(3);
                break;
            default:
                return;
        }

        if (moved)
        {
            GenerateNewTile();
            if (scoreLabel != null)
                scoreLabel.Text = $"Счет: {_score}";
            if (gamePanel != null)
                gamePanel.Invalidate();

            if (CheckWin())
            {
                _gameWon = true;
                MessageBox.Show("Поздравляем! Вы достигли 2048!", "Победа!",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (IsGameOver())
            {
                _gameOver = true;
                MessageBox.Show($"Игра окончена! Ваш счет: {_score}", "Поражение",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
    private bool Move(int direction)
    {
        bool moved = false;
        int[,] oldGrid = (int[,])_grid.Clone();

        for (int i = 0; i < GridSize; i++)
        {
            int[] line = GetLine(i, direction);

            line = Compress(line);

            bool merged = false;
            (line, merged) = Merge(line);
            if (merged)
                moved = true;

            line = Compress(line);

            SetLine(i, direction, line);
        }

        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                if (oldGrid[i, j] != _grid[i, j])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int[] Compress(int[] line)
    {
        var nonZero = line.Where(x => x != 0).ToArray();
        var newLine = new int[GridSize];
        Array.Copy(nonZero, newLine, nonZero.Length);
        return newLine;
    }

    private (int[], bool) Merge(int[] line)
    {
        bool merged = false;
        for (int i = 0; i < GridSize - 1; i++)
        {
            if (line[i] != 0 && line[i] == line[i + 1])
            {
                line[i] *= 2;
                _score += line[i];
                line[i + 1] = 0;
                merged = true;
            }
        }
        return (line, merged);
    }

    private int[] GetLine(int index, int direction)
    {
        int[] line = new int[GridSize];
        for (int j = 0; j < GridSize; j++)
        {
            switch (direction)
            {
                case 0:
                    line[j] = _grid[j, index];
                    break;
                case 1:
                    line[j] = _grid[GridSize - 1 - j, index];
                    break;
                case 2:
                    line[j] = _grid[index, j];
                    break;
                case 3:
                    line[j] = _grid[index, GridSize - 1 - j];
                    break;
            }
        }
        return line;
    }

    private void SetLine(int index, int direction, int[] line)
    {
        for (int j = 0; j < GridSize; j++)
        {
            switch (direction)
            {
                case 0:
                    _grid[j, index] = line[j];
                    break;
                case 1:
                    _grid[GridSize - 1 - j, index] = line[j];
                    break;
                case 2:
                    _grid[index, j] = line[j];
                    break;
                case 3:
                    _grid[index, GridSize - 1 - j] = line[j];
                    break;
            }
        }
    }

    private bool CheckWin()
    {
        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                if (_grid[i, j] >= 2048)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsGameOver()
    {
        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                if (_grid[i, j] == 0)
                    return false;
            }
        }

        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                int val = _grid[i, j];

                if (j < GridSize - 1 && _grid[i, j + 1] == val)
                    return false;

                if (i < GridSize - 1 && _grid[i + 1, j] == val)
                    return false;
            }
        }

        return true;
    }

    private void GamePanel_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint =
            System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        g.Clear(_gridColor);

        for (int i = 0; i < GridSize; i++)
        {
            for (int j = 0; j < GridSize; j++)
            {
                DrawTile(g, _grid[i, j], i, j);
            }
        }
    }

    private void DrawTile(Graphics g, int value, int row, int col)
    {
        int x = col * (TileSize + TileMargin) + TileMargin;
        int y = row * (TileSize + TileMargin) + TileMargin;

        Color tileColor =
            TileColors.ContainsKey(value) ? TileColors[value] : TileColors[2048];

        using (SolidBrush brush = new SolidBrush(tileColor))
        {
            int cornerRadius = 8;
            Rectangle rect = new Rectangle(x, y, TileSize, TileSize);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, 2 * cornerRadius, 2 * cornerRadius, 180,
                            90);

                path.AddArc(rect.X + rect.Width - 2 * cornerRadius, rect.Y,
                            2 * cornerRadius, 2 * cornerRadius, 270, 90);

                path.AddArc(rect.X + rect.Width - 2 * cornerRadius,
                            rect.Y + rect.Height - 2 * cornerRadius, 2 * cornerRadius,
                            2 * cornerRadius, 0, 90);

                path.AddArc(rect.X, rect.Y + rect.Height - 2 * cornerRadius,
                            2 * cornerRadius, 2 * cornerRadius, 90, 90);
                path.CloseFigure();

                g.FillPath(brush, path);
            }
        }

        if (value > 0)
        {
            string text = value.ToString();

            float fontSize =
                (text.Length <= 2) ? 36f : (text.Length == 3 ? 30f : 24f);

            using (Font font = new Font("Arial", fontSize, FontStyle.Bold)) using (
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                }) using (Brush textBrush = new SolidBrush(GetTileTextColor(value)))
            {
                RectangleF layoutRect = new RectangleF(x, y, TileSize, TileSize);
                g.DrawString(text, font, textBrush, layoutRect, format);
            }
        }
    }
}