#pragma warning disable CS1591

namespace SMTSP.Entities;

public enum MessageTypes
{
    Unknown,
    TransferRequest,
    DeviceInfo,
    DeviceLookupRequest,
    RemoveDeviceFromDiscovery
}