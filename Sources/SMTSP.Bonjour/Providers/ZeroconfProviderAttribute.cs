#region header

// Arkane.ZeroConf - ZeroconfProviderAttribute.cs
// 

#endregion

#region using

using System;

#endregion

namespace SMTSP.Bonjour.Providers ;

[AttributeUsage (AttributeTargets.Assembly)]
public class ZeroconfProviderAttribute : Attribute
{
    public ZeroconfProviderAttribute (Type providerType) => ProviderType = providerType ;

    public Type ProviderType { get ; }
}
