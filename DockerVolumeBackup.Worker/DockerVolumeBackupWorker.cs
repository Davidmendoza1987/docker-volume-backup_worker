using System.Management.Automation;

namespace DockerVolumeBackup.Worker;

// TODO: Add unit tests
/// <summary>
/// Class DockerVolumeBackupWorker.
/// Implements the <see cref="Microsoft.Extensions.Hosting.BackgroundService" />
/// </summary>
/// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
public class DockerVolumeBackupWorker : BackgroundService
{
    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger<DockerVolumeBackupWorker> _logger; // TODO: Implement logging

    /// <summary>
    /// The discord service
    /// </summary>
    private readonly IDiscordService _discordService;

    /// <summary>
    /// The env
    /// </summary>
    private readonly IEnvironment _env;

    /// <summary>
    /// The webhook URL
    /// </summary>
    private readonly string _webhookUrl;

    /// <summary>
    /// The retention policy enabled
    /// </summary>
    private readonly bool _retentionPolicyEnabled;

    /// <summary>
    /// The retention policy length
    /// </summary>
    private readonly TimeSpan _retentionPolicyLength;

    /// <summary>
    /// The retention policy minimum volume count
    /// </summary>
    private readonly int _retentionPolicyMinimumVolumeCount;

    /// <summary>
    /// The worker interval in ms
    /// </summary>
    private readonly int _workerIntervalInMs;

    /// <summary>
    /// The backup paths
    /// </summary>
    private readonly string[] _backupPaths;

    /// <summary>
    /// The backup date time
    /// </summary>
    private DateTime BackupDateTime;

    /// <summary>
    /// The discord messages
    /// </summary>
    private List<string> DiscordMessages = [];

    public DockerVolumeBackupWorker(
        ILogger<DockerVolumeBackupWorker> logger,
        IDiscordService discordService,
        IEnvironment env)
    {
        _logger = logger;
        _discordService = discordService;
        _env = env;
        _backupPaths = _env.GetRequiredEnvironmentVariable("DVB_BACKUP_PATHS").Split(',');
        _retentionPolicyEnabled = _env.GetRequiredEnvironmentVariable<bool>("DVB_RETENTION_POLICY_ENABLED");
        _retentionPolicyLength = _env.GetRequiredEnvironmentVariable<TimeSpan>("DVB_RETENTION_POLICY_LENGTH");
        _retentionPolicyMinimumVolumeCount = _env.GetRequiredEnvironmentVariable<int>("DVB_RETENTION_POLICY_MINIMUM_VOLUME_COUNT");
        _webhookUrl = _env.GetRequiredEnvironmentVariable("DVB_DISCORD_DVBBOT_WEBHOOK_URL");
        _workerIntervalInMs = _env.GetRequiredEnvironmentVariable<int>("DVB_WORKER_INTERVAL_IN_MS");
    }

    /// <summary>
    /// This method overrides the BackgroundService's ExecuteAsync to periodically execute backup logic.
    /// It runs in a loop until a cancellation is requested, handling backup operations and sending notifications.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Continue looping until a cancellation request is received
        while (!stoppingToken.IsCancellationRequested)
        {
            // Set BackupDateTime to the current date and time at the start of each loop iteration
            BackupDateTime = DateTime.Now;

            // Initialize or clear the list of Discord messages for this iteration
            DiscordMessages = [];

            // Execute the backup logic which includes verifying and backing up Docker volumes
            await ExecuteDockerVolumeBackupLogic(stoppingToken);

            // Check if there are any messages to be sent to Discord after executing the backup logic
            if (DiscordMessages.Count != 0)
            {
                // Concatenate all messages into a single string separated by newline characters
                var discordPostMessage = string.Join("\n", DiscordMessages);

                // Send the concatenated messages to Discord via the configured webhook URL
                await _discordService.PostDiscordMessageAsync(_webhookUrl, discordPostMessage);
            }

            // Pause the execution of the loop for a duration specified by an environment variable
            // This delay determines how often the backup process runs
            await Task.Delay(_workerIntervalInMs, stoppingToken);
        }
    }

    /// <summary>
    /// This method orchestrates the overall Docker volume backup process. It checks for available volumes,
    /// verifies backup paths, initiates backup procedures, and applies data retention policies.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>System.Threading.Tasks.Task.</returns>
    private async Task ExecuteDockerVolumeBackupLogic(CancellationToken stoppingToken)
    {
        //TODO: Implement stoppingToken usage to handle cancellation requests

        // Encapsulates the logic in a try block to catch and handle any unexpected exceptions
        try
        {
            // Initialize lists to hold validated volumes and backup paths
            var verifiedVolumes = new List<string>();
            var verifiedBackupPaths = new List<string>();

            // Call method to retrieve all available Docker volumes
            var volumeNames = GetAllDockerVolumes();

            // If no volumes are found, log this event and exit the method early
            if (volumeNames.Count == 0)
            {
                DiscordMessages.Add($"{DateTime.Now} | No Docker volumes found.");
                return;
            }

            // Iterate over each provided backup path to verify its existence
            foreach (var backupPath in _backupPaths)
            {
                if (Directory.Exists(backupPath))
                {
                    // If a backup path is valid, add it to the list of verified backup paths
                    verifiedBackupPaths.Add(backupPath);
                }
                else
                {
                    // If a backup path does not exist, log this event
                    DiscordMessages.Add($"{DateTime.Now} | Backup path {backupPath} does not exist.");
                }
            }

            // If no valid backup paths are found after verification, log this event and exit the method early
            if (verifiedBackupPaths.Count == 0)
            {
                DiscordMessages.Add($"{DateTime.Now} | No backup paths were verified.");
                return;
            }

            // Double nested loop to backup each volume to each verified backup path
            foreach (var volumeName in volumeNames)
            {
                foreach (var backupPath in verifiedBackupPaths)
                {
                    // Call method to backup a specific volume to a specific path
                    BackupDockerVolume(volumeName, backupPath);
                }
            }

            // If retention policy is enabled, apply it to each backup path
            if (_retentionPolicyEnabled)
            {
                foreach (var path in _backupPaths)
                {
                    // Call asynchronous method to apply retention policy to a specific path
                    // TODO: Apply rention policy asynchronously to all paths in parallel
                    await ApplyVolumeRetentionPolicyAsync(path);
                }
            }
        }
        // Catch block to handle and log any exceptions that occur during the backup process
        catch (Exception ex)
        {
            // Log the error message derived from the exception
            DiscordMessages.Add($"{DateTime.Now} | An error occurred: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>
    /// This method retrieves a list of all Docker volumes available on the host using PowerShell.
    /// It returns a list of volume names or an empty list if an error occurs.
    /// </summary>
    /// <returns>System.Collections.Generic.List&lt;string&gt;.</returns>
    private List<string> GetAllDockerVolumes()
    {
        // Define the PowerShell script to list all Docker volumes quietly
        string script = "docker volume ls -q";

        // Create a new PowerShell instance to run the script
        using PowerShell ps = PowerShell.Create();

        // Add the docker volume listing script to the PowerShell instance
        ps.AddScript(script);

        // Try block to attempt to execute the script and handle potential errors
        try
        {
            // Execute the PowerShell script and capture the output
            var results = ps.Invoke();

            // Filter the results to ignore nulls, convert each to string, and collect into a list
            return results.Where(result => result != null).Select(result => result.ToString()).ToList();
        }
        catch (Exception ex)
        {
            // Catch any exceptions that occur during execution, log the error, and return an empty list
            DiscordMessages.Add($"{DateTime.Now} | Failed to list Docker volumes: {ex.Message}");

            // Return an empty list indicating that no volumes were retrieved
            return [];
        }
    }

    /// <summary>
    /// This method handles the backup of a specified Docker volume to a verified backup path.
    /// It constructs a backup file name, executes a Docker command via PowerShell to create the backup,
    /// and logs the outcome or any errors encountered during the process.
    /// </summary>
    /// <param name="volumeName">Name of the volume.</param>
    /// <param name="verifiedBackupPath">The verified backup path.</param>
    private void BackupDockerVolume(string volumeName, string verifiedBackupPath)
    {
        // Construct the backup file name using the volume name, current date and time, and a unique identifier
        string backupFileName = $"{volumeName}_{BackupDateTime:yyyy-MM-dd_HH-mm-ss}_{Guid.NewGuid()}.tar";

        // Combine the verified backup path with the backup file name to create a full path for the backup file
        string fullBackupPath = Path.Combine(verifiedBackupPath, backupFileName);

        // Define the PowerShell script that runs a Docker command to back up the specified volume
        // The script uses an Alpine Linux container to tar (archive and compress) the volume's data
        string script = $"docker run --rm -v {volumeName}:/data --volume {verifiedBackupPath}:/backup alpine tar -czf /backup/{backupFileName} /data";

        // Create an instance of PowerShell to execute the backup script
        using PowerShell ps = PowerShell.Create();

        // Add the backup script to the PowerShell instance
        ps.AddScript(script);

        // Try block to handle execution of the backup command
        try
        {
            // Execute the PowerShell script
            ps.Invoke();

            // Check if the backup file was successfully created by verifying its existence
            if (File.Exists(fullBackupPath))
            {
                // Retrieve information about the backup file
                FileInfo fileInfo = new(fullBackupPath);

                // Check if the backup file is not empty by verifying its size
                if (fileInfo.Length > 0)
                {
                    // Add a success message to DiscordMessages if the backup file is valid
                    DiscordMessages.Add($"{DateTime.Now} | Backup successfully created for volume {volumeName} at {verifiedBackupPath}");
                }
                else
                {
                    // Add a warning message to DiscordMessages if the backup file is empty
                    DiscordMessages.Add($"{DateTime.Now} | Backup file created but is empty for volume {volumeName}");
                }
            }
            else
            {
                // Add an error message to DiscordMessages if the backup file was not created
                DiscordMessages.Add($"{DateTime.Now} | Failed to create backup for volume {volumeName}. Check Docker and file system permissions.");
            }
        }
        // Catch block to handle any exceptions that occur during the backup process
        catch (Exception ex)
        {
            // Add an error message to DiscordMessages detailing the exception
            DiscordMessages.Add($"{DateTime.Now} | An error occurred backing up Docker volume {volumeName} to {verifiedBackupPath}: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    /// <summary>
    /// This asynchronous method applies a data retention policy to the backup files in a specified directory.
    /// It deletes files based on their age and ensures a minimum number of backup files are retained.
    /// </summary>
    /// <param name="backupPath">The backup path.</param>
    /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
    private async Task ApplyVolumeRetentionPolicyAsync(string backupPath)
    {
        // Retrieve all backup files from the specified path and store them as FileInfo objects for easy manipulation
        var backupFiles = new DirectoryInfo(backupPath)
            .EnumerateFiles("*.tar")
            .OrderBy(f => f.CreationTime)
            .ToList();

        // Order the retrieved files by their creation time in ascending order to process the oldest files first
        var orderedFiles = backupFiles.OrderBy(f => f.CreationTime).ToList();

        // Filter the list of files to find those older than the specified retention policy length
        var filesToDelete = orderedFiles
            .Where(f => DateTime.Now - f.CreationTime > _retentionPolicyLength)
            // Determine the number of files that can be deleted while maintaining the minimum required file count
            .Take(Math.Max(0, orderedFiles.Count - _retentionPolicyMinimumVolumeCount)).ToList();

        // Iterate through the list of files to delete
        // TODO: Delete files in bulk to improve performance
        foreach (var file in filesToDelete)
        {
            try
            {
                // Attempt to delete the file
                file.Delete();

                // Log a success message noting that an old backup was successfully deleted according to the retention policy
                DiscordMessages.Add($"{DateTime.Now} | Applied Data Retention Policy - Deleted old backup: {file.FullName}");
            }
            // Catch block for handling UnauthorizedAccessException which occurs if the application does not have the necessary permissions to delete the file
            catch (UnauthorizedAccessException uex)
            {
                // Log a specific error message for permission issues to the DiscordMessages list
                DiscordMessages.Add($"{DateTime.Now} | Permission issue deleting file {file.FullName}: {uex.Message}");
            }

            // Catch block for handling IOExceptions which can occur due to various IO problems such as the file being in use by another process, or hardware errors affecting file access
            catch (IOException ioex)
            {
                // Log a specific error message for general IO issues to the DiscordMessages list
                DiscordMessages.Add($"{DateTime.Now} | IO issue deleting file {file.FullName}: {ioex.Message}");
            }
            catch (Exception ex)
            {
                // If an error occurs during file deletion, log an error message with details of the failure
                DiscordMessages.Add($"{DateTime.Now} | An error occurred applying the retention policy, error deleting file {file.FullName}: {ex.Message}");
            }
        }
    }
}
