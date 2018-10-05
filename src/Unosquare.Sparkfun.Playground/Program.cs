namespace Unosquare.Sparkfun.Playground
{
    using System;
    using System.Collections.Generic;
    using FingerprintModule;
    using Swan;
#if NETCOREAPP2_1
    using RJCP.IO.Ports;
#else
    using System.IO.Ports;
#endif

    public class Program
    {
        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 115200;

        private static readonly Dictionary<ConsoleKey, string> Options = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            {ConsoleKey.C, "Count enrolled users"},
            {ConsoleKey.M, "Match 1:N"},
            {ConsoleKey.S, "Enter standby mode"},
        };

        static void Main(string[] args)
        {
            try
            {
                "Getting ports...".Info();
#if NETCOREAPP2_1
                foreach (var p in SerialPortStream.GetPortNames())
#else
                foreach (var p in SerialPort.GetPortNames())
#endif

                {
                    $"Port: {p}".Info();
                }

                "Creating port...".Info();

                var reader = new FingerprintReader(FingerprintReaderModel.GT521F52);

                $"Opening port at {InitialBaudRate}...".Info();
                reader.OpenAsync("COM4").Wait();

                $"Serial Number: {reader.SerialNumber}".Info();
                $"Firmware Version: {reader.FirmwareVersion}".Info();

                while (true)
                {
                    //Console.Clear();
                    var option = "Select an option".ReadPrompt(Options, "Esc to quit");
                    if (option.Key == ConsoleKey.C)
                    {
                        var countResponse = reader.CountEnrolledFingerprintAsync().Result;
                        if (countResponse.IsSuccessful)
                            $"Users enrolled: {countResponse.EnrolledFingerprints}".Info();
                    }
                    else if (option.Key == ConsoleKey.M)
                    {
                        var matchResponse = reader.MatchOneToN().Result;
                        if (matchResponse.IsSuccessful)
                            $"UserId: {matchResponse.UserId}".Info();
                        else
                            $"Error: {matchResponse.ErrorCode}".Error();
                    }
                    else if (option.Key == ConsoleKey.S)
                    {
                        var standbyResponse = reader.EnterStandByMode().Result;
                        if (standbyResponse.IsSuccessful)
                            $"Standby Mode".Info();
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
                ex.Message.Error();
                Console.ReadLine();
            }
        }
    }
}
