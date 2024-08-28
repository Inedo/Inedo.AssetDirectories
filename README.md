**This library has been deprecated. Use [Inedo.ProGet](https://github.com/Inedo/pgutil) instead.**

# Inedo.AssetDirectories

This library is a set of utilities for working with [ProGet Asset Directories](https://docs.inedo.com/docs/proget-advanced-assets). It wraps the [Asset Directory API](https://docs.inedo.com/docs/proget-reference-api-asset-directories-api)
with a more accessible C#/.NET interface.

## Installation
Add a reference to Inedo.AssetDirectories using NuGet package manager.

## Configuration
All access to a ProGet Asset Directory is through the `AssetDirectoryClient` class. Supply the required `endpointUrl` to the constructor and optionally the `apiKey` (or `userName` and `password`):

```C#
var client = new AssetDirectoryClient(endpointUrl: "https://my.proget/endpoints/<AssetDirectoryName>", apiKey: "<API KEY>");
```

## List Directory Contents
```C#
// List items in the root folder of the asset directory.
var rootItems = await client.ListContentsAsync();

// List items in the myFolder/ folder of the asset directory.
var myFolderItems = await client.ListContentsAsync("myFolder");

// List items in the other/files/ folder and all subfolders.
var recursiveItems = await client.ListContentsAsync("other/files", true);
```

## Get Item Metadata
```C#
// Get metadata for path/to/file.txt. An exception is raised if the file does not exist.
var item1 = await client.GetItemMetadataAsync("path/to/file.txt");

// Get metadata for another-file.txt if it exists. If it does not exist, returns null.
var item2 = await client.TryGetItemMetadataAsync("another-file.txt");
```

## Download a File
```C#
// Download the archive.zip file to C:\temp\archive.zip.
using var stream = await client.DownloadFileAsync("archive.zip");
using var dest = File.Create(@"C:\temp\archive.zip");
await stream.CopyToAsync(dest);
```

## Open a File for Random Access
```C#
// Open the archive.zip file on the remote asset directory without downloading it, and wrap it with a BufferedStream.
using var stream = new BufferedStream(await client.OpenRandomAccessFileAsync("archive.zip"));
using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
```

## Upload a File
```C#
// Upload backup.zip to the asset directory, breaking the upload into multiple chunks as necessary.
using var source = File.OpenRead(@"C:\temp\backup.zip");
using var destStream = await client.UploadMultipartFileAsync("backup.zip", source.Length);
await source.CopyToAsync(destStream);
```

## Upload a File (Single Request)
```C#
// Upload users.txt to the asset directory as a single request.
using var source = File.OpenRead(@"C:\temp\users.txt");
using var destStream = await client.UploadFileAsync("users.txt", source.Length, totalSize: source.Length);
await source.CopyToAsync(destStream);
```

## Delete a File
```C#
// Delete the myfile.txt file in the asset directory if it exists.
await client.DeleteItemAsync("myfile.txt");
```

## Delete a Directory
```C#
// Delete the my/dir in the asset directory if it exists and is empty.
await client.DeleteItemAsync("my/dir");

// Delete the my/dir in the asset directory if it exists, and recusively deletes its contents if it is not empty.
await client.DeleteItemAsync("my/dir", true);
```

## Create a Directory
```C#
// Ensures that the emptydir folder exists in the asset directory.
await client.CreateDirectoryAsync("emptydir");
```

## Write Metadata to Item
```C#
// Ensures that the "owner" property of archive.zip is set to "Steve Dennis".
await client.UpdateItemMetadata("archive.zip",
	userMetadata: new Dictionary<string, UserMetadataValue>
	{
		["owner"] = "Steve Dennis"
	}
);
```
