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
    // Using a class so we can have a enum of strings and use a
    public class SwrveCampaignType
    {
        private SwrveCampaignType(string value)
        {
            Value = value;
        }
        public string Value
        {
            get;
            set;
        }
        public static SwrveCampaignType Iam
        {
            get {
                return new SwrveCampaignType("iam");
            }
        }
        public static SwrveCampaignType Conversation
        {
            get {
                return new SwrveCampaignType("conversation");
            }
        }
        public static SwrveCampaignType Embedded
        {
            get {
                return new SwrveCampaignType("embedded");
            }
        }

        public override bool Equals( object obOther )
        {
            if (null == obOther)
                return false;
            if (this.GetType() != obOther.GetType())
                return false;
            return this.Value.Equals(((SwrveCampaignType)obOther).Value);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public long id;
    public long variantId;
    public SwrveCampaignType type;
    public bool displayed;
    public string reason;

    public SwrveQaUserCampaignInfo(long id, long variantId, SwrveCampaignType type, bool displayed, String reason = "")
    {
        this.id = id;
        this.variantId = variantId;
        this.type = type;
        this.displayed = displayed;
        this.reason = reason;
    }
}
}
