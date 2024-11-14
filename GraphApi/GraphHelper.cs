using Azure.Core;
using Azure.Identity;
using CBA.SOE.GraphApi.Entities;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CBA.SOE.GraphApi
{
    public static class GraphHelper
    {
        // Logger object
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Settings object
        private static Settings _settings;

        // App-ony auth token credential
        private static ClientSecretCredential _clientSecretCredential;

        // Client configured with app-only authentication
        private static GraphServiceClient _appClient;

        public static void InitializeGraphForAppOnlyAuth(Settings settings)
        {
            try
            {
                // Ensure settings isn't null
                _settings = settings ?? throw new System.NullReferenceException("Settings cannot be null");

                if (_clientSecretCredential == null)
                {
                    _clientSecretCredential = new ClientSecretCredential(_settings.TenantId, _settings.ClientId, _settings.ClientSecret);
                }

                if (_appClient == null)
                {
                    // Use the default scope, which will request the scopes configured on the app registration
                    _appClient = new GraphServiceClient(_clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[InitializeGraphForAppOnlyAuth] Error initializing Graph for app-only auth: {ex.Message}");
            }
        }


        public static async Task<string> GetAppOnlyTokenAsync()
        {
            try
            {
                // Ensure credential isn't null
                _ = _clientSecretCredential ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Request token with given scopes
                var context = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
                var response = await _clientSecretCredential.GetTokenAsync(context);

                return response.Token;
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetAppOnlyTokenAsync] Error getting app-only auth token: {ex.Message}");

                return null;
            }

        }

        public static Task<UserCollectionResponse> GetUsersAsync()
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                return _appClient.Users.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id", "mail" };

                    // Get at most 25 results
                    config.QueryParameters.Top = 25;

                    // Sort by display name
                    config.QueryParameters.Orderby = new[] { "displayName" };
                });
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetUsersAsync] Error getting users: {ex.Message}");

                return null;
            }
        }

        public async static Task<Stream> GetUserProfilePictureAsync(string userId)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get user's profile picture
                var profilePhoto = await _appClient.Users[userId].Photo.Content.GetAsync();

                return profilePhoto;
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetUserProfilePictureAsync] Error getting profile picture for user ({userId}) : {ex.Message}");

                return null;
            }
        }

        public static async Task<Dictionary<string, List<User>>> GetUsersByGroupsAsync()
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get all groups
                var groups = await _appClient.Groups.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id" };
                });

                // Dictionary to hold users by group
                var usersByGroups = new Dictionary<string, List<User>>();

                // Iterate through each group
                foreach (var group in groups.Value)
                {
                    // Get users in the group
                    var users = await _appClient.Groups[group.Id].Members.GetAsync((config) =>
                    {
                        // Only request specific properties
                        config.QueryParameters.Select = new[] { "displayName", "id", "mail" };

                        // Get at most 25 results
                        config.QueryParameters.Top = 25;

                        // Sort by display name
                        config.QueryParameters.Orderby = new[] { "displayName" };

                        // Add ConsistencyLevel header
                        config.Headers.Add("ConsistencyLevel", "eventual");

                        // Add $count=true query parameter
                        config.QueryParameters.Count = true;
                    });

                    // Add users to the dictionary
                    usersByGroups[group.DisplayName] = users.Value.OfType<User>().ToList();
                }

                return usersByGroups;
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetUsersByGroupsAsync] Error getting users by groups: {ex.Message}");

                return null;
            }
        }

        public static async Task<List<Group>> GetGroupsAsync()
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get all groups
                var groups = await _appClient.Groups.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id" };
                });

                return groups?.Value?.ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetGroupsAsync] Error getting groups: {ex.Message}");

                return null;
            }
        }

        public static async Task<List<User>> GetUsersInGroupAsync(string groupId)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get users in the group
                var users = await _appClient.Groups[groupId].Members.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id", "mail" };

                    // Get at most 25 results
                    config.QueryParameters.Top = 25;

                    // Sort by display name
                    config.QueryParameters.Orderby = new[] { "displayName" };

                    // Add ConsistencyLevel header
                    config.Headers.Add("ConsistencyLevel", "eventual");

                    // Add $count=true query parameter
                    config.QueryParameters.Count = true;
                });

                return users?.Value?.OfType<User>().ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetUsersInGroupAsync] Error getting users in group: {ex.Message}");

                return null;
            }
        }
        public static async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get all users
                var users = await _appClient.Users.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id", "mail" };

                    // Get at most 25 results
                    config.QueryParameters.Top = 25;

                    // Sort by display name
                    config.QueryParameters.Orderby = new[] { "displayName" };

                    // Add ConsistencyLevel header
                    config.Headers.Add("ConsistencyLevel", "eventual");

                    // Add $count=true query parameter
                    config.QueryParameters.Count = true;
                });

                return users?.Value?.ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetAllUsersAsync] Error getting users: {ex.Message}");

                return null;
            }
        }

        public static async Task<List<Group>> GetGroupsForUserAsync(string userId)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get groups for the user
                var groups = await _appClient.Users[userId].MemberOf.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id" };

                    // Add ConsistencyLevel header
                    config.Headers.Add("ConsistencyLevel", "eventual");

                    // Add $count=true query parameter
                    config.QueryParameters.Count = true;
                });

                return groups?.Value?.OfType<Group>().ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetGroupsForUserAsync] Error getting groups for user: {ex.Message}");

                return null;
            }
        }

        public static async Task<List<Device>> GetDevicesForUserAsync(string userId)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get devices for the user
                var devices = await _appClient.Users[userId].OwnedDevices.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id" };

                    // Add ConsistencyLevel header
                    config.Headers.Add("ConsistencyLevel", "eventual");

                    // Add $count=true query parameter
                    config.QueryParameters.Count = true;
                });

                return devices?.Value?.OfType<Device>().ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetDevicesForUserAsync] Error getting devices for user: {ex.Message}");

                return null;
            }
        }

        public static async Task<List<Device>> GetDevicesInDomainAsync(string domain)
        {
            try
            {
                // Ensure client isn't null
                _ = _appClient ?? throw new System.NullReferenceException("Graph has not been initialized for app-only auth");

                // Get devices in the domain
                var devices = await _appClient.Devices.GetAsync((config) =>
                {
                    // Only request specific properties
                    config.QueryParameters.Select = new[] { "displayName", "id" };

                    // Filter by domain
                    config.QueryParameters.Filter = $"domain eq '{domain}'";

                    // Add ConsistencyLevel header
                    config.Headers.Add("ConsistencyLevel", "eventual");

                    // Add $count=true query parameter
                    config.QueryParameters.Count = true;
                });

                return devices?.Value?.ToList();
            }
            catch (Exception ex)
            {
                // Handle exception
                _logger.Error($"[GetDevicesInDomainAsync] Error getting devices in domain: {ex.Message}");

                return null;
            }
        }
    }
}
