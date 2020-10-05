﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Util;
using Hoho.Android.UsbSerial;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Util;
using Java.Lang;
using MissionPlanner.ArduPilot;
using MissionPlanner.Comms;
using MissionPlanner.Utilities;
using Exception = System.Exception;
using String = System.String;

namespace Xamarin.Droid
{
    public class USBDevices: IUSBDevices
    {
        static readonly string TAG = "MP";


        public async Task<List<DeviceInfo>> GetDeviceInfoList()
        {
            var usbManager = (UsbManager)Application.Context.GetSystemService(Context.UsbService);

            foreach (var deviceListValue in usbManager.DeviceList.Values)
            {
                Log.Info(TAG,"GetDeviceInfoList "+ deviceListValue.DeviceName);
            }

            Log.Info(TAG,"GetDeviceInfoList "+ "Refreshing device list ...");

            var drivers = await AndroidSerialBase.GetPorts(usbManager);

            if (drivers == null || drivers.Count == 0)
            {
                Log.Info(TAG, "GetDeviceInfoList "+"No usb devices");
                return new List<DeviceInfo>();
            }

            List<DeviceInfo> ans = new List<DeviceInfo>();

            foreach (var driver in drivers.ToArray())
            {
                try
                {
                    Log.Info(TAG,
                        string.Format("GetDeviceInfoList "+"+ {0}: {1} port{2}", driver, drivers.Count,
                            drivers.Count == 1 ? string.Empty : "s"));

                    Log.Info(TAG,
                        string.Format("GetDeviceInfoList "+"+ {0}: {1} ", driver.Device.ProductName, driver.Device.ManufacturerName));

                    var deviceInfo = GetDeviceInfo(driver.Device);

                    ans.Add(deviceInfo);

                    await usbManager.RequestPermissionAsync(driver.Device, Application.Context);
                }
                catch (Exception e)
                {
                    Log.Error("MP", "GetDeviceInfoList "+e.StackTrace);
                }
            }

            return ans;
        }

        public void USBEventCallBack(object usbDeviceReceiver, object device)
        {
            USBEvent?.Invoke(usbDeviceReceiver, GetDeviceInfo(device));
        }

        public event EventHandler<DeviceInfo> USBEvent;

        /// <summary>
        /// UsbDevice to DeviceInfo
        /// </summary>
        /// <param name="devicein"></param>
        /// <returns></returns>
        public DeviceInfo GetDeviceInfo(object devicein)
        {
            var device = (devicein as UsbDevice);
            var deviceInfo = new DeviceInfo()
            {
                board = device.ProductName,
                description = device.ProductName,
                hardwareid = String.Format("USB\\VID_{0:X4}&PID_{1:X4}", device.VendorId, device.ProductId),
                name = device.DeviceName
            };
            return deviceInfo;
        }


        public async Task<ICommsSerial> GetUSB(DeviceInfo di)
        {
            var usbManager = (UsbManager) Application.Context.GetSystemService(Context.UsbService);
            
            foreach (var deviceListValue in usbManager.DeviceList.Values)
            {
                Log.Info(TAG,"GetUSB "+ deviceListValue.DeviceName);
            }

            Log.Info(TAG, "GetUSB "+"Refreshing device list ...");

            var drivers = await AndroidSerialBase.GetPorts(usbManager);

            if (drivers.Count == 0)
            {
                Log.Info(TAG, "GetUSB "+"No usb devices");
                return null;
            }

            foreach (var driver in drivers.ToArray())
            {
                Log.Info(TAG, string.Format("GetUSB "+"+ {0}: {1} ports {2}", driver, drivers.Count, driver.Ports.Count));

                Log.Info(TAG, string.Format("GetUSB "+"+ {0}: {1} ", driver.Device.ProductName, driver.Device.ManufacturerName));
            }

            var usbdevice = drivers.First(a =>
                di.hardwareid.Contains(a.Device.VendorId.ToString("X4")) &&
                di.hardwareid.Contains(a.Device.ProductId.ToString("X4")));

            var permissionGranted =
                await usbManager.RequestPermissionAsync(usbdevice.Device, Application.Context);
            if (permissionGranted)
            {
                var defaultport = drivers.First().Ports.First();
                if (drivers.First().Ports.Count > 1)
                {
                    ManualResetEvent mre = new ManualResetEvent(false);

                    var handler = new Handler(MainActivity.Current.MainLooper);

                    handler.Post(() =>
                    {
                        AlertDialog.Builder alert = new AlertDialog.Builder(MainActivity.Current);
                        alert.SetTitle("Multiple Ports");
                        alert.SetCancelable(false);
                        var items = drivers.First().Ports.Select(a =>
                                a.Device.GetInterface(a.PortNumber).Name ?? a.PortNumber.ToString())
                            .ToArray();
                        alert.SetSingleChoiceItems(items, 0, (sender, args) =>
                        {
                            defaultport = drivers.First().Ports[args.Which];
                        });

                        alert.SetNeutralButton("OK", (senderAlert, args) => { mre.Set(); });

                        Dialog dialog = alert.Create();
                        if(!MainActivity.Current.IsFinishing)
                            dialog.Show();
                    });

                    mre.WaitOne();
                }

                var portInfo = new UsbSerialPortInfo(defaultport);

                int vendorId = portInfo.VendorId;
                int deviceId = portInfo.DeviceId;
                int portNumber = portInfo.PortNumber;

                Log.Info(TAG, string.Format("GetUSB "+"VendorId: {0} DeviceId: {1} PortNumber: {2}", vendorId, deviceId, portNumber));

                var driver = drivers.Where((d) => d.Device.VendorId == vendorId && d.Device.DeviceId == deviceId).FirstOrDefault();
                var port = driver.Ports[portNumber];

                var serialIoManager = new SerialInputOutputManager(usbManager, port);

                return new AndroidSerial(serialIoManager) {PortName = usbdevice.Device.ProductName};
            }

            return null;
        }
    }
}