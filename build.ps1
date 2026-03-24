function New-Plugin {
    param (
        [string] $Name
    )
    dotnet publish $Name/$Name.Plugin -c Release -r win-x64 --no-self-contained
    New-Item -ItemType Directory -Path plugins -Force | Out-Null
    Compress-Archive -Path $Name/$Name.Plugin/bin/Release/win-x64/publish/* -DestinationPath plugins/$Name.zip -Force
}

New-Plugin -Name "PIA"
New-Plugin -Name "Uuid"