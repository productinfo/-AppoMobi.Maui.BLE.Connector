namespace AppoMobi.Maui.BLE.Connector;
public partial class BluetoothPermissions : Permissions.BasePlatformPermission
{
	public static bool NeedGPS { get; protected set; }

	public static async Task<PermissionStatus> CheckBluetoothStatus()
	{
		try
		{
			var requestStatus = await new BluetoothPermissions().CheckStatusAsync();
			return requestStatus;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
			return PermissionStatus.Unknown;
		}
	}

	public static async Task<PermissionStatus> RequestBluetoothAccess()
	{
		try
		{
			var requestStatus = await new BluetoothPermissions().RequestAsync();
			return requestStatus;

			//#if ANDROID



			//#else
			//            var requestStatus = await new BluetoothPermissions().RequestAsync();
			//            return requestStatus;

			//#endif

		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
			return PermissionStatus.Unknown;
		}
	}

}