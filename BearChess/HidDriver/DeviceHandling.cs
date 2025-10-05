using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChess.HidDriver
{
    public class DeviceHandling
    {
        private readonly ILogging _logger;
        private int _readDataIDevice = -1;
        private int _writeDataIDevice = -1;

        public int DeviceId => _readDataIDevice == -1 ? _writeDataIDevice : _readDataIDevice;

        public DeviceHandling(ILogging logger)
        {
            _logger = logger;
        }

        public void Write(byte[] data)
        {
            if (!Definitions.HIDWriteData.State)
            {
                return;
            }

            var hidReport = new byte[Definitions.HIDWriteData.Device[_writeDataIDevice].Caps.OutputReportByteLength];
            var varA = 0U;
            bool send = false;
            int j = -1;
            for (var i = 0; i < data.Length; i++)
            {
                j++;
                if (j >= hidReport.Length)
                {
                    j = 0;
                    send = true;
                    Definitions.WriteFile(Definitions.HIDWriteData.Device[_writeDataIDevice].Pointer, hidReport, (uint)hidReport.Length, ref varA, IntPtr.Zero);
                }
                //if (j < hidReport.Length)
                {
                    send = false;
                    hidReport[j] = data[i];
                }
            }

            if (!send)
            {
                Definitions.WriteFile(Definitions.HIDWriteData.Device[_writeDataIDevice].Pointer,
                    hidReport, (uint)hidReport.Length, ref varA, IntPtr.Zero);
            }

        }

        public byte[] Read()
        {
            var result = new List<byte>();
            if (!Definitions.HIDReadData.State)
            {
                return result.ToArray();
            }
            var hidReport = new byte[Definitions.HIDReadData.Device[_readDataIDevice].Caps.InputReportByteLength];

            if (hidReport.Length > 0)
            {
                var varA = 0U;
                Definitions.ReadFile(Definitions.HIDReadData.Device[_readDataIDevice].Pointer, hidReport, Definitions.HIDReadData.Device[Definitions.HIDReadData.iDevice].Caps.InputReportByteLength, ref varA, IntPtr.Zero);


                for (var index = 0; index < Definitions.HIDReadData.Device[_readDataIDevice].Caps.InputReportByteLength; index++)
                {
                    result.Add(hidReport[index]);

                }
            }
            return result.ToArray();
        }

        public string GetManufacturer()
        {
            if (!Definitions.HIDReadData.State)
            {
                return string.Empty;
            }

            return Definitions.HIDReadData.Device[Definitions.HIDReadData.iDevice].Manufacturer;
        }

        public string GetProduct()
        {
            if (!Definitions.HIDReadData.State)
            {
                return string.Empty;
            }

            try
            {
                return Definitions.HIDReadData.Device[Definitions.HIDReadData.iDevice].Product;
            }
            catch
            {
                return string.Empty;
            }
        }

        public int GetSerialNumber()
        {
            if (!Definitions.HIDReadData.State)
            {
                return 0;
            }

            return Definitions.HIDReadData.Device[Definitions.HIDReadData.iDevice].SerialNumber;
        }
        public int GetVersionNumber()
        {
            if (!Definitions.HIDReadData.State)
            {
                return 0;
            }

            return Definitions.HIDReadData.Device[Definitions.HIDReadData.iDevice].VersionNumber;
        }

        public int GetNumberOfDevices()
        {
            var hidGuid = new Guid();
            var deviceInfoData = new Definitions.SP_DEVICE_INTERFACE_DATA();

            Definitions.HidD_GetHidGuid(ref hidGuid);

            //
            // Open a handle to the plug and play dev node.
            //
            Definitions.SetupDiDestroyDeviceInfoList(Definitions.HardwareDeviceInfo);
            Definitions.HardwareDeviceInfo = Definitions.SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, Definitions.DIGCF_PRESENT | Definitions.DIGCF_DEVICEINTERFACE);
            deviceInfoData.cbSize = Marshal.SizeOf(typeof(Definitions.SP_DEVICE_INTERFACE_DATA));

            var Index = 0;
            while (Definitions.SetupDiEnumDeviceInterfaces(Definitions.HardwareDeviceInfo, IntPtr.Zero, ref hidGuid, Index, ref deviceInfoData))
            {
                Index++;
            }

            return Index;
        }

        public bool FindReadDevice(ushort vendorId, string productName, List<int> ignoreIds, ushort productId = (ushort)0)
        {
            Definitions.HIDReadData.State = false;
            if (vendorId == 0)
            {
                return Definitions.HIDReadData.State;
            }
            _logger?.LogDebug($"HID: FindReadDevice for vendor id {vendorId}");
            var numberOfDevices = GetNumberOfDevices();
            _logger?.LogDebug($"HID: Number of devices: {numberOfDevices}");
            Definitions.HIDReadData.Device = new Definitions.HID_DEVICE[numberOfDevices];
            FindKnownHIDDevices(ref Definitions.HIDReadData.Device);
            for (var index = 0; index < numberOfDevices; index++)
            {
                _logger?.LogDebug($"HID: {index}. vendor id: {Definitions.HIDReadData.Device[index].Attributes.VendorID}");
                _logger?.LogDebug($"HID: {index}. manufacturer: {Definitions.HIDReadData.Device[index].Manufacturer}");
                _logger?.LogDebug($"HID: {index}. product: {Definitions.HIDReadData.Device[index].Product}");
                _logger?.LogDebug($"HID: {index}. serial number: {Definitions.HIDReadData.Device[index].SerialNumber}");
                _logger?.LogDebug($"HID: {index}. version number: {Definitions.HIDReadData.Device[index].VersionNumber}");
                _logger?.LogDebug($"HID: {index}. OpenedForRead: {Definitions.HIDReadData.Device[index].OpenedForRead}");
                _logger?.LogDebug($"HID: {index}. OpenedForWrite: {Definitions.HIDReadData.Device[index].OpenedForWrite}");
            }

            for (var index = 0; index < numberOfDevices; index++)
            {
                _logger?.LogDebug($"HID: {index}. vendor id: {Definitions.HIDReadData.Device[index].Attributes.VendorID}");
                _logger?.LogDebug($"HID: {index}. manufacturer: {Definitions.HIDReadData.Device[index].Manufacturer}");
                _logger?.LogDebug($"HID: {index}. product: {Definitions.HIDReadData.Device[index].Product}");
                if (ignoreIds.Contains(index))
                {
                    _logger?.LogDebug($"HID: {index} part of ignore list");
                    continue;
                }
                if (Definitions.HIDReadData.Device[index].Attributes.VendorID == vendorId)
                {
                    if ( (productId==0 || Definitions.HIDReadData.Device[index].Attributes.ProductID == productId)
                        && (string.IsNullOrWhiteSpace(Definitions.HIDReadData.Device[index].Product) 
                            || Definitions.HIDReadData.Device[index].Product.StartsWith(productName)))
                    {
                        Definitions.HIDReadData.ProductID = Definitions.HIDReadData.Device[index].Attributes.ProductID;
                        Definitions.HIDReadData.VendorID = vendorId;
                        Definitions.HIDReadData.iDevice = index;
                        Definitions.HIDReadData.State = true;
                        _readDataIDevice = index;
                        break;
                    }
                }

            }

            return Definitions.HIDReadData.State;
        }

        public bool FindWriteDevice(ushort vendorId, string productName, ushort usagePage, List<int> ignoreIds)
        {
            Definitions.HIDWriteData.State = false;
            if (vendorId == 0 || usagePage == 0)
            {
                return Definitions.HIDWriteData.State;
            }
            _logger?.LogDebug($"HID: FindWriteDevice for vendor id {vendorId} and usage page {usagePage}");
            var numberOfDevices = GetNumberOfDevices();
            _logger?.LogDebug($"HID: Number of devices: {numberOfDevices}");
            Definitions.HIDWriteData.Device = new Definitions.HID_DEVICE[numberOfDevices];
            FindKnownHIDDevices(ref Definitions.HIDWriteData.Device);
            for (var index = 0; index < numberOfDevices; index++)
            {
                _logger?.LogDebug($"HID: {index}. vendor id: {Definitions.HIDWriteData.Device[index].Attributes.VendorID} usagePage: {Definitions.HIDWriteData.Device[index].Caps.UsagePage} ");
                _logger?.LogDebug($"HID: {index}. manufacturer: {Definitions.HIDWriteData.Device[index].Manufacturer}");
                _logger?.LogDebug($"HID: {index}. product: {Definitions.HIDWriteData.Device[index].Product}");
                if (ignoreIds.Contains(index))
                {
                    _logger?.LogDebug($"HID: {index} part of ignore list");
                    continue;
                }
                if ((Definitions.HIDWriteData.Device[index].Attributes.VendorID == vendorId) &&
                    (Definitions.HIDWriteData.Device[index].Attributes.ProductID == Definitions.HIDReadData.ProductID) &&
                    (Definitions.HIDWriteData.Device[index].Caps.UsagePage == usagePage) &&
                    (string.IsNullOrWhiteSpace(Definitions.HIDReadData.Device[index].Product)
                     || Definitions.HIDReadData.Device[index].Product.StartsWith(productName)))
                {
                    Definitions.HIDWriteData.ProductID = Definitions.HIDReadData.ProductID;
                    Definitions.HIDWriteData.VendorID = vendorId;
                    Definitions.HIDWriteData.iDevice = index;
                    Definitions.HIDWriteData.State = true;
                    _writeDataIDevice = index;
                    break;
                }

            }

            return Definitions.HIDWriteData.State;
        }

        private int FindKnownHIDDevices(ref Definitions.HID_DEVICE[] HID_Devices)
        {
            var hidGuid = new Guid();
            var deviceInfoData = new Definitions.SP_DEVICE_INTERFACE_DATA();
            var functionClassDeviceData = new Definitions.SP_DEVICE_INTERFACE_DETAIL_DATA();

            Definitions.HidD_GetHidGuid(ref hidGuid);

            //
            // Open a handle to the plug and play dev node.
            //
            Definitions.SetupDiDestroyDeviceInfoList( Definitions.HardwareDeviceInfo);
            Definitions.HardwareDeviceInfo = Definitions.SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, Definitions.DIGCF_PRESENT | Definitions.DIGCF_DEVICEINTERFACE);
            deviceInfoData.cbSize = Marshal.SizeOf(typeof(Definitions.SP_DEVICE_INTERFACE_DATA));

            var index = 0;
            while (Definitions.SetupDiEnumDeviceInterfaces(Definitions.HardwareDeviceInfo, IntPtr.Zero, ref hidGuid, index, ref deviceInfoData))
            {
                var requiredLength = 0;

                //
                // Allocate a function class device data structure to receive the
                // goods about this particular device.
                //
                Definitions.SetupDiGetDeviceInterfaceDetail(Definitions.HardwareDeviceInfo, ref deviceInfoData, IntPtr.Zero, 0, ref requiredLength, IntPtr.Zero);

                if (IntPtr.Size == 8)
                {
                    functionClassDeviceData.cbSize = 8;
                }
                else if (IntPtr.Size == 4)
                {
                    functionClassDeviceData.cbSize = 5;
                }

                //
                // Retrieve the information from Plug and Play.
                //
                Definitions.SetupDiGetDeviceInterfaceDetail(Definitions.HardwareDeviceInfo, ref deviceInfoData, ref functionClassDeviceData, requiredLength, ref requiredLength, IntPtr.Zero);

                //
                // Open device with just generic query abilities to begin with
                //
                OpenHIDDevice(functionClassDeviceData.DevicePath, ref HID_Devices, index);

                index++;
            }

            return index;
        }

        private void OpenHIDDevice(string devicePath, ref Definitions.HID_DEVICE[] hidDevice, int index)
        {
            hidDevice[index].DevicePath = devicePath;

            Definitions.CloseHandle(hidDevice[index].Pointer);
            hidDevice[index].Pointer = Definitions.CreateFile(hidDevice[index].DevicePath, Definitions.GENERIC_READ | Definitions.GENERIC_WRITE, Definitions.FILE_SHARE_READ | Definitions.FILE_SHARE_WRITE, 0, Definitions.OPEN_EXISTING, 0, IntPtr.Zero);
            hidDevice[index].Caps = new Definitions.HIDP_CAPS();
            hidDevice[index].Attributes = new Definitions.HIDD_ATTRIBUTES();

            Definitions.HidD_FreePreparsedData(ref hidDevice[index].Ppd);
            hidDevice[index].Ppd = IntPtr.Zero;

            Definitions.HidD_GetPreparsedData(hidDevice[index].Pointer, ref hidDevice[index].Ppd);
            Definitions.HidD_GetAttributes(hidDevice[index].Pointer, ref hidDevice[index].Attributes);
            Definitions.HidP_GetCaps(hidDevice[index].Ppd, ref hidDevice[index].Caps);

            var Buffer = Marshal.AllocHGlobal(126);
            {
                if (Definitions.HidD_GetManufacturerString(hidDevice[index].Pointer, Buffer, 126))
                {
                    hidDevice[index].Manufacturer = Marshal.PtrToStringAuto(Buffer);
                }
                if (Definitions.HidD_GetProductString(hidDevice[index].Pointer, Buffer, 126))
                {
                    hidDevice[index].Product = Marshal.PtrToStringAuto(Buffer);
                }
                if (Definitions.HidD_GetSerialNumberString(hidDevice[index].Pointer, Buffer, 126))
                {
                    int.TryParse(Marshal.PtrToStringAuto(Buffer), out hidDevice[index].SerialNumber);
                }
            }
            Marshal.FreeHGlobal(Buffer);
        }
    }
}
