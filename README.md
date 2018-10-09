[![Build status](https://ci.appveyor.com/api/projects/status/61tiduyk2eo8g7r9/branch/master?svg=true)](https://ci.appveyor.com/project/geoperez/sparkfunfingerprint/branch/master)
[![Analytics](https://ga-beacon.appspot.com/UA-8535255-2/unosquare/wsfingerprint/)](https://github.com/igrigorik/ga-beacon)

# ![Fingerprint](https://github.com/unosquare/sparkfunfingerprint/raw/master/logos/sffp-logo-32.png "Unosquare SparkFun Fingerprint Reader") SparkFun Fingerprint Reader (GT-521Fxx)

*:star: Please star this project if you find it useful!*

Interfacing Library for .NET 4.5 (and Mono) and .NET Core!.

* [Product Page](https://www.sparkfun.com/products/14585)
* [Data sheet](https://cdn.sparkfun.com/assets/learn_tutorials/7/2/3/GT-521FX2_datasheet_V1.1__003_.pdf)
* [Reference Manual](https://cdn.sparkfun.com/assets/learn_tutorials/7/2/3/GT-521F52_Programming_guide_V10_20161001.pdf)
* [Wiki](https://learn.sparkfun.com/tutorials/fingerprint-scanner-gt-521fxx-hookup-guide)

![GT-521Fxx](https://github.com/unosquare/sparkfunfingerprint/raw/master/logos/sffp-image.jpg "GT-521Fxx")

## Specifications

| Technical Specs              | GT-521F32 / GT-521F52                               |
| ---------------------------- | --------------------------------------------------- |
| CPU                          | ARM Cortex M3 Cortex                                |
| Sensor                       | optical                                             |
| Window                       | 16.9mm x 12.9mm                                     |
| Effective Area of the Sensor | 14mm x 12.5mm                                       | 
| Image Size                   | 258x202 px                                          |
| Resolution                   | 450 dpi                                             |
| Max # of Fingerprints        | 200 / 3000				                             |
| Matching Mode                | 1:1, 1:N                                            |
| Size of Template             | 496 Bytes(template) + 2 Bytes (checksum)            |
| Serial Communication         | UART (Default: 9600 baud) and USB v2.0 (Full Speed) |
| False Acceptance Rate (FAR)  | < 0.001%                                            |
| False Rejection Rate (FRR)   | < 0.01%                                             |
| Enrollment Time              | < 3 sec (3 fingerprints)                            |
| Identification Time          | < 1.5                                               |
| Operating Voltage            | 3.3V ~ 6Vdc                                         |
| Operating Current            | < 130mA                                             |
| Touch Operating Voltage      | 3.3Vdc                                              |
| Touch Operating Current      | < 3mA                                               |
| Touch Standby Current        | < μ5                                                |

## Library Features
* All documented commands are implemented (2018-06-25)
* Operations are all asynchronous
* Nice sample application included for testing
* MIT License
* .Net Framework (and Mono)
  * No dependencies
* .Net Standard
  * [SerialPortStream](https://github.com/jcurl/serialportstream): Independent implementation of System.IO.Ports.SerialPort and SerialStream for portability.

## NuGet Installation: [![NuGet version](https://badge.fury.io/nu/Unosquare.Sparkfun.FingerprintReader.svg)](https://badge.fury.io/nu/Unosquare.Sparkfun.FingerprintReader)

```
PM> Install-Package Unosquare.Sparkfun.FingerprintReader
```

## Usage

```csharp
using (var reader = new FingerprintReader(FingerprintReaderModel.GT521F52))
    {
         await reader.Open("COM4");
         Console.WriteLine($"Serial Number: {reader.SerialNumber}");
         Console.WriteLine($"Firmware Version: {reader.FirmwareVersion}");
    }
```

## Related fingerprint projects

| Project | Description |
|--------| ---|
|[wsfingerprint](https://github.com/unosquare/wsfingerprint)|WaveShare Fingerprint Reader - Interfacing Library for .NET |
|[libfprint-cs](https://github.com/unosquare/libfprint-cs)|The long-awaited C# (.NET/Mono) wrapper for the great fprint library|
