using System;
using System.Collections.Generic;

namespace Swrve.ResourceManager
{
/// <summary>
/// Represents a resource set up in the dashboard.
/// </summary>
public class SwrveResource
{
    public readonly Dictionary<string, string> Attributes;

    public SwrveResource (Dictionary<string, string> attributes)
    {
        this.Attributes = attributes;
    }

    /// <summary>
    /// Get a resource attribute or the default value given.
    /// </summary>
    /// <param name="attributeName">
    /// Attribute identifier.
    /// </param>
    /// <param name="defaultValue">
    /// Default attribute value.
    /// </param>
    /// <returns>
    /// Value of the attribute or default value.
    /// </returns>
    public T GetAttribute<T> (string attributeName, T defaultValue)
    {
        if (Attributes.ContainsKey (attributeName)) {
            string val = Attributes [attributeName];
            if (val != null) {
                return (T)Convert.ChangeType (val, typeof(T));
            }
        }

        return defaultValue;
    }
}

/// <summary>
/// Use this resource manager to obtain the latest resources and their values.
/// </summary>
public class SwrveResourceManager
{
    /// <summary>
    /// Latest resources available for this user.
    /// </summary>
    public Dictionary<string, SwrveResource> UserResources;

    /// <summary>
    /// Update the resources with the JSON content coming from the Swrve servers.
    /// </summary>
    /// <param name="userResources">
    /// JSON response coming from the Swrve servers.
    /// </param>
    public void SetResourcesFromJSON (Dictionary<string, Dictionary<string, string>> userResources)
    {
        Dictionary<string, SwrveResource> newUserResources = new Dictionary<string, SwrveResource> ();
        foreach (string uuid in userResources.Keys) {
            newUserResources [uuid] = new SwrveResource (userResources [uuid]);
        }
        UserResources = newUserResources;
    }

    /// <summary>
    /// Get resource by its unique identifier.
    /// </summary>
    /// <param name="resourceId">
    /// Unique resource identifier.
    /// </param>
    /// <returns>
    /// The resource with the given unique identifier if available.
    /// </returns>
    public SwrveResource GetResource (string resourceId)
    {	
        if (UserResources != null) {
            if (UserResources.ContainsKey (resourceId)) {
                return UserResources [resourceId];
            }
        } else {
            SwrveLog.LogWarning(String.Format("SwrveResourceManager::GetResource('{0}') Failed. 'SwrveResourceManager.UserResources' is not initialized.", resourceId));
        }
        return null;
    }

    /// <summary>
    /// Get a resource attribute or the default value given.
    /// </summary>
    /// <param name="resourceId">
    /// Unique resource identifier.
    /// </param>
    /// <param name="attributeName">
    /// Attribute identifier.
    /// </param>
    /// <returns>
    /// Value of the resource attribute or default value.
    /// </returns>
    public T GetResourceAttribute<T> (string resourceId, string attributeName, T defaultValue)
    {
        SwrveResource resource = GetResource (resourceId);
        if (resource != null) {
            return resource.GetAttribute<T> (attributeName, defaultValue);
        }

        return defaultValue;
    }
}
}

