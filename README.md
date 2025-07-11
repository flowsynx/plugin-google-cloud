# FlowSynx Google Cloud Storage Plugin

The Google Cloud Storage Plugin is a pre-packaged, plug-and-play integration component for the FlowSynx engine. It enables interacting with Google Cloud Storage to manage buckets and objects, supporting a variety of operations such as uploading, downloading, listing, and purging object data. Designed for FlowSynx’s no-code/low-code automation workflows, this plugin simplifies cloud storage integration and file management.

This plugin is automatically installed by the FlowSynx engine when selected within the platform. It is not intended for manual installation or standalone developer use outside the FlowSynx environment.

---

## Purpose

The Google Cloud Storage Plugin allows FlowSynx users to:

- Upload and download files to and from Google Cloud Storage buckets.
- Manage objects and buckets with create, delete, and purge operations.
- List contents of buckets with filtering and metadata support.
- Perform existence checks for files or folders in workflows without writing code.

---

## Supported Operations

- **create**: Creates a new object in the specified bucket and path.
- **delete**: Deletes an object at the specified path in the bucket.
- **exist**: Checks if an object exists at the specified path.
- **list**: Lists objects under a specified path (prefix), with filtering and optional metadata.
- **purge**: Deletes all objects under the specified path, optionally forcing deletion.
- **read**: Reads and returns the contents of an object at the specified path.
- **write**: Writes data to a specified path in the bucket, with support for overwrite.

---

## Plugin Specifications

The plugin requires the following configuration:

- `ProjectId` (string): **Required.** The Google Cloud project ID.
- `PrivateKeyId` (string): **Required.** The private key ID of the service account.
- `PrivateKey` (string): **Required.** The private key of the service account.
- `ClientEmail` (string): **Required.** The client email of the service account.
- `ClientId` (string): **Required.** The client ID of the service account.
- `BucketName` (string): **Required.** The name of the Google Cloud Storage bucket to use.

### Example Configuration

```json
{
  "ProjectId": "my-gcp-project",
  "PrivateKeyId": "abc123xyz456",
  "PrivateKey": "-----BEGIN PRIVATE KEY-----\nMIIEv...\n-----END PRIVATE KEY-----\n",
  "ClientEmail": "service-account@my-gcp-project.iam.gserviceaccount.com",
  "ClientId": "1234567890",
  "BucketName": "flowfiles"
}
```

---

## Input Parameters

Each operation accepts specific parameters:

### Create
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path where the new object is created.|

### Delete
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to delete.        |

### Exist
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to check.         |

### List
| Parameter         | Type    | Required | Description                                         |
|--------------------|---------|----------|-----------------------------------------------------|
| `Path`             | string  | Yes      | The prefix path to list objects from.              |
| `Filter`           | string  | No       | A filter pattern for object names.                 |
| `Recurse`          | bool    | No       | Whether to list recursively. Default: `false`.     |
| `CaseSensitive`    | bool    | No       | Whether the filter is case-sensitive. Default: `false`. |
| `IncludeMetadata`  | bool    | No       | Whether to include object metadata. Default: `false`. |
| `MaxResults`       | int     | No       | Maximum number of objects to list. Default: `2147483647`. |

### Purge
| Parameter     | Type    | Required | Description                                    |
|---------------|---------|----------|------------------------------------------------|
| `Path`        | string  | Yes      | The path prefix to purge.                     |
| `Force`       | bool    | No       | Whether to force deletion without confirmation.|

### Read
| Parameter     | Type    | Required | Description                              |
|---------------|---------|----------|------------------------------------------|
| `Path`        | string  | Yes      | The path of the object to read.          |

### Write
| Parameter     | Type    | Required | Description                                                  |
|---------------|---------|----------|--------------------------------------------------------------|
| `Path`        | string  | Yes      | The path where data should be written.                      |
| `Data`        | object  | Yes      | The data to write to the object.                             |
| `Overwrite`   | bool    | No       | Whether to overwrite if the object already exists. Default: `false`. |

### Example input (Write)

```json
{
  "Operation": "write",
  "Path": "documents/report.json",
  "Data": {
    "title": "Monthly Report",
    "content": "This is the report content."
  },
  "Overwrite": true
}
```

---

## Debugging Tips

- Verify the `ProjectId`, `PrivateKeyId`, `PrivateKey`, `ClientEmail`, `ClientId`, and `BucketName` values are correct and have sufficient permissions.
- Ensure the `Path` is valid and does not conflict with existing objects (especially for create/write).
- For write operations, confirm that `Data` is properly encoded or formatted for upload.
- When using list, adjust filters carefully to match object names (wildcards like `*.txt` are supported).
- Purge will fail on non-empty folders unless `Force` is set to `true`.

---

## Google Cloud Storage Considerations

- Case Sensitivity: Object paths are case-sensitive; use the `CaseSensitive` flag in list if needed.
- Hierarchy Simulation: Google Cloud Storage uses a flat namespace. "Folders" are simulated using object path prefixes (e.g., `folder1/file.txt`).
- Large File Uploads: For large files, uploads are automatically chunked by the plugin.
- Metadata: When using `IncludeMetadata` in list, additional API calls may increase latency.

---

## Security Notes

- The plugin uses Google Cloud service account authentication with the provided credentials.
- No credentials or object data are persisted outside the execution scope unless explicitly configured.
- Only authorized FlowSynx platform users can view or modify plugin configurations.

---

## License

© FlowSynx. All rights reserved.