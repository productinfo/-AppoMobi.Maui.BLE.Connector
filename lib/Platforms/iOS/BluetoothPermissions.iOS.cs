using CoreLocation;

namespace AppoMobi.Maui.BLE.Connector;


public partial class BluetoothPermissions : Permissions.BasePlatformPermission
{



	public static bool CheckGpsIsAvailable()
	{
		bool status = CLLocationManager.LocationServicesEnabled;

		return status;

	}


}