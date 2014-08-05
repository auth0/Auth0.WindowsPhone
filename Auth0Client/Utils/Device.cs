using System;
#if WINDOWS_PHONE
using Microsoft.Phone.Info;
#else
using Windows.Networking.Proximity;
#endif

namespace Auth0.SDK
{
    public class Device
    {
        private static string id;

        public static string GetUniqueId()
        {
            if (string.IsNullOrEmpty(id))
            {
#if WINDOWS_PHONE
                byte[] uniqueId = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
                id = string.Format("{0}-{1}-{2}", DeviceStatus.DeviceManufacturer, DeviceStatus.DeviceName,
                    BitConverter.ToString(uniqueId).Replace("-", ""));
#else
                var token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
                var hardwareId = token.Id;
                byte[] bytes = new byte[hardwareId.Length];
                using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId))
                {    
                    dataReader.ReadBytes(bytes);
                    id = string.Format("{0}-{1}", PeerFinder.DisplayName, BitConverter.ToString(bytes).Replace("-",""));
                }
#endif
            }

            return id;
        }
    }
}
