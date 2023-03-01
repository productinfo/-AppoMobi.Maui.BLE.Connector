using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppoMobi.Maui.BLE.Connector
{
	public class BleCharacteristicViewModel : INotifyPropertyChanged
	{

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

		private string _Id;
		public string Id
		{
			get { return _Id; }
			set
			{
				if (_Id != value)
				{
					_Id = value;
					OnPropertyChanged();
				}
			}
		}

		private string _title;
		public string Title
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value;
					OnPropertyChanged();
				}
			}
		}

		private string _Description;
		public string Description
		{
			get { return _Description; }
			set
			{
				if (_Description != value)
				{
					_Description = value;
					OnPropertyChanged();
				}
			}
		}

		private string _lastData;
		public string LastData
		{
			get { return _lastData; }
			set
			{
				if (_lastData != value)
				{
					_lastData = value;
					OnPropertyChanged();
				}
			}
		}

		public Characteristic Info { get; set; }

	}
}