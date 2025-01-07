using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Imazen.WebP;

namespace ImageResizer
{
    public partial class Form1 : Form
    {
        private bool isCancelled = false;
        public Form1()
        {
            InitializeComponent();

            // On form load, retrieve saved folder paths and populate textboxes
            var paths = ConfigHelper.GetSavedFolderPaths();
            if (!string.IsNullOrEmpty(paths.sourcePath))
            {
                sourceDir.Text = paths.sourcePath;  // Set source folder path to the textbox
            }
            if (!string.IsNullOrEmpty(paths.destinationPath))
            {
                desDir.Text = paths.destinationPath;  // Set destination folder path to the textbox
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isCancelled = true;
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    sourceDir.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    desDir.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void ProcessImages(string sourceFolder, string destinationFolder, List<(int Width, int Height)> sizes)
        {
            Invoke((Action)(() => btnStart.Enabled = false));
            var files = Directory.GetFiles(sourceFolder, "*.jpg");
            int processedCount = 0;

            foreach (var file in files)
            {
                if (isCancelled) break;
                string productCode = Path.GetFileName(file).Split('_')[0];

                foreach (var (width, height) in sizes)
                {
                    string resolutionFolder = Path.Combine(destinationFolder, productCode, $"{width}");
                    Directory.CreateDirectory(resolutionFolder);

                    string jpgOutputPath = Path.Combine(resolutionFolder, Path.GetFileName(file));
                    string webpOutputPath = Path.Combine(resolutionFolder, Path.GetFileNameWithoutExtension(file) + ".webp");

                    // Check for existing files and warn of overwrites
                    if (File.Exists(jpgOutputPath) || File.Exists(webpOutputPath))
                    {
                        DialogResult result = MessageBox.Show(
                            $"One or both of the files '{Path.GetFileName(jpgOutputPath)}' or '{Path.GetFileName(webpOutputPath)}' already exist in the destination folder.\nDo you want to overwrite them?",
                            "File Exists",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );

                        if (result == DialogResult.No)
                        {
                            continue; // Skip saving these files
                        }
                    }

                    // Resize and save the image as JPG and WEBP
                    using (var originalImage = Image.FromFile(file))
                    {
                        var resizedImage = ResizeImage(originalImage, width, height);

                        // Save as JPG
                        resizedImage.Save(jpgOutputPath, ImageFormat.Jpeg);
                        SaveImageAsJpg(resizedImage, jpgOutputPath);

                        // Save as WEBP using SimpleEncoder
                        SaveImageAsWebp(resizedImage, webpOutputPath);
                    }
                }

                Directory.CreateDirectory(oriDir.Text);
                File.Move(file, Path.Combine(oriDir.Text, Path.GetFileName(file)));

                processedCount++;
                UpdateProgress(processedCount, files.Length);
            }

            Invoke((Action)(() =>
            {
                btnStart.Enabled = true;
                MessageBox.Show($"Processed {processedCount} images.", "Completed", MessageBoxButtons.OK);
            }));
        }

        private Image ResizeImage(Image original, int targetWidth, int targetHeight)
        {
            float ratio = Math.Min((float)targetWidth / original.Width, (float)targetHeight / original.Height);
            int newWidth = (int)(original.Width * ratio);
            int newHeight = (int)(original.Height * ratio);

            var resized = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }

            return resized;
        }

        private void SaveImageAsWebp(Image image, string outputPath)
        {
            using (Bitmap bitmap = new Bitmap(image)) // Convert Image to Bitmap
            {
                // Set the DPI to 300 for high-quality output
                bitmap.SetResolution(96, 96);

                using (var saveImageStream = File.Open(outputPath, FileMode.Create))
                {
                    var encoder = new SimpleEncoder(); // Ensure SimpleEncoder is available
                    encoder.Encode(bitmap, saveImageStream, 90); // Adjust quality factor if needed
                }
            }
        }
        private void SaveImageAsJpg(Image image, string outputPath)
        {
            using (Bitmap bitmap = new Bitmap(image)) // Convert Image to Bitmap
            {
                // Set DPI to 300 for high-quality output
                bitmap.SetResolution(96, 96);

                // Save as JPG with quality settings
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L); // 100 = highest quality
                var jpegCodec = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                bitmap.Save(outputPath, jpegCodec, encoderParameters);
            }
        }
        private void UpdateProgress(int completed, int total)
        {
            Invoke((Action)(() =>
            {
                progressBar1.Maximum = total;
                progressBar1.Value = completed;
            }));
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(sourceDir.Text) || string.IsNullOrWhiteSpace(desDir.Text))
            {
                MessageBox.Show("Please select both source and destination folders.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sizes = new List<(int Width, int Height)>
            {
                ((int)width1.Value, (int)height1.Value),
                ((int)width2.Value, (int)height2.Value),
                ((int)width3.Value, (int)height3.Value),
                ((int)width4.Value, (int)height4.Value)
            };

            Task.Run(() => ProcessImages(sourceDir.Text, desDir.Text, sizes));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Get the source and destination paths from the textboxes
            string sourcePath = sourceDir.Text;  // Retrieve the source folder path
            string destinationPath = desDir.Text;  // Retrieve the destination folder path

            // Save the folder paths to app.config
            ConfigHelper.SaveFolderPaths(sourcePath, destinationPath);
            base.OnFormClosing(e);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    oriDir.Text = folderDialog.SelectedPath;
                }
            }
        }
    }
}
