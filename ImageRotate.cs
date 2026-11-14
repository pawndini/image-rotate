using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

internal static class ImageFiles
{
    internal const string OpenFilter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*";
    internal const string SaveFilter = "PNG image|*.png|JPEG image|*.jpg|Bitmap image|*.bmp|TIFF image|*.tiff";
    private static readonly ImageFormat[] Formats = { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Tiff };

    internal static Bitmap Open(string path)
    {
        using (var source = Image.FromFile(path))
        {
            const int Orientation = 0x0112;
            if (Array.IndexOf(source.PropertyIdList, Orientation) >= 0)
            {
                var value = source.GetPropertyItem(Orientation).Value;
                if (value.Length >= 2)
                {
                    int orientation = value[0] == 0 ? value[1] : value[0];
                    var transforms = new[] {
                        RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipNone,
                        RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate180FlipNone,
                        RotateFlipType.Rotate180FlipX, RotateFlipType.Rotate90FlipX,
                        RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate270FlipX,
                        RotateFlipType.Rotate270FlipNone
                    };
                    if (orientation >= 2 && orientation <= 8) source.RotateFlip(transforms[orientation]);
                }
            }
            return new Bitmap(source);
        }
    }

    internal static int FormatIndex(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".png": return 1;
            case ".jpg": case ".jpeg": return 2;
            case ".bmp": return 3;
            case ".tif": case ".tiff": return 4;
            default: return 0;
        }
    }

    internal static void Save(Bitmap bitmap, string path)
    {
        int index = FormatIndex(path);
        if (index == 0) throw new IOException("Use a PNG, JPG, BMP, or TIFF file extension.");
        string temp = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew))
            {
                if (index == 2)
                {
                    using (var flattened = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
                    {
                        using (var graphics = Graphics.FromImage(flattened))
                        {
                            graphics.Clear(Color.White);
                            graphics.DrawImageUnscaled(bitmap, 0, 0);
                        }
                        using (var options = new EncoderParameters(1))
                        {
                            options.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
                            var codec = Array.Find(ImageCodecInfo.GetImageEncoders(), item => item.FormatID == ImageFormat.Jpeg.Guid);
                            flattened.Save(stream, codec, options);
                        }
                    }
                }
                else bitmap.Save(stream, Formats[index - 1]);
            }
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}

internal sealed class Editor : Form
{
    private readonly PictureBox preview = new PictureBox();
    private readonly Label empty = new Label();
    private readonly Label status = new Label();
    private readonly Button left;
    private readonly Button right;
    private readonly Button save;
    private Bitmap bitmap;
    private string sourcePath;
    private int angle;

    internal Editor()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Image Rotate";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(920, 620);
        MinimumSize = new Size(690, 430);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(244, 246, 250);
        ForeColor = Color.FromArgb(25, 34, 53);
        KeyPreview = true;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(22) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var toolbar = new FlowLayoutPanel {
            Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true, Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(toolbar, 0, 0);
        toolbar.Controls.Add(MakeButton("Open image...", OpenImage, false));
        toolbar.Controls.Add(MakeButton("Paste image", PasteImage, false));
        left = MakeButton("Rotate left 90°", () => Turn(false), false);
        right = MakeButton("Rotate right 90°", () => Turn(true), false);
        save = MakeButton("Save as...", SaveImage, true);
        toolbar.Controls.AddRange(new Control[] { left, right, save });

        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 37, 55), Margin = Padding.Empty, Padding = new Padding(12) };
        layout.Controls.Add(panel, 0, 1);
        preview.Dock = DockStyle.Fill;
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        panel.Controls.Add(preview);
        empty.Dock = DockStyle.Fill;
        empty.Text = "Open, drop, or paste an image";
        empty.TextAlign = ContentAlignment.MiddleCenter;
        empty.ForeColor = Color.White;
        empty.Font = new Font("Segoe UI", 15);
        panel.Controls.Add(empty);
        empty.BringToFront();
        empty.DoubleClick += (sender, args) => OpenImage();
        foreach (Control control in new Control[] { this, panel, preview, empty })
        {
            control.AllowDrop = true;
            control.DragEnter += (sender, args) =>
            {
                if (args.Data.GetDataPresent(DataFormats.FileDrop)) args.Effect = DragDropEffects.Copy;
            };
            control.DragDrop += (sender, args) =>
            {
                var files = args.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length == 1) LoadImage(files[0]);
                else MessageBox.Show(this, "Drop one image at a time.", Text);
            };
        }
        status.Dock = DockStyle.Fill;
        status.AutoSize = true;
        status.Margin = new Padding(0, 8, 0, 0);
        status.TextAlign = ContentAlignment.MiddleLeft;
        status.AutoEllipsis = true;
        layout.Controls.Add(status, 0, 2);
        UpdatePreview();
        ResumeLayout(true);
    }

    private static Button MakeButton(string text, Action action, bool primary)
    {
        var button = new Button {
            Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 7, 12, 7), Margin = new Padding(0, 0, 8, 6),
            FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
            BackColor = primary ? Color.FromArgb(35, 73, 194) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(25, 34, 53)
        };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 222);
        button.Click += (sender, args) => action();
        return button;
    }

    private void UpdatePreview()
    {
        preview.Image = bitmap;
        empty.Visible = bitmap == null;
        left.Enabled = right.Enabled = save.Enabled = bitmap != null;
        save.BackColor = bitmap == null ? Color.FromArgb(226, 230, 238) : Color.FromArgb(35, 73, 194);
        status.Text = bitmap == null ? "PNG, JPEG, BMP, GIF and TIFF" :
            String.Format("{0}     {1} × {2} px     {3}° clockwise", sourcePath == null ? "Clipboard image" : Path.GetFileName(sourcePath), bitmap.Width, bitmap.Height, angle);
    }

    private void OpenImage()
    {
        using (var dialog = new OpenFileDialog { Filter = ImageFiles.OpenFilter, Title = "Open image" })
            if (dialog.ShowDialog(this) == DialogResult.OK) LoadImage(dialog.FileName);
    }

    private void LoadImage(string path)
    {
        try
        {
            SetImage(ImageFiles.Open(path), path);
        }
        catch (Exception ex) { ShowError("Could not open the image", ex); }
    }

    private void PasteImage()
    {
        try
        {
            if (TryPasteImage(Clipboard.GetDataObject())) return;
            MessageBox.Show(this, "Copy an image or a single image file, then try again.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError("Could not paste the image", ex); }
    }

    private bool TryPasteImage(IDataObject data)
    {
        if (data == null) return false;

        // Apps and browsers can include a PNG alongside the Windows bitmap.
        var png = data.GetData("PNG", false) as Stream;
        if (png != null)
        {
            if (png.CanSeek) png.Position = 0;
            using (var image = Image.FromStream(png))
                SetImage(new Bitmap(image), null);
            return true;
        }

        using (var image = data.GetData(DataFormats.Bitmap, true) as Image)
        {
            if (image != null)
            {
                SetImage(new Bitmap(image), null);
                return true;
            }
        }

        var files = data.GetData(DataFormats.FileDrop, false) as string[];
        if (files == null || files.Length != 1) return false;
        LoadImage(files[0]);
        return true;
    }

    private void SetImage(Bitmap next, string path)
    {
        var previous = bitmap;
        bitmap = next;
        sourcePath = path;
        angle = 0;
        UpdatePreview();
        if (previous != null) previous.Dispose();
    }

    private void Turn(bool clockwise)
    {
        if (bitmap == null) return;
        bitmap.RotateFlip(clockwise ? RotateFlipType.Rotate90FlipNone : RotateFlipType.Rotate270FlipNone);
        angle = (angle + (clockwise ? 90 : 270)) % 360;
        UpdatePreview();
        preview.Invalidate();
    }

    private void SaveImage()
    {
        if (bitmap == null) return;
        int index = sourcePath == null ? 1 : ImageFiles.FormatIndex(sourcePath);
        string[] extensions = { ".png", ".jpg", ".bmp", ".tiff" };
        using (var dialog = new SaveFileDialog {
            Filter = ImageFiles.SaveFilter, FilterIndex = index == 0 ? 1 : index,
            FileName = (sourcePath == null ? "clipboard-image" : Path.GetFileNameWithoutExtension(sourcePath)) + "-rotated",
            InitialDirectory = sourcePath == null ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) : Path.GetDirectoryName(sourcePath), AddExtension = true, OverwritePrompt = true,
            Title = "Save rotated image"
        })
        {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            string path = dialog.FileName;
            if (String.IsNullOrEmpty(Path.GetExtension(path))) path += extensions[dialog.FilterIndex - 1];
            try
            {
                ImageFiles.Save(bitmap, path);
                status.Text = "Saved " + Path.GetFileName(path);
            }
            catch (Exception ex) { ShowError("Could not save the image", ex); }
        }
    }

    private void ShowError(string heading, Exception error)
    {
        MessageBox.Show(this, heading + ".\r\n\r\n" + error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keys)
    {
        switch (keys)
        {
            case Keys.Control | Keys.O: OpenImage(); return true;
            case Keys.Control | Keys.V: PasteImage(); return true;
            case Keys.Control | Keys.S: SaveImage(); return true;
            case Keys.Control | Keys.Left: Turn(false); return true;
            case Keys.Control | Keys.Right: Turn(true); return true;
            default: return base.ProcessCmdKey(ref message, keys);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && bitmap != null)
        {
            preview.Image = null;
            bitmap.Dispose();
            bitmap = null;
        }
        base.Dispose(disposing);
    }

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Editor());
    }
}
