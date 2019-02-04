namespace Unosquare.Sparkfun.Playground
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using FingerprintModule;
    using Swan;
#if NET461
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
#endif

    public class Program
    {
        private const int InitialBaudRate = 9600;

        private static readonly Dictionary<ConsoleKey, string> Options = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            {ConsoleKey.C, "Count enrolled users"},
            {ConsoleKey.M, "Match 1:N"},
            {ConsoleKey.I, "Get Image"},
            {ConsoleKey.R, "Get Raw Image"},
            {ConsoleKey.S, "Enter Standby Mode"},
        };

        public static async Task Main(string[] args) 
        {
            try
            {
                "Getting ports...".Info();

                foreach (var p in FingerprintReader.GetPortNames())
                {
                    $"Port: {p}".Info();
                }

                "Creating port...".Info();
                var reader = new FingerprintReader(FingerprintReaderModel.GT521F52);

                $"Opening port at {InitialBaudRate}...".Info();
                await reader.OpenAsync("COM4");

                $"Serial Number: {reader.SerialNumber}".Info();
                $"Firmware Version: {reader.FirmwareVersion}".Info();

                while (true)
                {
                    var option = "Select an option".ReadPrompt(Options, "Esc to quit");
                    if (option.Key == ConsoleKey.C)
                    {
                        var countResponse = await reader.CountEnrolledFingerprintAsync();

                        if (countResponse.IsSuccessful)
                            $"Users enrolled: {countResponse.EnrolledFingerprints}".Info();
                    }
                    else if (option.Key == ConsoleKey.M)
                    {
                        try
                        {
                            var matchResponse = await reader.MatchOneToN();

                            if (matchResponse.IsSuccessful)
                                $"UserId: {matchResponse.UserId}".Info();
                            else
                                $"Error: {matchResponse.ErrorCode}".Error();
                        }
                        catch (OperationCanceledException ex)
                        {
                            $"Error: {ex.Message}".Error();
                        }
                    }
                    else if (option.Key == ConsoleKey.I)
                    {
                        try
                        {
                            var imageResponse = await reader.GetImageAsync();
                            if (imageResponse.IsSuccessful)
                            {
                                $"Image size: {imageResponse.Image.Length}bytes".Info();
#if NET461
                                SaveImage(imageResponse.Image, 202, 258, "Image.bmp");
#endif
                            }
                            else
                            {
                                $"Error: {imageResponse.ErrorCode}".Error();
                            }
                        }
                        catch (OperationCanceledException ex)
                        {
                            $"Error: {ex.Message}".Error();
                        }
                    }
                    else if (option.Key == ConsoleKey.R)
                    {
                        try
                        {
                            var imageResponse = await reader.GetRawImageAsync();
                            if (imageResponse.IsSuccessful)
                            {
                                $"Image size: {imageResponse.Image.Length}bytes".Info();
#if NET461
                                SaveImage(imageResponse.Image, 160, 120, "RawImage.bmp");
#endif
                            }
                            else
                            {
                                $"Error: {imageResponse.ErrorCode}".Error();
                            }
                        }
                        catch (OperationCanceledException ex)
                        {
                            $"Error: {ex.Message}".Error();
                        }
                    }
                    else if (option.Key == ConsoleKey.S)
                    {
                        var standbyResponse = await reader.EnterStandByMode();

                        if (standbyResponse.IsSuccessful)
                            "Standby Mode".Info();
                        else
                            $"Error: {standbyResponse.ErrorCode}".Error();
                    }
                    else if (option.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                "Closing port...".Info();
                reader.Dispose();
                Console.Clear();
            }
            catch (Exception ex)
            {
                ex.Log("Program.Main");
                Console.ReadLine();
            }
        }

#if NET461
        private static void SaveImage(byte[] image, int width, int height, string fileName)
        {
            var newData = new byte[image.Length * 4];

            for (int x = 0; x < image.Length; x++)
            {
                newData[x * 4] = image[x];
                newData[(x * 4) + 1] = image[x];
                newData[(x * 4) + 2] = image[x];
                newData[(x * 4) + 3] = image[x];
            }

            using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppRgb))
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                                  ImageLockMode.WriteOnly,
                                                  bmp.PixelFormat);

                var pnative = bmpData.Scan0;
                Marshal.Copy(newData, 0, pnative, newData.Length);

                bmp.UnlockBits(bmpData);

                bmp.Save(fileName);
            }
        }
#endif
    }
}
