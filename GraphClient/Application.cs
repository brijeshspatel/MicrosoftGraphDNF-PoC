using CBA.SOE.GraphApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace CBA.SOE.ConsoleApp
{
    public class Application
    {
        private readonly ILogger<Application> _logger;

        public Application(ILogger<Application> logger, IConfiguration config)
        {
            _logger = logger;
        }

        public async Task Run(string[] args)
        {
            _logger.LogInformation("Running CBA.SOE.ConsoleApp");

            Console.WriteLine(".NET Graph App-only PoC\n");

            // Initialize Graph
            InitGraph.InitializeGraph();


            #region Graph Choices & Calls
            int choice = -1;

            while (choice != 0)
            {
                Console.WriteLine($"{Environment.NewLine}Please choose one of the following options:");
                Console.WriteLine("1. Display access token");
                Console.WriteLine("2. List users");
                Console.WriteLine("3. Download profile photos");
                Console.WriteLine("4. List users by all groups");
                Console.WriteLine("5. List users by a selected group");
                Console.WriteLine("6. List groups by a selected user");
                Console.WriteLine("7. List devices by a selected user");
                Console.WriteLine("8. List devices by a selected domain");
                Console.WriteLine("0. Exit");

                Console.Write($"{Environment.NewLine}Enter your choice here: ");
                try
                {
                    choice = int.Parse(Console.ReadLine() ?? string.Empty);
                }
                catch (System.FormatException)
                {
                    // Set to invalid value
                    choice = -1;
                }

                switch (choice)
                {
                    case 0:
                        // Exit the program
                        Console.WriteLine("Goodbye...");
                        break;

                    case 1:
                        // Display access token
                        await DisplayAccessTokenAsync();
                        break;

                    case 2:
                        // List users
                        await ListUsersAsync();
                        break;

                    case 3:
                        // Download profile photos
                        await DownloadProfilePhotoOfAllUsersAsyn();
                        break;

                    case 4:
                        // List users by groups
                        await ListUsersByGroupsAsync();
                        break;

                    case 5:
                        // List users by a selected group
                        await ListUsersBySelectedGroupAsync();
                        break;

                    case 6:
                        // List groups by a selected user
                        await ListGroupsBySelectedUserAsync();
                        break;

                    case 7:
                        // List devices by a selected user
                        await ListDevicesBySelectedUserAsync();
                        break;

                    case 8:
                        // List devices by a selected domain
                        await ListDevicesBySelectedDomainAsync();
                        break;

                    default:
                        Console.WriteLine("Invalid choice! Please try again.");
                        break;
                }
            }
            #endregion


            _logger.LogInformation("Successfully executed CBA.SOE.ConsoleApp");
            Environment.ExitCode = 0;
        }

        private async Task DisplayAccessTokenAsync()
        {
            try
            {
                var appOnlyToken = await GraphHelper.GetAppOnlyTokenAsync();
                Console.WriteLine($"App-only token: {appOnlyToken}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DisplayAccessTokenAsync] Error getting app-only access token: {ex.Message}");
            }
        }

        private async Task ListUsersAsync()
        {
            try
            {
                var userPage = await GraphHelper.GetUsersAsync();

                if (userPage?.Value == null)
                {
                    Console.WriteLine("No results returned.");
                    return;
                }

                // Output each users's details
                foreach (var user in userPage.Value)
                {
                    Console.WriteLine($" User: {user.DisplayName ?? "NO NAME"}");
                    Console.WriteLine($"   ID: {user.Id}");
                    Console.WriteLine($"Email: {user.Mail ?? "NO EMAIL"}");
                }

                // If NextPageRequest is not null, there are more users
                // available on the server
                // Access the next page like:
                // var nextPageRequest = new UsersRequestBuilder(userPage.OdataNextLink, _appClient.RequestAdapter);
                // var nextPage = await nextPageRequest.GetAsync();
                var moreAvailable = !string.IsNullOrEmpty(userPage.OdataNextLink);

                Console.WriteLine($"More users available? {moreAvailable}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListUsersAsync] Error getting users: {ex.Message}");
            }
        }

        private async Task DownloadProfilePhotoOfAllUsersAsyn()
        {
            try
            {
                var userPage = await GraphHelper.GetUsersAsync();

                if (userPage?.Value == null)
                {
                    Console.WriteLine("No results returned.");
                    return;
                }

                // Download each users's profile photo
                foreach (var user in userPage.Value)
                {
                    await DownloadProfilePhotoOfUserAsync(user.Id);
                }

                Console.WriteLine($@"All available profile photos have been successfully downloaded and stored in the Trials\ProfilePhotos diretory.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DownloadProfilePhotoOfAllUsersAsyn] Error getting users: {ex.Message}");
            }
        }

        public async Task DownloadProfilePhotoOfUserAsync(string userId)
        {
            try
            {
                Stream profilePhoto = await GraphHelper.GetUserProfilePictureAsync(userId);

                if (profilePhoto != null)
                {
                    // Create the ProfilePhotos subdirectory in the root directory if it doesn't exist
                    string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), @"Trials\ProfilePhotos");
                    Directory.CreateDirectory(directoryPath);

                    // Initialise the Profile Photo path
                    string filePath = Path.Combine(directoryPath, $"ProfilePhoto-{userId}.jpg");

                    // Save the Profile Photo using System.Drawing
                    using (var image = Image.FromStream(profilePhoto))
                    {
                        image.Save(filePath, ImageFormat.Jpeg);
                    }

                    throw new Exception("TEST");

                    // Open the Profile Photo in the default image viewer
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[DownloadProfilePhotoOfUserAsync] Error saving profile photo for user ({userId}): {ex.Message}");
            }
        }
        private async Task ListUsersByGroupsAsync()
        {
            try
            {
                var usersByGroups = await GraphHelper.GetUsersByGroupsAsync();

                if (usersByGroups == null || usersByGroups.Count == 0)
                {
                    Console.WriteLine("No results returned.");
                    return;
                }

                // Output each group's users' details
                foreach (var group in usersByGroups)
                {
                    Console.WriteLine($"Group: {group.Key} ({group.Value.Count} members)");

                    foreach (var user in group.Value)
                    {
                        Console.WriteLine($"    User: {user.DisplayName ?? "NO NAME"}");
                        Console.WriteLine($"      ID: {user.Id}");
                        Console.WriteLine($"   Email: {user.Mail ?? "NO EMAIL"}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListUsersByGroupsAsync] Error getting users by groups: {ex.Message}");
            }
        }

        private async Task ListUsersBySelectedGroupAsync()
        {
            try
            {
                var groups = await GraphHelper.GetGroupsAsync();

                if (groups == null || groups.Count == 0)
                {
                    Console.WriteLine("No groups returned.");
                    return;
                }

                // List all groups
                Console.WriteLine("Groups:");
                for (int i = 0; i < groups.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {groups[i].DisplayName}");
                }

                // Get user to select a group
                Console.Write("Select a group by number: ");
                if (!int.TryParse(Console.ReadLine(), out int selectedGroupIndex) || selectedGroupIndex < 1 || selectedGroupIndex > groups.Count)
                {
                    Console.WriteLine("Invalid selection.");
                    return;
                }

                var selectedGroup = groups[selectedGroupIndex - 1];

                // Get users in the selected group
                var users = await GraphHelper.GetUsersInGroupAsync(selectedGroup.Id);

                if (users == null || users.Count == 0)
                {
                    Console.WriteLine($"No users returned for the selected group - {selectedGroup.DisplayName} (ID: {selectedGroup.Id}).");
                    return;
                }

                // Output selected group's users' details
                Console.WriteLine($"Group: {selectedGroup.DisplayName} ({users.Count} members)");

                foreach (var user in users)
                {
                    Console.WriteLine($"    User: {user.DisplayName ?? "NO NAME"}");
                    Console.WriteLine($"      ID: {user.Id}");
                    Console.WriteLine($"   Email: {user.Mail ?? "NO EMAIL"}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListUsersBySelectedGroupAsync] Error getting users by selected group: {ex.Message}");
            }
        }

        private async Task ListGroupsBySelectedUserAsync()
        {
            try
            {
                var users = await GraphHelper.GetAllUsersAsync();

                if (users == null || users.Count == 0)
                {
                    Console.WriteLine("No users returned.");
                    return;
                }

                // List all users
                Console.WriteLine("Users:");
                for (int i = 0; i < users.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {users[i].DisplayName ?? "NO NAME"}");
                }

                // Get user to select a user
                Console.Write("Select a user by number: ");
                if (!int.TryParse(Console.ReadLine(), out int selectedUserIndex) || selectedUserIndex < 1 || selectedUserIndex > users.Count)
                {
                    Console.WriteLine("Invalid selection.");
                    return;
                }

                var selectedUser = users[selectedUserIndex - 1];

                // Get groups for the selected user
                var groups = await GraphHelper.GetGroupsForUserAsync(selectedUser.Id);

                if (groups == null || groups.Count == 0)
                {
                    Console.WriteLine($"No groups returned for the selected user - {selectedUser.DisplayName} (ID: {selectedUser.Id}).");
                    return;
                }

                // Output selected user's groups' details
                Console.WriteLine($"User: {selectedUser.DisplayName} ({groups.Count} group memberships)");

                foreach (var group in groups)
                {
                    Console.WriteLine($"    Group: {group.DisplayName ?? "NO NAME"}");
                    Console.WriteLine($"      ID: {group.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListGroupsBySelectedUserAsync] Error getting groups by selected user: {ex.Message}");
            }
        }

        private async Task ListDevicesBySelectedUserAsync()
        {
            try
            {
                var users = await GraphHelper.GetAllUsersAsync();

                if (users == null || users.Count == 0)
                {
                    Console.WriteLine("No users returned.");
                    return;
                }

                // List all users
                Console.WriteLine("Users:");
                for (int i = 0; i < users.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {users[i].DisplayName ?? "NO NAME"}");
                }

                // Get user to select a user
                Console.Write("Select a user by number: ");
                if (!int.TryParse(Console.ReadLine(), out int selectedUserIndex) || selectedUserIndex < 1 || selectedUserIndex > users.Count)
                {
                    Console.WriteLine("Invalid selection.");
                    return;
                }

                var selectedUser = users[selectedUserIndex - 1];

                // Get devices for the selected user
                var devices = await GraphHelper.GetDevicesForUserAsync(selectedUser.Id);

                if (devices == null || devices.Count == 0)
                {
                    Console.WriteLine($"No devices returned for the selected user - {selectedUser.DisplayName} (ID: {selectedUser.Id}).");
                    return;
                }

                // Output selected user's devices' details
                Console.WriteLine($"User: {selectedUser.DisplayName} ({devices.Count} devices)");

                foreach (var device in devices)
                {
                    Console.WriteLine($"    Device: {device.DisplayName ?? "NO NAME"}");
                    Console.WriteLine($"      ID: {device.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListDevicesBySelectedUserAsync] Error getting devices by selected user: {ex.Message}");
            }
        }

        private async Task ListDevicesBySelectedDomainAsync()
        {
            try
            {
                // List of domains
                var domains = new List<string> { "67tc81.onmicrosoft.com", "au.cbainet.com", "branch1.cbainet.com", "branch2.cbainet.com", "cbainet.com", "pbs.cbainet.com", "uat.cbainet.com", "aud01.cbaidev01.com", "aut01.cbaitest01.com" };

                // List all domains
                Console.WriteLine("Domains:");
                for (int i = 0; i < domains.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {domains[i]}");
                }

                // Get user to select a domain
                Console.Write("Select a domain by number: ");
                if (!int.TryParse(Console.ReadLine(), out int selectedDomainIndex) || selectedDomainIndex < 1 || selectedDomainIndex > domains.Count)
                {
                    Console.WriteLine("Invalid selection.");
                    return;
                }

                var selectedDomain = domains[selectedDomainIndex - 1];

                // Get devices in the selected domain
                var devices = await GraphHelper.GetDevicesInDomainAsync(selectedDomain);

                if (devices == null || devices.Count == 0)
                {
                    Console.WriteLine($"No devices returned for the selected domain - {selectedDomain}.");
                    return;
                }

                // Output selected domain's devices' details
                Console.WriteLine($"Domain: {selectedDomain} ({devices.Count} devices)");

                foreach (var device in devices)
                {
                    Console.WriteLine($"    Device: {device.DisplayName ?? "NO NAME"}");
                    Console.WriteLine($"      ID: {device.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ListDevicesBySelectedDomainAsync] Error getting devices by selected domain: {ex.Message}");
            }
        }
    }
}
