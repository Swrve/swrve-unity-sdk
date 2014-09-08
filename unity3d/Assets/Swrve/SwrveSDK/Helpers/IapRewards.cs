/*
 * SWRVE CONFIDENTIAL
 * 
 * (c) Copyright 2010-2014 Swrve New Media, Inc. and its licensors.
 * All Rights Reserved.
 *
 * NOTICE: All information contained herein is and remains the property of Swrve
 * New Media, Inc or its licensors.  The intellectual property and technical
 * concepts contained herein are proprietary to Swrve New Media, Inc. or its
 * licensors and are protected by trade secret and/or copyright law.
 * Dissemination of this information or reproduction of this material is
 * strictly forbidden unless prior written permission is obtained from Swrve.
 */

using System.Collections.Generic;
using System;
using SwrveMiniJSON;
using Swrve.Helpers;

/// <summary>
/// Reperesents the rewards given to a user after a purchase.
/// </summary>
public class IapRewards
{
    protected Dictionary<string, Dictionary<string,string>> rewards;

    public IapRewards ()
    {
        this.rewards = new Dictionary<string, Dictionary<string, string>> ();
    }

    /// <summary>
    /// Create a currency reward
    /// </summary>
    /// <param name="currencyName">
    /// Name of the currency as specified on the dashboard
    /// </param>
    /// <param name="amount">
    /// Amount to be given
    /// </param>
    public IapRewards (string currencyName, long amount)
    {
        this.rewards = new Dictionary<string, Dictionary<string, string>> ();
        this.AddCurrency (currencyName, amount);
    }

    /// <summary>
    /// Add a resource reward
    /// </summary>
    /// <param name="resourceName">
    /// Name of the resource as specified on the dashboard
    /// </param>
    /// <param name="quantity">
    /// Quantity to be given
    /// </param>
    public void AddItem (string resourceName, long quantity)
    {
        this._AddObject (resourceName, quantity, "item");
    }

    /// <summary>
    /// Add a currency reward
    /// </summary>
    /// <param name="currencyName">
    /// Name of the currency as specified on the dashboard
    /// </param>
    /// <param name="amount">
    /// amount to be given
    /// </param>
    public void AddCurrency (string currencyName, long amount)
    {
        this._AddObject (currencyName, amount, "currency");
    }

    public Dictionary<string, Dictionary<string, string>> getRewards ()
    {
        return this.rewards;
    }

    protected void _AddObject (string name, long quantity, string type)
    {
        if (!_CheckArguments (name, quantity, type)) {
            SwrveLog.LogError ("ERROR: IapRewards reward has not been added because it received an illegal argument");
            return;
        }

        Dictionary<string,string> item = new Dictionary<string, string> ();
        item.Add ("amount", quantity.ToString ());
        item.Add ("type", type);

        this.rewards.Add (name, item);
    }

    protected bool _CheckArguments (string name, long quantity, string type)
    {
        if (String.IsNullOrEmpty (name)) {
            SwrveLog.LogError ("IapRewards illegal argument: reward name cannot be empty");
            return false;
        }
        if (quantity <= 0) {
            SwrveLog.LogError ("IapRewards illegal argument: reward amount must be greater than zero");
            return false;
        }
        if (String.IsNullOrEmpty (type)) {
            SwrveLog.LogError ("IapRewards illegal argument: type cannot be empty");
            return false;
        }

        return true;
    }
}

