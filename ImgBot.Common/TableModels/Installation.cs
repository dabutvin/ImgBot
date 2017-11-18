﻿using Microsoft.WindowsAzure.Storage.Table;

namespace ImgBot.Common.TableModels
{
    public class Installation : TableEntity
    {
        public Installation()
        {
        }

        public Installation(int installationId, string repoName)
        {
            PartitionKey = installationId.ToString();
            RowKey = repoName;

            InstallationId = installationId;
            RepoName = repoName;
        }

        public int InstallationId { get; set; }

        public string RepoName { get; set; }

        public string AccessTokensUrl { get; set; }

        public string CloneUrl { get; set; }

        public string Owner { get; set; }
    }
}
