$ffmpg = "D:\Programy\ffmpeg-2022-12-04-git-6c814093d8-full_build\bin\ffmpeg.exe";
$output = "C:\Users\TomaszBak\Downloads\VID_20221028_161408.webm"
$inputVideo = "C:\Users\TomaszBak\Downloads\VID_20221028_161408.mp4"

$a = @("-i",
$inputVideo,
"-c:v libvpx-vp9 -keyint_min 150",
"-g 150 -tile-columns 4 -frame-parallel 1 -f webm -dash 1",
"-an -vf scale=160:90 -b:v 250k -dash 1 video_160x90_250k.webm",
"-an -vf scale=320:180 -b:v 500k -dash 1 video_320x180_500k.webm",
"-an -vf scale=640:360 -b:v 750k -dash 1 video_640x360_750k.webm",
"-an -vf scale=640:360 -b:v 1000k -dash 1 video_640x360_1000k.webm",
"-an -vf scale=1280:720 -b:v 1500k -dash 1 video_1280x720_1500k.webm"
)
$a = [System.String]::Join(" ", $a)
$command = "$ffmpg $a"

Invoke-Expression -Command $command