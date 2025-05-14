using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FileClient
{
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileName)
            {
                string extension = Path.GetExtension(fileName).ToLower();

                string iconName = extension switch
                {
                    ".txt" => "text.png",
                    ".pdf" => "pdf.png",
                    ".jpg" or ".jpeg" or ".png" => "image.png",
                    ".zip" or ".rar" => "archive.png",
                    ".mp3" or ".wav" => "audio.png",
                    ".mp4" or ".avi" => "video.png",
                    _ => "file.png"
                };

                return new BitmapImage(new Uri($"pack://application:,,,/Resources/Icons/{iconName}"));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
