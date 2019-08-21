function KillProcessesListenOn([string[]] $ports) {
# PS C:\Users\deping> netstat -ano

# 活动连接

#   协议  本地地址          外部地址        状态           PID
#   TCP    0.0.0.0:135            0.0.0.0:0              LISTENING       1088    
    $lines = netstat -ano | select -skip 4 | `
    % {$a = $_ -split ' {3,}'; New-Object 'PSObject' -Property @{Original=$_;Fields=$a}} | `
    ? { ($_.Fields[3] -match '^LISTENING$') }

    ForEach ($port in $ports) {
        $tmp = $lines | ? { ($_.Fields[1] -match ($port + '$')) };
        if ($tmp.Length -gt 0) {
            taskkill /F /PID $tmp[0].Fields[4];
            Write-Output ("Process listens on port " + $port + " is killed.")
        } else {
            Write-Output ("No process listens on port " + $port + ".")
        }
    }
}


# KillProcessesListenOn @('6003', '8090', '9993', '8761')
# java -jar .\eureka-server-0.0.1-SNAPSHOT.jar # port 8761
# java -jar .\calc-service-0.0.1-SNAPSHOT.jar # port 9993
# java -jar .\spring-apigateway-0.0.1-SNAPSHOT.jar # port 8090
# java -jar .\ruoyi-admin.jar # port 6003