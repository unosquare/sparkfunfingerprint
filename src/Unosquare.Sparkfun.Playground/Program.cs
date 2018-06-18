namespace Unosquare.Sparkfun.Playground
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Linq;
    using System.Threading;
    using FingerprintScanner;
    using Swan;

    class Program
    {

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 115200;

        private static readonly Dictionary<ConsoleKey, string> Options = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            {ConsoleKey.U, "Enrolled user"},
            {ConsoleKey.E, "Enroll new user"},
            {ConsoleKey.I, "Identify user"},
            {ConsoleKey.T, "Get Template"},
            {ConsoleKey.R, "Reset fingerprint reader"},
        };

        private static FingerprintReader reader;

        static void Main(string[] args)
        {
            try
            {
                "Getting ports...".Info();

                foreach (var p in SerialPort.GetPortNames())
                {
                    $"Puerto: {p}".Info();
                }

                "Creating port...".Info();

                var reader = new FingerprintReader(FingerprintReaderModel.GT521F52);

                $"Opening port at {InitialBaudRate}...".Info();

                reader.Open("COM4").Wait();

                //while (true)
                //{
                //    Console.Clear();
                //    var option = "Select an option".ReadPrompt(Options, "Quit");
                //    if (option.Key == ConsoleKey.U)
                //    {
                //        GetEnrollingCount();
                //        Console.ReadKey();
                //    }
                //    else if (option.Key == ConsoleKey.E)
                //    {
                //        EnrollUser();
                //    }
                //    else if (option.Key == ConsoleKey.I)
                //    {
                //        IdentifyUser();
                //    }
                //    else if (option.Key == ConsoleKey.T)
                //    {
                //        GetTemplate();
                //    }
                //    else if (option.Key == ConsoleKey.R)
                //    {
                //        ResetFingerprintReader();
                //    }
                //    else
                //        break;
                //}

                Console.ReadLine();

                "Closing port...".Info();
                reader.Dispose();
                Console.Clear();
            }
            catch (Exception ex)
            {
                ex.Message.Error();
            }
        }

        //private static void EnrollUser()
        //{
        //    try
        //    {
        //        Console.Clear();

        //        var index = GetEnrollingCount();

        //        "Starting new enrollment...".Info();
        //        var result = Write(Command.EnrollStart, index);

        //        if (result.HasError)
        //            throw new Exception(result.Error.ToString());

        //        for (int i = 0; i < 3; i++)
        //        {
        //            $"TEMPLATE {i + 1}...".Info();

        //            WaitFingerAction(FingerAction.Place);

        //            result = Write(Command.CaptureFinger);
        //            if (result.HasError)
        //                throw new Exception(result.Error.ToString());

        //            var cmd = Command.Enroll3;
        //            switch (i)
        //            {
        //                case 0:
        //                    cmd = Command.Enroll1;
        //                    break;
        //                case 1:
        //                    cmd = Command.Enroll2;
        //                    break;
        //                default:
        //                    cmd = Command.Enroll3;
        //                    break;
        //            }

        //            $"Enrolling template {i + 1}...".Info();
        //            result = Write(cmd);
        //            if (result.HasError)
        //                throw new Exception(result.Error.ToString());

        //            if (i < 2)
        //                WaitFingerAction(FingerAction.Remove);
        //        }

        //        result = Write(Command.CheckEnrolled, index);

        //        if (result.HasError)
        //            throw new Exception(result.Error.ToString());

        //        $"User enrolled".Info();

        //        GetEnrollingCount();
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Log("Error");
        //    }
        //    Console.ReadKey();
        //}

        //private static void IdentifyUser()
        //{
        //    try
        //    {
        //        WaitFingerAction(FingerAction.Place);

        //        var result = Write(Command.CaptureFinger);
        //        if (result.HasError)
        //            throw new Exception(result.Error.ToString());

        //        result = Write(Command.Identify);
        //        if (result.HasError)
        //        {
        //            $"Invalid user ({result.Error})".Error();
        //        }
        //        else
        //        {
        //            $"Valid user ({result.Parameter})".Info();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Message.Error();
        //    }
        //    Console.ReadKey();
        //}

        //private static void ResetFingerprintReader()
        //{
        //    try
        //    {
        //        Console.Clear();

        //        GetEnrollingCount();

        //        "Deleting DB...".Info();
        //        var result = Write(Command.DeleteAll);
        //        if (result.HasError)
        //            throw new Exception(result.Error.ToString());

        //        GetEnrollingCount();
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Message.Error();
        //    }
        //    Console.ReadKey();
        //}

        //private static void GetTemplate()
        //{
        //    try
        //    {
        //        Console.Clear();

        //        var count = GetEnrollingCount();

        //        if (count == 0)
        //            "Fingerprint reader DB is empty.".Info();

        //        //Write(Command.Get, 0);
        //        Console.ReadKey();
        //    }
        //    catch (Exception ex)
        //    {
        //        ex.Message.Error();
        //    }
        //    Console.ReadKey();
        //}

        //private static void WaitFingerAction(FingerAction action)
        //{
        //    if (action == FingerAction.Place)
        //        "Place fingerprint in sensor...".Info();
        //    else
        //        "Remove fingerprint from sensor...".Info();

        //    while (true)
        //    {
        //        var result = Write(Command.IsPressFinger);

        //        if (result.HasError)
        //            throw new Exception(result.Error.ToString());

        //        if ((action == FingerAction.Place && result.Parameter == 0) ||
        //            (action == FingerAction.Remove && result.Parameter != 0))
        //            break;

        //        Thread.Sleep(50);
        //    }
        //}

        //private static int GetEnrollingCount()
        //{
        //    var result = Write(Command.GetEnrollCount);

        //    if (result.HasError)
        //        throw new Exception(result.Error.ToString());

        //    $"Enrolled users = {result.Parameter}".Info();
        //    return result.Parameter;
        //}

        //private static ResponsePacket Write(Command command, int parameter = 0)
        //{
        //    var header = new byte[] { 0x55, 0xAA, 0x01, 0x00 };
        //    var cmd = BitConverter.GetBytes((UInt16)command);
        //    var param = BitConverter.GetBytes(parameter);

        //    var crc = BitConverter.GetBytes(
        //                Convert.ToUInt16(
        //                        header.Sum(d => Convert.ToUInt32(d)) +
        //                        cmd.Sum(d => Convert.ToUInt32(d)) +
        //                        param.Sum(d => Convert.ToUInt32(d))));

        //    if (!BitConverter.IsLittleEndian)
        //    {
        //        Array.Reverse(cmd);
        //        Array.Reverse(param);
        //        Array.Reverse(crc);
        //    }
        //    var data = header.Concat(param).Concat(cmd).Concat(crc).ToArray();

        //    //"Writting data...".Info();
        //    //String.Join(" ", data.Select(d => d.ToString("X2"))).Info();
        //    port.Write(data, 0, data.Length);
        //    port.BaseStream.Flush();

        //    return ReadData();
        //}

        //private static ResponsePacket ReadData()
        //{
        //    var data = new List<byte>();
        //    var readed = new byte[1024];
        //    var timeout = TimeSpan.FromSeconds(2);
        //    var startTime = DateTime.Now;
        //    while (data.Count == 0 || port.BytesToRead > 0)
        //    {
        //        if (port.BytesToRead > 0)
        //        {
        //            var count = port.Read(readed, 0, readed.Length);
        //            if (count > 0)
        //            {
        //                data.AddRange(readed.Take(count));
        //            }
        //        }

        //        if (DateTime.Now.Subtract(startTime) > timeout)
        //        {
        //            return null;
        //        }
        //        Thread.Sleep(10);
        //    }

        //    $"Response: {String.Join(" ", data.Select(d => d.ToString("X2")))}".Info();

        //    return ResponsePacket.FromByteArray(data.ToArray());
        //}
    }
}
