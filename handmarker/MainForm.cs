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
    public partial class MainForm : Form
    {
        private string currentPath = System.AppDomain.CurrentDomain.BaseDirectory.ToString();
        private List<string> allowedFileExtensions = new List<string>() { ".jpg", ".bmp", ".png" };
        private List<Point> points = new List<Point>(); //原始图像上的坐标，便于保存
        private Rectangle boundingBox = new Rectangle(); //原始图像上的坐标，便于保存

        private int numPoints = 22;
        private int pointCount = 0;
        private double scale = 0;
        private int offsetX = 0;//offset是缩放后的
        private int offsetY = 0;

        private bool isImageEdited = false;
        private string currentFileName;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.SelectedPath = currentPath;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.currentPath = dlg.SelectedPath;
                this.lblPath.Text = this.currentPath;

                DirectoryInfo dir = new DirectoryInfo(this.currentPath);

                FileInfo[] fs = dir.GetFiles();
                this.listBox1.Items.Clear();
                foreach (FileInfo f in fs)
                {
                    if (this.allowedFileExtensions.Contains(f.Extension.ToLower()))
                    {
                        this.listBox1.Items.Add(f.Name);
                    }
                }
            }

            if (this.listBox1.Items.Count > 0)
            {
                this.listBox1.SelectedIndex = 0;
                this.btnOpenBBoxWindow.Enabled = true;
                this.btnMirror.Enabled = true;
                this.btnRotateClockwise.Enabled = true;
                this.btnRotateCounterClockwise.Enabled = true;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(this.points.Count>0 && this.points.Count<this.numPoints)
            {
                if(MessageBox.Show("当前标记未完成，显示其它图像会清空当前标记，确认吗？","提示",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            // 暂存当前图片和文件名
            Bitmap editedImage = null;
            string previousFileName = this.currentFileName;
            if(this.isImageEdited)
            {
                editedImage = (Bitmap)this.pictureBox1.Image;
            }


            // 加载新图片
            this.currentFileName = this.currentPath + "\\" + this.listBox1.SelectedItem.ToString();
            FileStream fs = new FileStream(this.currentFileName, FileMode.Open, FileAccess.Read);
            this.pictureBox1.Image = Image.FromStream(fs);
            fs.Close();

            this.ComputeScale();
            this.points.Clear();
            this.pointCount = this.points.Count;
            this.boundingBox.Width = 0;

            // 加载notation
            string pointFileName = this.currentFileName.Substring(0, this.currentFileName.LastIndexOf(".")) + ".pts";
            this.points = this.LoadPoints(pointFileName);
            string bboxFileName = this.currentFileName.Substring(0, this.currentFileName.LastIndexOf(".")) + ".box";
            this.boundingBox = this.LoadBoundingBox(bboxFileName);

            // 保存编辑后的图片
            if (this.isImageEdited)
            {
                editedImage.Save(previousFileName);
                this.isImageEdited = false;
            }

        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (this.pictureBox1.Image == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // 如果没有达到预定的数目，显示
                if (this.pointCount < this.numPoints)
                {
                    Point pt = new Point();
                    pt.X = (int)Math.Round((e.X - this.offsetX) / scale);
                    pt.Y = (int)Math.Round((e.Y - this.offsetY) / scale);

                    Graphics g = this.pictureBox1.CreateGraphics();
                    this.points.Add(pt);
                    this.pointCount++;
                    this.DrawPoint(g, pt, this.pointCount);

                    // 如果达到预定的数目，自动保存
                    if (this.pointCount == this.numPoints)
                    {
                        string fileName = this.currentPath + "\\" + this.listBox1.SelectedItem.ToString();
                        string pointFileName = fileName.Substring(0, fileName.LastIndexOf(".")) + ".pts";

                        this.SavePoints(pointFileName);
                    }
                }
                else // 如果大于预定的数目，转到下一张图像
                {
                    if (this.listBox1.SelectedIndex < this.listBox1.Items.Count - 1)
                    {
                        this.listBox1.SelectedIndex = this.listBox1.SelectedIndex + 1;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 撤销一个点
                this.Cancel();
            }
        }


        private void Cancel()
        {
            if (this.points.Count > 0)
            {
                this.points.RemoveAt(this.points.Count - 1);//删除最后标记的一个点
                this.pointCount--;

                this.pictureBox1.Invalidate();
            }
        }

        private void SavePoints(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine("version: 1");
                sw.WriteLine("n_points: " + this.points.Count.ToString());
                sw.WriteLine("{");

                foreach (Point pt in this.points)
                {
                    sw.WriteLine(pt.X.ToString() + " " + pt.Y.ToString());
                }

                sw.WriteLine("}");

            }
        }

        private void DrawPoint(Graphics g, Point pt, int cnt)
        {
            // pt是原始图像上的坐标，要转换成缩放后的坐标，然后在picturebox上绘图
            // Graphics g = this.pictureBox1.CreateGraphics();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿

            int r = this.pictureBox1.Width / 300;
            Rectangle rect = new Rectangle();
            rect.X = (int)Math.Round(pt.X * this.scale + this.offsetX - Math.Sqrt(2)*r);
            rect.Y = (int)Math.Round(pt.Y * this.scale + this.offsetY - Math.Sqrt(2)*r);
            rect.Width = 2 * r + 1;
            rect.Height = 2 * r + 1;
            g.FillEllipse(new SolidBrush(Color.Red), rect);

            RectangleF stringRect = new RectangleF();
            stringRect.X = rect.X + rect.Width + 2;
            stringRect.Y = rect.Y;
            stringRect.Width = 100;
            stringRect.Height = rect.Height * 2;

            g.DrawString(cnt.ToString(), new Font("Times New Roman",4 * r,GraphicsUnit.Pixel), new SolidBrush(Color.Red),stringRect);

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


            g.DrawRectangle(new Pen(Color.Blue, (float)1.0 * this.pictureBox1.Width / 400), tempRect);
        }

        public void DrawBoundingBoxFromChild()
        {
            this.pictureBox1.Refresh();
            Graphics g = this.pictureBox1.CreateGraphics();
            this.DrawBoundingBox(g, this.boundingBox);
        }


        private void ComputeScale()
        {
            if(this.pictureBox1 == null || this.pictureBox1.Image == null)
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



        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            this.ComputeScale();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                int x = (int)Math.Round((e.X - this.offsetX) / this.scale);
                int y =(int)Math.Round((e.Y - this.offsetY) / this.scale);
                this.lblCoordinate.Text = "(" + x.ToString() + "," + y.ToString() + ")";
            }
        }

        private List<Point> LoadPoints(string fileName)
        {
            List<Point> points = new List<Point>();
            int lineCount = 0;
            int num_pts = 0;
            double x = 0;
            double y = 0;

            if (new FileInfo(fileName).Exists)
            {
                using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        lineCount++;

                        if (line == "}")
                        {
                            break;
                        }

                        if (lineCount > 3)
                        {
                            string[] strs = line.Split(' ');

                            double.TryParse(strs[0], out x);
                            double.TryParse(strs[1], out y);
                            Point pt = new Point((int)Math.Round(x), (int)Math.Round(y));

                            points.Add(pt);

                            num_pts++;
                        }

                    }
                }
            }

            this.pointCount = points.Count;

            return points;
        }

        private Rectangle LoadBoundingBox(string fileName)
        {
            Rectangle rect = new Rectangle(0,0,0,0);
            int x = 0;
            int y = 0;
            int w = 0;
            int h = 0;
            if(new FileInfo(fileName).Exists)
            {
                using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
                {
                    // assume that only one bounding box in one file
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Trim() != "")
                        {
                            string[] strs = line.Split(' ');
                            int.TryParse(strs[0], out x);
                            int.TryParse(strs[1], out y);
                            int.TryParse(strs[2], out w);
                            int.TryParse(strs[3], out h);

                            rect.X = x;
                            rect.Y = y;
                            rect.Width = w;
                            rect.Height = h;
                        }
                    }
                }
            }

            return rect;
                
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            int cnt = 1;
            foreach (Point pt in this.points)
            {
                this.DrawPoint(e.Graphics, pt, cnt);
                cnt++;
            }

            this.DrawBoundingBox(e.Graphics, this.boundingBox);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("清空所有标记吗？","提示",MessageBoxButtons.YesNo,MessageBoxIcon.Question)== DialogResult.Yes)
            {
                this.points.Clear();
                this.pointCount = this.points.Count;
                this.pictureBox1.Invalidate();
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Cancel();
        }

        private void btnOpenBBoxWindow_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                BoundingBoxForm bboxForm = new BoundingBoxForm();
                bboxForm.Owner = this;
                bboxForm.SetImage(this.pictureBox1.Image);
                bboxForm.SetFileName(this.currentPath + "\\" + this.listBox1.SelectedItem.ToString());
                bboxForm.ShowDialog();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        public void SetBoundingBox(Rectangle rect)
        {
            this.boundingBox = rect;
        }


        private void EditImage(RotateFlipType type)
        {
            Bitmap bmp = (Bitmap)this.pictureBox1.Image;
            bmp.RotateFlip(type);
            this.pictureBox1.Image = bmp;

            this.points.Clear();
            this.pointCount = this.points.Count;
            this.boundingBox.Width = 0;
            this.pictureBox1.Invalidate();

            this.ComputeScale();

            this.isImageEdited = true;
        }

        private void btnMirror_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                if (this.points.Count > 0 || this.boundingBox.Width > 0)
                {
                    if (MessageBox.Show("编辑图像需要重新进行标记，确认清除所有标记吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.EditImage(RotateFlipType.RotateNoneFlipX);
                    }
                }
                else
                {
                    this.EditImage(RotateFlipType.RotateNoneFlipX);
                }
            }
        }

        

        private void btnRotateClockwise_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                if (this.points.Count > 0 || this.boundingBox.Width > 0)
                {
                    if (MessageBox.Show("编辑图像需要重新进行标记，确认清除所有标记吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.EditImage(RotateFlipType.Rotate90FlipNone);
                    }
                }
                else
                {
                    this.EditImage(RotateFlipType.Rotate90FlipNone);
                }
            }
        }

        private void btnRotateCounterClockwise_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                if (this.points.Count > 0 || this.boundingBox.Width > 0)
                {
                    if (MessageBox.Show("编辑图像需要重新进行标记，确认清除所有标记吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.EditImage(RotateFlipType.Rotate270FlipNone);
                    }
                }
                else
                {
                    this.EditImage(RotateFlipType.Rotate270FlipNone);
                }
            }
        }
    }
}
