param(
    [Alias('Address','Server')]
    [string]$TargetHost = '127.0.0.1',

    [Alias('PortNumber','P')]
    [int]$Port = 9000,

    [int]$TimeoutMs = 3000
)

$client = New-Object System.Net.Sockets.TcpClient
$client.Connect($TargetHost, $Port)

$stream = $client.GetStream()
$stream.ReadTimeout  = $TimeoutMs
$stream.WriteTimeout = $TimeoutMs

$reader = New-Object System.IO.StreamReader($stream)
$writer = New-Object System.IO.StreamWriter($stream)
$writer.NewLine  = "`r`n"
$writer.AutoFlush = $true

# Try to read the backend banner (echo-backend 127.0.0.1:PORT)
try {
    $banner = $reader.ReadLine()
    if ($banner) {
        Write-Host "Connected to: $banner"
    } else {
        Write-Host "No banner received."
    }
} catch {
    Write-Host "No banner received (timeout)."
}

# Send a line and read the echo
$writer.WriteLine("hello through load balancer connected to $banner")
$response = $reader.ReadLine()
Write-Host "Server echoed: $response"

$client.Close()