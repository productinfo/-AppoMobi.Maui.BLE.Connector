namespace AppoMobi.Maui.BLE.Connector;


public partial class BluetoothPermissions : Permissions.BasePlatformPermission
{
	private readonly bool _scan;
	private readonly bool _advertise;
	private readonly bool _connect;
	private readonly bool _bluetoothLocation;




	public static bool CheckGpsIsAvailable()
	{
		bool value = false;

		return value;
	}


}