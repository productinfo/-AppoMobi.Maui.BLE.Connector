# AppoMobi.Maui.BLE.Connector

Wrapper for AppoMobi.Maui.BLE (https://github.com/taublast/-AppoMobi.Maui.BLE)
Remains in -PRE state

## Roadmap:

- Add example
- Test and fix Windows platform permissions


## Quick Start
The following will add a DI for `IBluetoothLE`

```csharp
        builder
            .UseBlootoothLE()
```

Create your simple connector by subclassing the provided connector:

```csharp

[Preserve(AllMembers = true)]
public class MyConnector : BLEConnector
{

    public MyConnector(IBluetoothLE ble) : base(ble)
    {
    }

    /// <summary>
    /// Let's say you want to connect to your brand device that has this service id.
    /// </summary>
    public static Guid MyKnownDeviceService { get; } = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb");

    /// <summary>
    /// For your brand device
    /// </summary>
    public static string ServiceId { get; set; } = "0000ffe0-0000-1000-8000-00805f9b34fb";

    /// <summary>
    /// For your brand device
    /// </summary>
    public static string CharacteristicId { get; set; } = "0000ffe1-0000-1000-8000-00805f9b34fb";

    #region EXAMPLE WRITE

    public readonly string CommandReadDeviceSettings = "CONF_READ";

    public async Task<bool> RequestMyDeviceSettings()
    {
        if (WriteTo != null)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(CommandReadDeviceSettings);
            await WriteTo.Info.WriteAsync(bytes);

            return true;
        }

        return false;
    }

    #endregion

    public event EventHandler<bool> DeviceConnectionChanged;

    protected override async void OnConnectionChanged()
    {

        base.OnConnectionChanged();

        Debug.WriteLine($"DEVICE connected: {IsConnected}");

        if (!IsConnected)
        {
            StopMonitoring(_subscribed);
            WriteTo = null;
            ConnectedDevice = null;

            DeviceConnectionChanged?.Invoke(this, false);
        }


    }



    /// <summary>
    /// Uses Serial property
    /// </summary>
    /// <param name="needThrow"></param>
    /// <returns></returns>
    public async Task ConnectToSerialDevice(bool needThrow = false)
    {

#if ANDROID || IOS || MACCATALYST || WINDOWS

        //connect
        try
        {
            ConnectionState = ConnectionState.Connecting;

            StopScanning();

            var device = await ConnectDevice(Serial);
            if (device == null)
            {
                throw new Exception($"{Serial} not found");
            }

            LastName = device.Device.Name;
            LastSerial = device.Id;
            ConnectionState = ConnectionState.Connected;

            var service = device.Services.FirstOrDefault(x => x.Id.Equals(ServiceId, StringComparison.InvariantCultureIgnoreCase));
            if (service == null)
            {
                //service not found
                throw new Exception($"Service {ServiceId} not found");
            }

            var read = service.Characteristics.FirstOrDefault(x => x.Id.Equals(CharacteristicId, StringComparison.InvariantCultureIgnoreCase));
            if (read == null)
            {
                //characteristic not found
                throw new Exception($"Characteristic {CharacteristicId} not found");
            }

            //Our brand device uses same characteristic for read/write
            if (read.Info.CanWrite)
                WriteTo = read; //the actual case
            else
            {
                var write = service.Characteristics.Where(c => c.Info.CanWrite).ToArray();
                foreach (var writable in write)
                {
                    Trace.WriteLine($"[W] {writable.Id} {writable.Info.Id} {writable.Info.Name} {writable.Info.Uuid}");
                }
                //just in case..
                WriteTo = write.FirstOrDefault();
            }

            if (ConnectedDevice == null)
                throw new Exception("unknown error");

            await StartMonitoring(read, true);

            DeviceConnectionChanged?.Invoke(this, true);
        }
        catch (Exception e)
        {
            WriteTo = null;
            ConnectedDevice = null;

            Console.WriteLine(e);
            ConnectionState = ConnectionState.Error;
            DeviceConnectionChanged?.Invoke(this, false);
            if (needThrow)
            {
                throw e;
            }
        }
#else

        throw new NotImplementedException();

#endif

    }

    BleCharacteristicViewModel WriteTo
    {
        get => _writeTo;
        set => _writeTo = value;
    }


    #region PERMISSIONS

    /// <summary>
    /// Initialize SDK, parameters are for displaying permissions prompts
    /// </summary>
    /// <param name="mainpage">your maui app mainPage to attach permission propts to</param>
    /// <param name="appTitile">the title that will be displayed for permission prompts</param>
    public void Init(Page mainPage, string appTitile)
    {
        AppTitle = appTitile;
        MainPage = mainPage;
        Initialized = true;
    }

    public virtual bool CheckGpsIsAvailable()
    {
        return BluetoothPermissions.CheckGpsIsAvailable();
    }

    public virtual bool CheckBluetoothIsAvailable()
    {
        return Bluetooth.IsAvailable;
    }

    public virtual bool CheckBluetoothIsOn()
    {
        return Bluetooth.IsOn;
    }

    public virtual bool NativeCheckCanConnect()
    {
        return true; //we have no checks provided by native sdk like HasPermissions etc.
    }

    public virtual void OnCanConnect()
    {
        //normally could call some native code to update the internal sdk state etc
    }

    #region CHECK BLE

    protected string AppTitle;

    protected Page MainPage;
    public bool Initialized { get; protected set; }

    void ShowBluetoothOFFError()
    {
        Debug.WriteLine(ResStrings.AlertTurnOnBluetooth);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage.DisplayAlert(AppTitle, ResStrings.AlertTurnOnBluetooth, ResStrings.BtnOk);
        });
    }
    void ShowGPSPermissionsError()
    {
        Debug.WriteLine(ResStrings.AlertNeedGpsPermissionsForBluetooth);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage.DisplayAlert(AppTitle, ResStrings.AlertNeedGpsPermissionsForBluetooth, ResStrings.BtnOk);
        });
    }
    void ShowErrorGPSOff()
    {
        Debug.WriteLine(ResStrings.AlertNeedGpsOnForBluetooth);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage.DisplayAlert(AppTitle, ResStrings.AlertNeedGpsOnForBluetooth, ResStrings.BtnOk);
        });
    }

    void ShowBluetoothNotAvailableError()
    {
        Debug.WriteLine(ResStrings.AlertBluetoothUnsupported);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage.DisplayAlert(AppTitle, ResStrings.AlertBluetoothUnsupported, ResStrings.BtnOk);
        });
    }

    void ShowBluetoothPermissionsError()
    {
        Debug.WriteLine(ResStrings.AlertBluetoothPermissionsOff);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MainPage.DisplayAlert(AppTitle, ResStrings.AlertBluetoothPermissionsOff, ResStrings.BtnOk);
        });
    }

    public async Task<bool> CheckCanConnectDisplayErrors()
    {


#if ANDROID

        var status = await BluetoothPermissions.CheckBluetoothStatus();
        if (status != PermissionStatus.Granted)
        {
            Tasks.StartTimerAsync(TimeSpan.FromMilliseconds(150), async () =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {

                    if (BluetoothPermissions.NeedGPS)
                    {
                        await MainPage.DisplayAlert(AppTitle, ResStrings.AlertNeedLocationForBluetooth, ResStrings.BtnOk);
                    }

                    status = await BluetoothPermissions.RequestBluetoothAccess();
                    if (status == PermissionStatus.Granted)
                    {
                        //disabled, using android:usesPermissionFlags="neverForLocation"

                        if (BluetoothPermissions.NeedGPS)
                        {
                            if (!CheckGpsIsAvailable())
                            {
                                ShowErrorGPSOff();
                                return;
                            }
                        }

                        await CheckCanConnectDisplayErrors();
                    }
                    else
                    {
                        ShowBluetoothPermissionsError();
                    }
                });
                return false;
            });

            return false;
        }

        if (BluetoothPermissions.NeedGPS)
        {
            if (!CheckGpsIsAvailable())
            {
                ShowErrorGPSOff();
                return false;
            }
        }



#else
        //Not android

        if (DeviceInfo.DeviceType == DeviceType.Virtual) // simulator
            return true;

#if (IOS || MACCATALYST)

        var create = CheckBluetoothIsAvailable(); //for to show permissions prompt
        if (!NativeCheckCanConnect())
        {
            return false;
        }
        else
        {
            await Task.Delay(200); // loads ble status (on or off)
            OnCanConnect();
        }

#else

        // WINDOWS?..


#endif


#endif


        if (CheckBluetoothIsAvailable())
        {
            if (CheckBluetoothIsOn())
            {
                return true;
            }

            ShowBluetoothOFFError();
            return false;

        }
        else
        {
            ShowBluetoothNotAvailableError();
            return false;
        }


        return true;
    }

    #endregion


    #endregion


    public event EventHandler<RaceBoxState> OnDecoded;
    public event EventHandler<RaceBoxExtendedState> OnDecodedExtended;
    public event EventHandler<RaceBoxSettingsState> OnDecodedSettings;

    private string _LastSerial;
    public string LastSerial
    {
        get { return _LastSerial; }
        set
        {
            if (_LastSerial != value)
            {
                _LastSerial = value;
                OnPropertyChanged();
            }
        }
    }

    private string _LastName;
    public string LastName
    {
        get { return _LastName; }
        set
        {
            if (_LastName != value)
            {
                _LastName = value;
                OnPropertyChanged();
            }
        }
    }


    public void StopScanning()
    {
        if (CancelScan != null)
        {
            CancelScan.Cancel();
        }
    }

    protected CancellationTokenSource CancelScan { get; set; }


    public async Task<bool> ScanForCompatibleDevices()
    {
        if (IsBusy)
        {
            throw new Exception("Already connecting");
        }

        if (await CheckCanConnectDisplayErrors())
        {

            try
            {
                CancelScan = new();

                FilterServiceUuids.Clear();

                await
                    WithScanTimeout(8000)
                        .WithServiceUuid(MyKnownDeviceService).ScanAsync(CancelScan.Token);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        return false;

    }


    private string _Serial = "";

    /// <summary>
    /// Device UID 
    /// </summary>
    public string Serial
    {
        get
        {
            return _Serial;
        }
        set
        {
            if (_Serial != value)
            {
                if (value != null)
                {
                    value = value.Trim();
                }
                _Serial = value;
                OnPropertyChanged();
                OnPropertyChanged("SerialIsValid");
            }
        }
    }

    public bool IsMonitoring
    {
        get
        {
            return _subscribed != null;
        }
    }

    private ConnectionState _ConnectionState;
    /// <summary>
    /// Статус Bluetooth соединения
    /// </summary>
    public ConnectionState ConnectionState
    {
        get { return _ConnectionState; }
        set
        {
            if (_ConnectionState != value)
            {
                _ConnectionState = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SerialIsValid
    {
        get
        {
            return Serial != null && Serial.Length == 36;
        }
    }

    public async Task FindDeviceAndConnect()
    {
        if (!SerialIsValid)
            throw new Exception("Bad serial");

        Preferences.Set("Serial", Serial);

        if (await CheckCanConnectDisplayErrors())
        {
            await ConnectToSerialDevice(true);
        }
    }


    private BleCharacteristicViewModel _subscribed;

    /// <summary>
    /// We subsribe to a READ characteristing and will callback called when value changes there
    /// </summary>
    /// <param name="characteristic"></param>
    /// <param name="needThrow"></param>
    /// <returns></returns>
    public async Task<bool> StartMonitoring(BleCharacteristicViewModel characteristic, bool needThrow = false)
    {
        if (_subscribed != null)
            StopMonitoring(_subscribed);

        try
        {
            characteristic.Info.ValueUpdated -= OnDataChanged;
            characteristic.Info.ValueUpdated += OnDataChanged;

            await characteristic.Info.StartUpdatesAsync();

            _subscribed = characteristic;

            SetStatus($"Monitoring on");

            //todo your upon connected device logic

            return true;
        }
        catch (Exception e)
        {
            _subscribed = null;

            Trace.WriteLine(e);

            SetStatus("Monitoring unsuppurted..");

            if (needThrow)
            {
                OnPropertyChanged("IsMonitoring");
                throw e;
            }
        }
        finally
        {
            OnPropertyChanged("IsMonitoring");
        }

        return false;
    }


    private string _LastSentCommand = "none";
    public string LastSentCommand
    {
        get { return _LastSentCommand; }
        set
        {
            if (_LastSentCommand != value)
            {
                _LastSentCommand = value;
                OnPropertyChanged();
            }
        }
    }

    private string _LastSentData;
    public string LastSentData
    {
        get { return _LastSentData; }
        set
        {
            if (_LastSentData != value)
            {
                _LastSentData = value;
                OnPropertyChanged();
            }
        }
    }

    protected bool ChannelBusy { get; set; }


    void ProcessDataReceived(byte[] bytes)
    {

        try
        {

            //todo your logic 

        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            ChannelBusy = false;
        }
    }

    public void StopMonitoring(BleCharacteristicViewModel characteristic)
    {

        _subscribed = null;

        try
        {
            characteristic.Info.ValueUpdated -= OnDataChanged;
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
        }

        SetStatus("Monitoring off");

        OnPropertyChanged("IsMonitoring");
    }

    public event EventHandler<byte[]> DataReceived;

    /// <summary>
    /// DEBUG only
    /// </summary>
    /// <param name="status"></param>
    [Conditional("DEBUG")]
    void SetStatus(string status)
    {
        Status = status;
#if WINDOWS
        Trace.WriteLine(status);
#else
        Console.WriteLine(status);
#endif
    }

    private string _Status;
    public string Status
    {
        get { return _Status; }
        set
        {
            if (_Status != value)
            {
                _Status = value;
                OnPropertyChanged();
                Debug.WriteLine($"Status: {value}");
            }
        }
    }

    private string _DataIn;
    private BleCharacteristicViewModel _writeTo;


    public string DataIn
    {
        get { return _DataIn; }
        set
        {
            if (_DataIn != value)
            {
                _DataIn = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnDataChanged(object sender, CharacteristicUpdatedEventArgs args)
    {
        int total = 0;
        try
        {

            var bytes = args.Characteristic.Value;

            total = bytes.Length;

            SetStatus($"Received {total} bytes");

            //Debug.WriteLine($"[BLE MONITOR] {Status}");

            if (total == 0)
                return;

            ProcessDataReceived(bytes);

            DataReceived?.Invoke(this, bytes);
        }
        catch (Exception e)
        {
            SetStatus("Read error");

            Console.WriteLine(e);
            DataIn = "";//$"Gor {total} vytes + Error: {e.ToString()}";
        }
    }

 
}

```

Inside your viewmodel, you can now use it to scan for compatible devices and to connect, for example (consider this a pseudo-code):

```csharp
  private SemaphoreSlim semaphoreConnector = new(1, 1);

  async Task Connect()
  {
      await semaphoreConnector.WaitAsync();

      try
      {
          if (!IsBusy && !Connector.IsBusy)
          {
              IsBusy = true;

              LastDeviceId = _preferences.Get("LastDevice", string.Empty);

              if (string.IsNullOrEmpty(LastDeviceId)) //todo get from prefs last uid
              {
                  //need find available devices

                  var ok = await Connector.ScanForCompatibleDevices();

                  if (!ok)
                  {
                      throw new Exception("Scan failed");
                  }

                  var devices = Connector.FoundDevices.Where(x => x.Device.Name != null
                  && x.Device.Name.ToLower().Contains("brandname#")).DistinctBy(x => x.Device.Name).ToList();

                  if (devices.Any())
                  {
                      if (devices.Count > 1)
                      {
                          MainThread.BeginInvokeOnMainThread(async () =>
                          {
                              var options = devices.Select(x => new SelectableAction
                              {
                                  Action = () =>
                                  {
                                      Connector.Serial = x.Id;
                                  },
                                  Id = x.Id,
                                  Title = x.Device.Name
                              }).ToList();

                              var selected = await _ui.PresentSelection(options) as SelectableAction;
                              if (selected != null)
                              {
                                  try
                                  {
                                      selected?.Action?.Invoke();
                                      await ConnectWithSetup();
                                  }
                                  catch (Exception e)
                                  {
                                      Debug.WriteLine(e);
                                      LastDeviceId = null;
                                  }
                              }

                          });
                          return;
                      }

                      Connector.Serial = devices.First().Id;
                  }
                  else
                  {
                      MainThread.BeginInvokeOnMainThread(async () =>
                      {
                          await _ui.Alert(ResStrings.VendorTitle, ResStrings.CompatibleDevicesNotFound);
                      });

                      return;
                  }

              }
              else
              {
                  //try to connect to last device
                  Connector.Serial = LastDeviceId;
              }

              await Connector.ConnectToSerialDevice(true);
          }
      }
      catch (Exception ex)
      {
          Debug.WriteLine(ex);
          LastDeviceId = null;
      }
      finally
      {
          IsBusy = false;

          semaphoreConnector.Release();

          UpdateUi();
      }

  }

```


## Permissions

Connector contains code to request permissions. 
How to setup permissions for your projects:

Android manifest:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" xmlns:tools="http://schemas.android.com/tools"
          android:versionCode="1">
	<application
		android:allowBackup="true" android:supportsRtl="true"></application>
	<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
	<uses-permission android:name="android.permission.INTERNET" />

	<uses-feature android:name="android.hardware.bluetooth" android:required="false" tools:node="replace" />
	<uses-feature android:name="android.hardware.bluetooth_le" android:required="false" tools:node="replace" />

	<!-- Request legacy Bluetooth permissions on older devices. -->
	<uses-permission android:name="android.permission.BLUETOOTH"
	                 android:maxSdkVersion="30" />
	<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"
	                 android:maxSdkVersion="30" />

	<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION"
	                 android:maxSdkVersion="28" />

	<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION"
	                 android:minSdkVersion="29" android:maxSdkVersion="30" />


	<!--<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION"
	                 android:minSdkVersion="29" android:maxSdkVersion="30" />-->

	<!-- Needed only if your app looks for Bluetooth devices.
         If your app doesn't use Bluetooth scan results to derive physical
         location information, you can strongly assert that your app
         doesn't derive physical location. -->
	<!--WARNING: If you include neverForLocation in your android:usesPermissionFlags, 
		some BLE beacons are filtered from the scan results.-->
	<uses-permission android:name="android.permission.BLUETOOTH_SCAN"
	                 android:usesPermissionFlags="neverForLocation" />
	<!-- Needed only if your app communicates with already-paired Bluetooth
         devices. -->
	<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
	<!-- Needed only if your app makes the device discoverable to Bluetooth
         devices. -->
	<!--<uses-permission android:name="android.permission.BLUETOOTH_ADVERTISE" />-->


</manifest>

```

Windows `Package.appxmanifest`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Package
	xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
	xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
	xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
	xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
	IgnorableNamespaces="uap rescap">

  ...

	<Capabilities>
		<rescap:Capability Name="runFullTrust" />
		<DeviceCapability Name="bluetooth" />
	</Capabilities>

</Package>
```

Mac Catalyst `Info.plist`:

```
	<key>NSBluetoothAlwaysUsageDescription</key>
	<string>Bluetooth is required for this app to function properly</string>
```

iPhone `Info.plist`:
```
	<key>NSBluetoothPeripheralUsageDescription</key>
	<string>Bluetooth is required for this app to function properly</string>
	<key>NSBluetoothAlwaysUsageDescription</key>
	<string>Bluetooth is required for this app to function properly</string>
```
