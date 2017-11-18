﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using ImgBot.Common;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;

namespace ImgBot.Function
{
    public static class CompressImages
    {
        private const string BranchName = "imgbot";
        private const string Username = "x-access-token";

        public static async Task RunAsync(CompressimagesParameters parameters)
        {
            CredentialsHandler credentialsProvider =
                (url, user, cred) =>
                new UsernamePasswordCredentials { Username = Username, Password = parameters.Password };

            var inMemoryCredentialStore = new InMemoryCredentialStore(new Octokit.Credentials(Username, parameters.Password));

            // clone
            var cloneOptions =
            LibGit2Sharp.Repository.Clone(parameters.CloneUrl, parameters.LocalPath, new CloneOptions
            {
                CredentialsProvider = credentialsProvider,
            });
            var repo = new LibGit2Sharp.Repository(parameters.LocalPath);
            var remote = repo.Network.Remotes["origin"];

            // check if we have the branch already or this is empty repo
            try
            {
                if (repo.Network.ListReferences(remote, credentialsProvider).Any() == false)
                    return;

                if (repo.Network.ListReferences(remote, credentialsProvider).Any(x => x.CanonicalName == $"refs/heads/{BranchName}"))
                    return;
            }
            catch
            {
                // ignore
            }

            var repoConfiguration = new RepoConfiguration();

            try
            {
                // see if .imgbotconfig exists in repo root
                var repoConfigJson = File.ReadAllText(parameters.LocalPath + "\\.imgbotconfig");
                if (!string.IsNullOrEmpty(repoConfigJson))
                {
                    repoConfiguration = JsonConvert.DeserializeObject<RepoConfiguration>(repoConfigJson);
                }
            }
            catch
            {
                // ignore
            }

            if (Schedule.ShouldOptimizeImages(repoConfiguration, repo) == false)
                return;

            // check out the branch
            repo.CreateBranch(BranchName);
            var branch = Commands.Checkout(repo, BranchName);

            // optimize images
            var imagePaths = ImageQuery.FindImages(parameters.LocalPath, repoConfiguration);
            var optimizedImages = OptimizeImages(repo, parameters.LocalPath, imagePaths);
            if (optimizedImages.Length == 0)
                return;

            // create commit message based on optimizations
            var commitMessage = CommitMessage.Create(optimizedImages);

            // commit
            var signature = new LibGit2Sharp.Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);

            // push to GitHub
            repo.Network.Push(remote, $"refs/heads/{BranchName}", new PushOptions
            {
                CredentialsProvider = credentialsProvider,
            });

            // open PR
            var githubClient = new GitHubClient(new ProductHeaderValue("ImgBot"), inMemoryCredentialStore);

            var pr = new NewPullRequest(KnownGitHubs.CommitMessageTitle, BranchName, "master");
            pr.Body = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information.";
            await githubClient.PullRequest.Create(parameters.RepoOwner, parameters.RepoName, pr);
        }

        private static CompressionResult[] OptimizeImages(LibGit2Sharp.Repository repo, string localPath, string[] imagePaths)
        {
            var optimizedImages = new List<CompressionResult>();

            ImageOptimizer imageOptimizer = new ImageOptimizer();
            Parallel.ForEach(imagePaths, image =>
            {
                try
                {
                    Console.WriteLine(image);
                    FileInfo file = new FileInfo(image);
                    double before = file.Length;
                    if (imageOptimizer.LosslessCompress(file))
                    {
                        optimizedImages.Add(new CompressionResult
                        {
                            Title = image.Substring(localPath.Length),
                            SizeBefore = before / 1024d,
                            SizeAfter = file.Length / 1024d,
                        });

                        Commands.Stage(repo, image);
                    }
                }
                catch
                {
                }
            });

            return optimizedImages.ToArray();
        }
    }
}
