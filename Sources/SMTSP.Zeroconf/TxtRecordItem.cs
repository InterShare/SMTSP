#region header

// Arkane.ZeroConf - TxtRecordItem.cs
// 

#endregion

#region using

using System.Text ;

#endregion

namespace ArkaneSystems.Arkane.Zeroconf ;

public class TxtRecordItem
{
    private static readonly Encoding Encoding = new UTF8Encoding () ;

    public TxtRecordItem (string key, byte[] valueRaw)
    {
        Key      = key ;
        ValueRaw = valueRaw ;
    }

    public TxtRecordItem (string key, string valueString)
    {
        Key         = key ;
        ValueString = valueString ;
    }

    private string valueString ;

    public string Key { get ; }

    public byte[] ValueRaw { get ; set ; }

    public string ValueString
    {
        get
        {
            if (valueString != null)
                return valueString ;

            valueString = Encoding.GetString (ValueRaw) ;
            return valueString ;
        }
        set
        {
            valueString = value ;
            ValueRaw    = Encoding.GetBytes (value) ;
        }
    }

    public override string ToString () => string.Format ("{0} = {1}", Key, ValueString) ;
}
