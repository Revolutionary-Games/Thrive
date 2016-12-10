function Select-Directory {
    param(
        [string]$Title,
        [string]$Directory
    )
    
    $app = new-object -com Shell.Application
    $folder = $app.BrowseForFolder(0, $Title, 0, $ssfDRIVES)
    if ($folder.Self.Path -ne "") {
        return $folder.Self.Path
    }
    else {
        return $null
    }
}



function Unzip {
    param(
        [string]$filename,
        [string]$directory
    )
    $shell_app=new-object -com shell.application
    $archive = $shell_app.namespace($filename)
    $destination = $shell_app.namespace($directory)
    $destination.Copyhere($archive.items(), 0x14)
}