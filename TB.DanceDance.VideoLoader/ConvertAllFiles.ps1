$ffmpg = "D:\Programy\ffmpeg-2022-12-04-git-6c814093d8-full_build\bin\ffmpeg.exe";
$commandArgs = '-i [INPUT_FILE_PATH] -c:v libvpx-vp9 -b:v 2M [OUTPUT_FILE_PATH]'
$folderWithVideos = "E:\WestCoastSwing"

$skipVideos = @(
    "20230127_235937.mp4",
    "20230129_000150.mp4",
    "20221005_183923.mp4",
    "20221019_183612.mp4"
)

$filesToConvert = Get-ChildItem -Path $folderWithVideos -Filter "*.mp4"

foreach($file in $filesToConvert)
{

    if ($skipVideos.Contains($file.Name))
    {
        Write-Output "Skipping ${file.Name}"
        continue
    }

    Write-Output "Converting ${$file.FullName}"
    $outputFile = $file.FullName.TrimEnd($file.Extension) + ".webm"
    $a = $commandArgs.Replace("[INPUT_FILE_PATH]", $file.FullName).Replace("[OUTPUT_FILE_PATH]", $outputFile)

    $command = "$ffmpg $a"
    Write-Output "Invoking command: '$command'"
    Invoke-Expression -Command $command
}

