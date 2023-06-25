using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


namespace Reactive
{
    public partial class PlanetForm : Form
    {
        private PlanetAgent _ownerAgent;
        private Bitmap _doubleBufferImage;
        public static Dictionary<string, Brush> colors = new Dictionary<string, Brush>();

        public PlanetForm()
        {
            InitializeComponent();
            for (int i = 0; i < Utils.NoExplorers; i++)
            {
                colors.Add("explorer" + i, Utils.PickBrush());
            }

        }

        public void SetOwner(PlanetAgent a)
        {
            _ownerAgent = a;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawPlanet();
        }

        public void UpdatePlanetGUI()
        {
            DrawPlanet();
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            DrawPlanet();
        }

        private void DrawPlanet()
        {
            int w = pictureBox.Width;
            int h = pictureBox.Height;

            if (_doubleBufferImage != null)
            {
                _doubleBufferImage.Dispose();
                GC.Collect(); // prevents memory leaks
            }

            _doubleBufferImage = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(_doubleBufferImage);
            g.Clear(Color.White);

            int minXY = Math.Min(w, h);
            int cellSize = 20;

            if (_ownerAgent != null)
            {
                for (int i = 0; i < Utils.maze.GetLength(0); i++)
                {
                    for (int j = 0; j < Utils.maze.GetLength(1); j++)
                    {
                        if (Utils.maze[i, j] == 1)
                            g.FillRectangle(Brushes.Black, i * cellSize , j * cellSize , cellSize , cellSize);
                    }
                }

                int pos = 0;
                foreach (KeyValuePair<string, string> v in _ownerAgent.ExplorerPositions)
                 {
                     string[] t = v.Value.Split();
                     int x = Convert.ToInt32(t[0]);
                     int y = Convert.ToInt32(t[1]);

                    g.FillEllipse(colors[v.Key], x * cellSize, y * cellSize , cellSize , cellSize);
                    pos += 1;
                 }
            }

            Graphics pbg = pictureBox.CreateGraphics();
            pbg.DrawImage(_doubleBufferImage, 0, 0);
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {

        }
    }
}