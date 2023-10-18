#region header

// Arkane.ZeroConf - IService.cs
// 

#endregion

namespace SMTSP.Bonjour ;

public interface IService
{
    string Name { get ; }

    string RegType { get ; }

    string ReplyDomain { get ; }

    ITxtRecord TxtRecord { get ; set ; }
}
