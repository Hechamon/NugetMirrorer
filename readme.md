# This is a project that mirrors one nuget feed to another

## How to use the tool
``` text
Usage: NugetMirrorer [--source <String>] [--destination <String>] [--search <String>] [--api-key <String>] [--dry-run] [--max-age-days <Int32>] [--with-dependencies=<true|false>] [--log-level <LogLevel>] [--help] [--version]

NugetMirrorer

Options:
  --source <String>                 (Required)
  --destination <String>            (Required)
  --search <String>         
  --api-key <String>          
  --dry-run
  --max-age-days <Int32>
  --with-dependencies=<true|false>  (Default: True)
  --log-level <LogLevel>            (Default: Minimal) (Allowed values: Debug, Verbose, Information, Minimal, Warning, Error)
  -h, --help                        Show help message
  --version                         Show version
```