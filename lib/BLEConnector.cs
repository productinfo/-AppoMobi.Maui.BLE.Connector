using AppoMobi.Maui.BLE.Enums;
using AppoMobi.Maui.BLE.EventArgs;
using AppoMobi.Specials.Extensions;
using Microsoft.Maui.Controls.Internals;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AppoMobi.Maui.BLE.Connector
{
	[Preserve(AllMembers = true)]
	public class BLEConnector : IDisposable, INotifyPropertyChanged
	{
		void Test()
		{

		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			var changed = PropertyChanged;
			if (changed == null)
				return;

			changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		public IBluetoothLE Bluetooth { get; }

		public BLEConnector(IBluetoothLE ble)
		{

			Bluetooth = ble;

			Bluetooth.StateChanged += OnStateChanged;

			if (Bluetooth.Adapter != null)
			{
				Bluetooth.Adapter.ScanMode = ScanMode.Balanced;// for foreground
				Bluetooth.Adapter.DeviceConnected += OnDeviceConnected;
				Bluetooth.Adapter.DeviceDisconnected += OnDeviceDisconnected;
				Bluetooth.Adapter.DeviceConnectionLost += OnDeviceLost;
				Bluetooth.Adapter.DeviceDiscovered += OnDevicesDiscovered;

				ProcessState(new BluetoothStateChangedArgs(BluetoothState.Unknown, Bluetooth.State));
			}

		}

		public virtual void ProcessState(BluetoothStateChangedArgs args)
		{
			Debug.WriteLine($"[BLE] State: {args.NewState}");
			//if (args.NewState == BluetoothState.Off)
			//{
			//	App.Instance.PlaySoundFile("disconnected.mp3");
			//}
		}

		public virtual void Dispose()
		{
			Bluetooth.StateChanged -= OnStateChanged;

			if (Bluetooth.Adapter != null)
			{
				Bluetooth.Adapter.DeviceDiscovered -= OnDevicesDiscovered;
				Bluetooth.Adapter.DeviceConnected -= OnDeviceConnected;
				Bluetooth.Adapter.DeviceDisconnected -= OnDeviceDisconnected;
				Bluetooth.Adapter.DeviceConnectionLost -= OnDeviceLost;
			}
		}


		public event EventHandler<BluetoothStateChangedArgs> StateChanged;


		private void OnStateChanged(object sender, BluetoothStateChangedArgs args)
		{
			ProcessState(args);

			StateChanged?.Invoke(this, args);
		}

		private bool _IsConnected;
		public bool IsConnected
		{
			get { return _IsConnected; }
			set
			{
				if (_IsConnected != value)
				{
					_IsConnected = value;
					OnPropertyChanged();
					OnConnectionChanged();
				}
			}
		}

		protected virtual void OnConnectionChanged()
		{
			ConnectionChanged?.Invoke(this, System.EventArgs.Empty);
		}

		public event EventHandler ConnectionChanged;

		private void OnDeviceLost(object sender, DeviceErrorEventArgs e)
		{
			IsConnected = false;
			//   _ui.ShowToast("Подключение потеряно");
		}

		private void OnDeviceDisconnected(object sender, DeviceEventArgs e)
		{
			IsConnected = false;
			// _ui.ShowToast("Отключено");
		}

		protected virtual void OnDeviceConnected(object sender, DeviceEventArgs e)
		{
			IsConnected = true;
			//_ui.ShowToast("Подключено");
		}



		private bool _IsBusy;
		public bool IsBusy
		{
			get { return _IsBusy; }
			set
			{
				if (_IsBusy != value)
				{
					_IsBusy = value;
					OnPropertyChanged();
				}
			}
		}

		public BLEConnector WithScanTimeout(int ms)
		{
			Bluetooth.Adapter.ScanTimeout = ms;
			return this;
		}

		/// <summary>
		/// Pass to StartScanningForDevicesAsync
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		protected bool InternalFIlter(Device device)
		{
			if (FilterDeviceNames is { Count: > 0 })
			{
				return FilterDeviceNames.Contains(device.Name);
			}

			if (FilterDeviceUuids is { Count: > 0 })
			{
				return FilterDeviceUuids.Contains(device.Id);
			}

			return true;
		}




		protected List<String> FilterDeviceNames = new();
		protected List<Guid> FilterDeviceUuids = new();
		protected List<Guid> FilterServiceUuids = new();

		public BLEConnector WithDeviceUuid(Guid guid)
		{
			if (!FilterDeviceUuids.Contains(guid))
			{
				this.FilterDeviceUuids.Add(guid);
			}
			return this;
		}

		public BLEConnector WithDeviceUuids(IEnumerable<Guid> uuids)
		{
			foreach (var guid in uuids.ToList())
			{
				if (!FilterDeviceUuids.Contains(guid))
				{
					this.FilterDeviceUuids.Add(guid);
				}
			}
			return this;
		}

		public BLEConnector WithServiceUuid(Guid guid)
		{
			if (!FilterServiceUuids.Contains(guid))
			{
				this.FilterServiceUuids.Add(guid);
			}
			return this;
		}

		public BLEConnector WithServiceUuids(IEnumerable<Guid> uuids)
		{
			foreach (var guid in uuids.ToList())
			{
				if (!FilterServiceUuids.Contains(guid))
				{
					this.FilterServiceUuids.Add(guid);
				}
			}
			return this;
		}

		public BLEConnector WithDeviceName(string name)
		{
			if (!FilterDeviceNames.Contains(name))
			{
				this.FilterDeviceNames.Add(name);
			}
			return this;
		}

		public BLEConnector WithDeviceNames(IEnumerable<string> names)
		{
			foreach (var name in names.ToList())
			{
				if (!FilterDeviceNames.Contains(name))
				{
					this.FilterDeviceNames.Add(name);
				}
			}
			return this;
		}

		/// <summary>
		/// Android has an internal limit of 5 startScan(…) method calls every 30 seconds per app
		/// </summary>
		/// <returns></returns>
		public virtual async Task ScanAsync(CancellationToken cancel = default)
		{
			// Android has an internal limit of 5 startScan(…) method calls every 30 seconds per app

			//todo add 30 secs delay here for android

			if (IsBusy)
				return;

			IsBusy = true;

			try
			{

				if (Bluetooth.Adapter != null)
				{
					//todo check UI thread

					MainThread.BeginInvokeOnMainThread(async () =>
					{
						FoundDevices.Clear();
					});

					await Task.Delay(10);

					await Bluetooth.Adapter.StartScanningForDevicesAsync(FilterServiceUuids.ToArray(),
						InternalFIlter, false, cancel);

					while (Bluetooth.Adapter.IsScanning)
					{
						await Task.Delay(10, cancel);
					}

					await DetectConnected();
				}

			}
			catch (Exception e)
			{
				IsBusy = false;

				throw e;
			}
			finally
			{
				IsBusy = false;
			}

		}

		public ObservableCollection<BleDeviceViewModel> FoundDevices { get; } = new();

		public BleDeviceViewModel ConnectedDevice { get; set; }

		public BleCharacteristicViewModel Selected { get; set; }


		public BleDeviceViewModel ImportDevice(Device device)
		{
			if (device != null)
			{
				var item = new BleDeviceViewModel
				{
					Id = device.Id.ToString(),
					Title = device.NameOrId,
					Description = $"UUID: {device.Id}",
					Device = device,
					Rssi = device.Rssi
				};

				Debug.WriteLine($"[Connector] Found device ({FoundDevices.Count + 1}) {item.Title}");

				var existing = FoundDevices.FirstOrDefault(x => x.Id == item.Id);
				if (existing == null)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						// Update the UI
						FoundDevices.Add(item);
					});
					return item;
				}
				else
				{
					Reflection.MapProps(item, existing);
					return existing;
				}

			}

			return null;
		}
		private void OnDevicesDiscovered(object sender, DeviceEventArgs e)
		{
			ImportDevice(e.Device);
		}

		public async Task Disconnect()
		{

			if (ConnectedDevice != null)
			{
				foreach (var service in ConnectedDevice.Services)
				{
					service.Characteristics.Clear();
				}
				ConnectedDevice.Services.Clear();

				await Bluetooth.Adapter.DisconnectDeviceAsync(ConnectedDevice.Device);

				ConnectedDevice.State = ConnectionState.Disconnected;

			}

			ConnectedDevice = null;
			Selected = null;
			IsConnected = false;
		}

		/// <summary>
		/// Check what found is connected and sets up ConnectedDevice prop.
		/// </summary>
		/// <returns></returns>
		public async Task DetectConnected()
		{
			foreach (var device in FoundDevices)
			{
				if (device.State == ConnectionState.Connected)
				{
					if (device.Device.State != DeviceState.Connected)
					{
						device.State = ConnectionState.Disconnected;
					}
				}
				if (device.Device.State == DeviceState.Connected)
				{
					device.State = ConnectionState.Connected;

					ConnectedDevice = device;

					if (!ConnectedDevice.Services.Any())
					{
						await GetServices(ConnectedDevice);
					}
					return;
				}
			}
		}



		public async Task GetServices(BleDeviceViewModel existing)
		{

			//get more info:
			var services = await existing.Device.GetServicesAsync();

			foreach (var service in services)
			{
				var add = new BleServiceViewModel
				{
					Title = $"'{service.Name}', Основная: {service.IsPrimary}",
					Id = service.Id.ToString(),
					IsPrimary = service.IsPrimary,
					Device = service.Device
				};
				var characteristics = await service.GetCharacteristicsAsync();
				foreach (var line in characteristics)
				{
					add.Characteristics.Add(new BleCharacteristicViewModel
					{
						Title = line.Name,
						Id = line.Id.ToString(),
						Info = line
					});
				}
				existing.Services.Add(add);
			}
		}

		public async Task<byte[]> ReadCharacteristic(BleCharacteristicViewModel characteristic)
		{
			if (characteristic == null)
			{
				throw new Exception("[BLService] Read characteristic is null");
			}

			var bytes = await characteristic.Info.ReadAsync();

			return bytes;
		}

		public async Task<bool> WriteCharacteristic(BleCharacteristicViewModel characteristic, byte[] bytes)
		{
			if (characteristic == null)
			{
				throw new Exception("[BLService] Write characteristic is null");
			}

			if (!characteristic.Info.CanWrite)
				throw new InvalidOperationException("Characteristic does not support write.");
			//return false;

			await characteristic.Info.WriteAsync(bytes);

			return true;

		}

#if (ANDROID || IOS || MACCATALYST || WINDOWS || TIZEN)

		public async Task<BleDeviceViewModel> ConnectDevice(string Uid, int msDelay = 5000)
		{
			try
			{
				await Disconnect();
			}
			catch (Exception e)
			{
				//Console.WriteLine(e);
			}

			Device device = null;
			try
			{
				//the completion means nothing here... the callback will be called upon connection state change
				var cancel = new CancellationTokenSource();
				cancel.CancelAfter(msDelay);

				device = await Bluetooth.Adapter.ConnectToKnownDeviceAsync(Guid.Parse(Uid), default, cancel.Token, true);

				//await Task.Delay(msDelay, cancel.Token);

			}
			catch (Exception e)
			{
				System.Diagnostics.Trace.WriteLine(e);
			}

			if (device == null || device.State != DeviceState.Connected)
			{
				return null;
			}

			try
			{

				var existing = ImportDevice(device);

				existing.State = ConnectionState.Connected;

				await GetServices(existing);

				ConnectedDevice = existing;

				return ConnectedDevice;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}

#endif



	}
}
