using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;

namespace SwrveUnity
{
/// <summary>
/// Used internally to hold QaUser info about campaigns.
/// </summary>
public class SwrveQaUserCampaignInfo
{

    public long id;
    public long variantId;
    public string type;
    public bool displayed;
    public string reason;

    public SwrveQaUserCampaignInfo(long id, long variantId, string type, bool displayed, String reason = "")
    {
        this.id = id;
        this.variantId = variantId;
        this.type = type;
        this.displayed = displayed;
        this.reason = reason;
    }
}
}
