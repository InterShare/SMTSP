namespace SMTSP.Entities;

public enum MessageTypes
{
    Unknown,
    TransferRequest,
    TransferRequestResponse,
    DataTransfer,
    DeviceInfo,
    DeviceLookupRequest
}