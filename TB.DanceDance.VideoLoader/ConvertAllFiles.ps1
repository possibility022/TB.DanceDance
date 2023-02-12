$ffmpg = "D:\Programy\ffmpeg-2022-12-04-git-6c814093d8-full_build\bin\ffmpeg.exe";
$commandArgs = '-i [INPUT_FILE_PATH] -c:v libvpx-vp9 -b:v 2M [OUTPUT_FILE_PATH]'
$folderWithVideos = "G:\West"

$skipVideos = @(
    "20220209_183940"
)

$filesToConvert = Get-ChildItem -Path $folderWithVideos -Filter "*.mp4"
$filesToConvert = $filesToConvert | Where-Object $skipVideos.Contains($_.Name) -eq $fase

foreach($file in $filesToConvert)
{
    Write-Output "Converting ${$file.FullName}"
    $outputFile = $file.FullName.TrimEnd($file.Extension) + ".webm"
    $a = $commandArgs.Replace("[INPUT_FILE_PATH]", $file.FullName).Replace("[OUTPUT_FILE_PATH]", $outputFile)

    $command = "$ffmpg $a"
    Write-Output "Invoking command: '$command'"
    Invoke-Expression -Command $command
}

