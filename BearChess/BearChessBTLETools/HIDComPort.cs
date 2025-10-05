using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication;
using www.SoLaNoSoft.com.BearChess.HidDriver;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using Path = System.IO.Path;


namespace www.SoLaNoSoft.com.BearChessBTLETools
{
    public class HIDComPort : IComPort
    {
        public string PortName => "HID";
        public string DeviceName { get; }
        public string Baud => string.Empty;
        public int DeviceIndex { get; set; }

        private readonly DeviceHandling _device = null;


        private readonly ConcurrentQueue<byte[]> _byteArrayQueue = new ConcurrentQueue<byte[]>();
        private Thread _readingThread;
        private bool _stopReading;
        private bool _isOpen;
        private readonly ILogging _logger;

        public static IComPort GetComPort(ushort vendorId, ushort usagePage, string deviceName, ILogging logger)
        {
            var fPath = Configuration.Instance.DeviceIndexPath;
            var strings = Directory.GetFiles(fPath, "*.devIndex", SearchOption.TopDirectoryOnly);
            var ignoreIds = new List<int>();
            foreach (var s in strings)
            {
                var fi = new FileInfo(s);
                ignoreIds.Add(int.Parse(fi.Name.Split(".".ToCharArray())[0]));
            }
            var deviceHandling = new DeviceHandling(logger);
            var findDevice = deviceHandling.FindReadDevice(vendorId, deviceName, ignoreIds);
            if (findDevice && deviceHandling.FindWriteDevice(vendorId, deviceName, usagePage, ignoreIds))
            {
             
                var productName = deviceHandling.GetProduct();
                return new HIDComPort(deviceHandling, string.IsNullOrWhiteSpace(productName) ? deviceName : productName, logger, deviceHandling.DeviceId);
            }
            return null;
        }

        public HIDComPort(DeviceHandling device, string deviceName,  ILogging logger, int deviceIndex)
        {
            _isOpen = false;
            _device = device;
            DeviceName = deviceName;
            _logger = logger;
            DeviceIndex = deviceIndex;
        }


        private void ReadFromDevice()
        {
            while (!_stopReading)
            {
                try
                {
                    if (IsOpen)
                    {
                        var buffer = _device.Read();
                        if (buffer.Length > 0)
                        {
                         //  _logger.LogDebug($"HID: Reading {buffer.Length} bytes");
                           _byteArrayQueue.Enqueue(buffer);
                        }

                    }
                }
                catch (Exception ex )
                {
                    _logger.LogError("HID: ",ex);
                }
                Thread.Sleep(10);
            }
        }

        public void Open()
        {
            try
            {
                var fPath = Configuration.Instance.DeviceIndexPath;
                try
                {
                    File.WriteAllText(Path.Combine(fPath, $"{DeviceIndex}.devIndex"), DeviceName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Cannot create device index file {DeviceIndex}.devIndex",ex);
                }
                _isOpen = true;
                _stopReading = false;
                _readingThread = new Thread(ReadFromDevice) { IsBackground = true };
                _readingThread.Start();
            }
            catch
            {
                //
            }
        }

        public void Close()
        {
            var fPath = Configuration.Instance.DeviceIndexPath;
            try
            {
                File.Delete(Path.Combine(fPath, $"{DeviceIndex}.devIndex"));
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Cannot delete device index file {DeviceIndex}.devIndex", ex);
            }
            _stopReading = true;
            _isOpen = false;
        }

        public bool IsOpen => _device != null && _isOpen;
        public string ReadLine()
        {
            return string.Empty;
        }

        public int ReadByte()
        {
            return 0;
        }

        public byte[] ReadByteArray()
        {
            if (_byteArrayQueue.TryDequeue(out byte[] result))
            {
                return result;
            }

            return Array.Empty<byte>();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _device?.Write(buffer);
        }

        public void Write(string message)
        {

        }

        public void WriteLine(string command)
        {
            
        }

        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public void ClearBuffer()
        {
         //
        }

        public string ReadBattery()
        {
            return "---";
        }

        public bool RTS { get; set; }
        public bool DTR { get; set; }
    }
}
