using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace handmarker
{
    public partial class BoundingBoxForm : Form
    {
        private bool isDrawing = true;
        private Rectangle rect = new Rectangle(0, 0, 0, 0);

        private double scale = 0;
        private int offsetX = 0;//offset是缩放后的
        private int offsetY = 0;

        private string fileName; 

        public BoundingBoxForm()
        {
            InitializeComponent();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                // 显示当前坐标
                int x = (int)Math.Round((e.X - this.offsetX) / this.scale);
                int y = (int)Math.Round((e.Y - this.offsetY) / this.scale);
                this.lblCoordinate.Text = "(" + x.ToString() + "," + y.ToString() + ")";


                // 按住鼠标左键时，绘制方框
                if(e.Button == MouseButtons.Left && this.isDrawing)
                {
                    Graphics g = this.pictureBox1.CreateGraphics();
                    int tempWidth = (int)Math.Round((e.X - offsetX) / this.scale - this.rect.X) + 1;
                    int tempHeight = (int)Math.Round((e.Y - offsetY) / this.scale - this.rect.Y) + 1;
                    Rectangle tempRect = new Rectangle(this.rect.X, this.rect.Y, tempWidth, tempHeight); // 真实坐标
                    this.pictureBox1.Refresh();
                    this.DrawBoundingBox(g, tempRect);

                }
            }
        }

        private void DrawBoundingBox(Graphics g, Rectangle rect)
        {
            // rect是原始图像上的坐标，要转换成缩放后的坐标，然后在picturebox上绘图
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿

            Rectangle tempRect = new Rectangle();
            tempRect.X = (int)Math.Round(rect.X * scale + offsetX);
            tempRect.Y = (int)Math.Round(rect.Y * scale + offsetY);
            tempRect.Width = (int)Math.Round(rect.Width * this.scale);
            tempRect.Height = (int)Math.Round(rect.Height * this.scale);


            g.DrawRectangle(new Pen(Color.Blue,(float)1.0*this.pictureBox1.Width/400), tempRect);
        }

        private void ComputeScale()
        {
            if (this.pictureBox1 == null || this.pictureBox1.Image == null)
            {
                return;
            }

            if ((double)this.pictureBox1.Image.Width / (double)this.pictureBox1.Image.Height < (double)this.pictureBox1.Width / (double)this.pictureBox1.Height)
            {
                this.scale = (double)this.pictureBox1.Height / (double)this.pictureBox1.Image.Height;
            }
            else
            {
                this.scale = (double)this.pictureBox1.Width / (double)this.pictureBox1.Image.Width;
            }

            this.offsetX = (int)Math.Round((this.pictureBox1.Width - this.pictureBox1.Image.Width * this.scale) / 2);
            this.offsetY = (int)Math.Round((this.pictureBox1.Height - this.pictureBox1.Image.Height * this.scale) / 2);
        }

        private void BoundingBoxForm_Load(object sender, EventArgs e)
        {
            
        }

        public void SetImage(Image image)
        {
            this.pictureBox1.Image = image;
            this.ComputeScale();
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            this.ComputeScale();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.isDrawing)
                {
                    this.rect.X = (int)Math.Round((e.X - this.offsetX) / this.scale);//真实坐标
                    this.rect.Y = (int)Math.Round((e.Y - this.offsetY) / this.scale);
                    this.rect.Width = 0;
                    this.rect.Height = 0;
                }
            }

        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.isDrawing)
                {
                    this.isDrawing = false;
                    this.rect.Width = (int)Math.Round((e.X - offsetX) / this.scale) - this.rect.X + 1;
                    this.rect.Height = (int)Math.Round((e.Y - offsetY) / this.scale) - this.rect.Y + 1;
                }
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            if(!this.isDrawing)
            {
                this.DrawBoundingBox(e.Graphics, this.rect);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.isDrawing = true;
            this.pictureBox1.Refresh();
            
        }

        private void BoundingBoxForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //保证bounding box在图像边界内
            if(this.rect.X<0)
            {
                this.rect.X = 0;
            }
            if(this.rect.Y<0)
            {
                this.rect.Y = 0;
            }
            if(this.rect.Width>this.pictureBox1.Image.Width)
            {
                this.rect.Width = this.pictureBox1.Image.Width;
            }
            if(this.rect.Height>this.pictureBox1.Image.Height)
            {
                this.rect.Height = this.pictureBox1.Image.Height;
            }


            //保存
            string bboxFileName = this.fileName.Substring(0, fileName.LastIndexOf(".")) + ".box";
            this.SaveBoundingBox(bboxFileName);


            //在父窗体中显示
            MainForm mainForm = (MainForm)this.Owner;
            mainForm.SetBoundingBox(this.rect);
            mainForm.DrawBoundingBoxFromChild();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                this.btnClear_Click(sender, e);
            }
        }

        private void SaveBoundingBox(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(this.rect.X.ToString() + " " + this.rect.Y.ToString() + " " + this.rect.Width.ToString() + " " + this.rect.Height.ToString());
            }
        }

        public void SetFileName(string fileName)
        {
            this.fileName = fileName;
        }
    }
}
