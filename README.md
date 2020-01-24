# ImgBot

ImgBot crawls through all your image files in GitHub and submits pull requests after applying a lossless compression.
This will make the file size go down, but leave the dimensions and quality just as good.

![screenshot](https://imgbot.net/images/screen.png?cache=2)

## Configuration

ImgBot supports optional configuration through a `.imgbotconfig` json file.
This is not a required step to using ImgBot and is only for more advanced scenarios.
This file should be placed in the root of the repository and set to your liking.

```
{
    "Schedule": "Daily", // daily|weekly|monthly
    "ignoredFiles": [
    	"*.jpg",                   // by extension
    	"image1.png",              // by filename
    	"public/special_images/*", // by folderpath
    ],
    "aggressiveCompression": "true", // true|false
    "compressWiki": "true" // true|false
}
```

The following are the currently supported parameters.
If there are any configuration settings you would like to see supported,
please feel free to open an issue here in the repo or shoot an email over
to help@imgbot.net.

**Schedule**

- Optional
- Accepts: daily|weekly|monthly
- Limits the PRs from ImgBot to once a day, once a week, or once a month respectively.
- The default behavior is to receive ImgBot PRs as images require optimization.

**ignoredFiles**

- Optional
- Accepts the syntax for searchPattern on [Directory.EnumerateFiles()](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles)
- Limits the images optimized by ImgBot by esentially ignoring them.
- When ignoring by filename no path is necessary, when ignoring by foldername full path from root is necessary.

**aggressiveCompression**

- Optional
- Accepts: true|false
- Opt in to use lossy compression algorithms
- The default behavior without this setting is lossless compression

**compressWiki**

- Optional
- Accepts: true|false
- Opt in to also compress wiki repo
    - Example: `https://github.com/YOUR_USERNAME/YOUR_REPOSITORY.wiki.git`
- The default behavior is opt out

Find out more: https://imgbot.net/docs

## Contributing

All the code for ImgBot is available on GitHub. We will gladly accept contributions for the service, the website, and the documentation. This is where you can find out how to get set up to run locally as well as detailed information on exactly how ImgBot works.

https://imgbot.net/docs#contributing
