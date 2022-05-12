using System;
using System.Collections.Generic;
using SwrveUnity.Helpers;

namespace SwrveUnity.ResourceManager
{
    /// <summary>
    /// Represents a resource set up in the dashboard.
    /// </summary>
    public class SwrveResource
    {
        public readonly Dictionary<string, string> Attributes;

        public SwrveResource(Dictionary<string, string> attributes)
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
        public T GetAttribute<T>(string attributeName, T defaultValue)
        {
            if (Attributes.ContainsKey(attributeName))
            {
                string val = Attributes[attributeName];
                if (val != null)
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Represents a resource set up in the dashboard.
    /// </summary>
    public class SwrveABTestDetails
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int CaseIndex;

        public SwrveABTestDetails(string id, string name, int caseIndex)
        {
            this.Id = id;
            this.Name = name;
            this.CaseIndex = caseIndex;
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
        /// Information about the AB Tests a user is part of. To use this feature enable the
        /// flag abTestDetailsEnabled in your configuration.
        /// </summary>
        public List<SwrveABTestDetails> ABTestDetails;

        public SwrveResourceManager()
        {
            UserResources = new Dictionary<string, SwrveResource>();
            ABTestDetails = new List<SwrveABTestDetails>();
        }

        /// <summary>
        /// Update the resources with the JSON content coming from the Swrve servers.
        /// </summary>
        /// <param name="userResourcesJson">
        /// JSON response coming from the Swrve servers.
        /// </param>
        public void SetResourcesFromJSON(Dictionary<string, Dictionary<string, string>> userResourcesJson)
        {
            Dictionary<string, SwrveResource> newUserResources = new Dictionary<string, SwrveResource>();
            Dictionary<string, Dictionary<string, string>>.Enumerator enumerator = userResourcesJson.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, Dictionary<string, string>> userResource = enumerator.Current;
                newUserResources[userResource.Key] = new SwrveResource(userResource.Value);
            }
            UserResources = newUserResources;
        }

        /// <summary>
        /// Update the AB Test details with the JSON content coming from the Swrve servers.
        /// </summary>
        /// <param name="abTestDetailsJson">
        /// JSON response coming from the Swrve servers.
        /// </param>
        public void SetABTestDetailsFromJSON(Dictionary<string, object> abTestDetailsJson)
        {
            List<SwrveABTestDetails> abTestDetails = new List<SwrveABTestDetails>();
            Dictionary<string, object>.Enumerator enumerator = abTestDetailsJson.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, object> abTestDetailsPair = enumerator.Current;
                if (abTestDetailsPair.Value is Dictionary<string, object>)
                {
                    Dictionary<string, object> abTestDetailsDic = (Dictionary<string, object>)abTestDetailsPair.Value;
                    string name = (string)abTestDetailsDic["name"];
                    int caseIndex = MiniJsonHelper.GetInt(abTestDetailsDic, "case_index", 0);
                    SwrveABTestDetails newDetails = new SwrveABTestDetails(abTestDetailsPair.Key, name, caseIndex);
                    abTestDetails.Add(newDetails);
                }
            }
            ABTestDetails = abTestDetails;
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
        public SwrveResource GetResource(string resourceId)
        {
            if (UserResources != null)
            {
                if (UserResources.ContainsKey(resourceId))
                {
                    return UserResources[resourceId];
                }
            }
            else
            {
                SwrveLog.LogWarning(String.Format("SwrveResourceManager::GetResource('{0}'): Resources are not available yet.", resourceId));
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
        public T GetResourceAttribute<T>(string resourceId, string attributeName, T defaultValue)
        {
            SwrveResource resource = GetResource(resourceId);
            if (resource != null)
            {
                return resource.GetAttribute<T>(attributeName, defaultValue);
            }

            return defaultValue;
        }
    }
}
