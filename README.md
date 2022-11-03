# Data Protection Key Gen

Command line utility to generate an ASP.NET Core Data Protection Key file. 

## Usage

``` bash
Description:
  Generate a DataProtection Key File

Usage:
  keygen [options]

Options:
  -d, --days <days> (REQUIRED)  Number of days until key expiration
  -p, --path <path> (REQUIRED)  Path to save key file to
  -k, --keep-files              Keep files in file system (vs. delete once complete [default: False]

```

## How it works

When running this utility, it will create a new key file with an expiration date set the number of `--days` specified. The standard output stream will contain the text content of the file that can be saved or fed into another process. 
By default, the file will not be permanently saved to disk, the `--path` option is a temporary location for the Data Protection SDK. Alteratively, you can leave the files on disk at the `--path` location by using the `--keep-files` flag.