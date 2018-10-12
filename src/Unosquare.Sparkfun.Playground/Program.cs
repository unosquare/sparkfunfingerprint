namespace Unosquare.Sparkfun.Playground
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FingerprintModule;
    using Swan;

    public class Program
    {
        private const int InitialBaudRate = 9600;

        private static readonly Dictionary<ConsoleKey, string> Options = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            {ConsoleKey.C, "Count enrolled users"},
            {ConsoleKey.M, "Match 1:N"},
            {ConsoleKey.S, "Enter standby mode"},
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
    }
}
