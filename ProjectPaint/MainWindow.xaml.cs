using Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isDrawing;
        Point2D anchor = new Point2D(-1, -1);
        ShapeType currentShapeType = ShapeType.Line2D;
        bool shiftMode;
        bool ctrlMode;
        List<IShape> shapes = new List<IShape>();
        Dictionary<int, List<Image>> images = new Dictionary<int, List<Image>>();
        List<IShape> redoList = new List<IShape>();
        IShape previewShape;
        public MainWindow()
        {
            InitializeComponent();
            DllLoader.loadDLL();
            var types = DllLoader.Types;
            foreach (var t in types)
            {
                if ("IShape" == t.BaseType.Name )
                {
                    System.Windows.Controls.Button newBtn = new Button();

                    newBtn.Content = t.Name;
                    newBtn.Name = t.Name;
                    newBtn.Margin = new Thickness(16, 8, 16,8);
                    newBtn.Padding = new Thickness(8, 8, 8, 8);
                    newBtn.Click += (sender, EventArgs) => { buttonClick(sender, EventArgs, t.Name); };
                    btnPannel.Children.Add(newBtn);
                }
            }
            KeyDown += new KeyEventHandler(OnButtonKeyDown);
            KeyUp += new KeyEventHandler(OnButtonKeyUp);
        }

        private void buttonClick(object sender, EventArgs e, string shape)
        {
            if (Enum.IsDefined(typeof(ShapeType), shape))
            {
                ShapeType myShape;
                currentShapeType = EnumHelper.ToEnum<ShapeType>(shape);

            }
        }


        private void C_MouseDown(object sender, MouseButtonEventArgs e)
        {
            redoList.Clear();
            double thickness = double.Parse(thicknessBox.Text);
            previewShape = (IShape)GetInstance($"{currentShapeType}");
            previewShape.StrokeThickness = thickness;
            isDrawing = true;
            Point currenCoord = e.GetPosition(MainCanvas);
            anchor.X = currenCoord.X;
            anchor.Y = currenCoord.Y;
            previewShape.HandleStart(anchor);
        }

        private void C_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                Point coord = e.GetPosition(MainCanvas);

                Point2D point = new Point2D(coord.X, coord.Y);
                previewShape.HandleEnd(point);
                if (shiftMode)
                {
                    previewShape.HandleShiftMode();
                }
                previewShape.DashStyle = ((ComboBoxItem)line.SelectedItem).Tag.ToString();
                MainCanvas.Children.Clear();
                redraw();
                previewShape.Color = Options.previewColor;
                MainCanvas.Children.Add(previewShape.Draw());
            }
        }

        private void C_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
            Point coord = e.GetPosition(MainCanvas);
            Point2D point = new Point2D(coord.X, coord.Y);
            if (previewShape != null)
            {
                previewShape.Color = Options.strokeColor;
                previewShape.HandleEnd(point);
                if (shiftMode)
                {
                    previewShape.HandleShiftMode();
                }
                previewShape.DashStyle = ((ComboBoxItem)line.SelectedItem).Tag.ToString();
                System.Drawing.Color temp = System.Drawing.Color.FromName(((ComboBoxItem)colorBox.SelectedItem).Tag.ToString());
                previewShape.Color = Color.FromArgb(temp.A, temp.R, temp.G, temp.B);
                shapes.Add(previewShape);
                MainCanvas.Children.Clear();
                redraw();
            }
        }

        private void CreateSaveBitmap(Canvas canvas, string filename)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)canvas.ActualWidth, (int)canvas.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            canvas.Measure(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight));
            canvas.Arrange(new Rect(new Size((int)canvas.ActualWidth, (int)canvas.ActualHeight)));

            renderBitmap.Render(canvas);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (FileStream file = File.Create(filename))
            {
                encoder.Save(file);
            }
        }

        private void SaveImg(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            //saveFileDialog.InitialDirectory = @"C:\";
            saveFileDialog.Filter = "Images|*.png";
            saveFileDialog.Title = "Save as PNG";
            saveFileDialog.RestoreDirectory = true;
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                String fileName = saveFileDialog.FileName;
                CreateSaveBitmap(MainCanvas, fileName);
            }
        }

        private void CreateLoadBitmap(ref Canvas canvas, string filename)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filename, UriKind.Absolute);
            bitmap.EndInit();

            Image image = new Image();
            image.Source = bitmap;
            image.Width = bitmap.Width;
            image.Height = bitmap.Height;
            if (bitmap.Width > canvas.Width || double.IsNaN(canvas.Width))
            {
                canvas.Width = bitmap.Width > canvas.ActualWidth ? bitmap.Width : double.NaN;
            }
            if (bitmap.Height > canvas.Height || double.IsNaN(canvas.Height))
            {
                canvas.Height = bitmap.Height > canvas.ActualHeight ? bitmap.Height : double.NaN;
            }
            if (!images.ContainsKey(shapes.Count))
            {
                images[shapes.Count] = new List<Image>();
            }
            images[shapes.Count].Add(image);
            canvas.Children.Add(image);
        }

        private void UploadImg(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Load image";
            openFileDialog.Filter = "Images|*.png;*.bmp;*.jpg";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == true)
            {
                previewShape = null;
                CreateLoadBitmap(ref MainCanvas, openFileDialog.FileName);
            };
        }

        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                shiftMode = true;
            }
            if (e.Key == Key.LeftCtrl)
            {
                ctrlMode = true;
            }
             if (e.Key == Key.Z && ctrlMode && !shiftMode)
            {
                if (images.ContainsKey(0) && shapes.Count == 0)
                {
                    if (images[0].Count > 0)
                    {
                        images[0].RemoveAt(images[0].Count - 1);
                        MainCanvas.Children.RemoveAt(MainCanvas.Children.Count - 1);
                    }
                }
                if (shapes.Count > 0)
                {
                    if (images.ContainsKey(shapes.Count) && images[shapes.Count].Count > 0)
                    {
                        images[shapes.Count].RemoveAt(images[shapes.Count].Count - 1);
                    }
                    else
                    {
                        redoList.Add(shapes[shapes.Count - 1]);
                        shapes.RemoveAt(shapes.Count - 1);
                    }
                    MainCanvas.Children.RemoveAt(MainCanvas.Children.Count - 1);
                }
            }
            else if (e.Key == Key.Z && ctrlMode && shiftMode)
            {
                if (redoList.Count > 0)
                {
                    shapes.Add(redoList[redoList.Count - 1]);
                    MainCanvas.Children.Add(shapes[shapes.Count - 1].Draw());
                    redoList.RemoveAt(redoList.Count - 1);
                }
            }
        }
        private void OnButtonKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                shiftMode = false;
            }
            else if (e.Key == Key.LeftCtrl)
            {
                ctrlMode = false;
            }
        }

        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            var types = DllLoader.Types;
            foreach (var t in types)
            {
                if (t.Name == strFullyQualifiedName)
                    return Activator.CreateInstance(t);
            }
            return null;
        }

        private void redraw()
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                if (images.ContainsKey(i))
                {
                    foreach (var image in images[i])
                    {
                        MainCanvas.Children.Add(image);
                    }
                }
                var element = shapes[i].Draw();
                MainCanvas.Children.Add(element);
            }
        }

      
    }
}
