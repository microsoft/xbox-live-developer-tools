
function Reset-XBLProgresss {
    param
    (
        #[parameter(DontShow, Mandatory=$true)]
        #[string]$token = @{"Set-XBLDevXDPCredential"},

        [Parameter(Mandatory = $true)]
        [string]$ServiceConfigId,

        [Parameter(Mandatory = $true)]
        [string]$SandBox,

        [Parameter(Mandatory = $true)]
        [string[]]$XboxUserIds,

        [Parameter(DontShow)]
        [ValidateSet('prod', 'dnet')]
        [string[]]$Environment = "prod"
    )

    Begin
    {
        Write-Debug "Begin"
        #$token = Set-XBLDevXDPCredential
        Write-Output "Auth succeeded."
    }

    Process
    {
        $toolEndpoint = "https://jobs.gtm.prod.live"
        if ($Environment.ToLower() -eq "dnet"){
            $toolEndpoint = "http://jobs.dnet.gtm.nonprod.live"
        }

        $tokenHeader = 'XBL3.0 x=-;' + $token

        $headers= @{
            "Authorization"=$tokenHeader
             "X-Xbl-Contract-Version"="100"
        }

        $jobs= New-Object System.Collections.ArrayList

        # submit reset jobs 
        foreach ($xboxuserId in $XboxUserIds) {
            
            try{
                # Prepare request body
                $body = @{
                    "JobType" = "deletedata"
                    "JobProperties" = @{
                        "Scid" = $ServiceConfigId
                        "UserId" = $xboxuserId
                    }
                }

                $jsonBody = ConvertTo-Json $body -Compress

                # Send request for submitting delete job
                $restResult = Invoke-RestMethod -Uri "$toolEndpoint/submitJob" -Method Post -Headers $headers -Body $jsonBody
                $jobId = $restResult

                $jobs.Add(@{ JobId = $jobId
                    XboxUserId = $xboxuserId
                    Status = "Submitted"
                }) > $null
                Write-Output "Jobs for resetting user $xboxuserid are submitted."
            }
            catch{
                Write-Error -Exception $_.Exception -Message "Jobs submitting for resetting user $xboxuserId failed."
            }
        }

        if ($jobs.count -eq 0){
            Write-Error -Message "Submitting resetting jobs failed."
            return;
        }

        Write-Output "All jobs are submitted, you can wait for jobs to finish or press ctrl+c and all jobs will be running on cloud."

        # Polling job status
        while ($jobs.count -ne 0)
        {
            Write-Output "Checking jobs status"
            foreach($job in $jobs){
                
                $restResult = Invoke-RestMethod -Uri "$toolEndpoint/jobs/$($job.JobId)" -Method Get -Headers $headers

                $job.Status = $restResult.status
                if ($job.Status -eq "CompletedSuccess") {
                    Write-Output "$($job.XboxUserId) progress has been reset."
                } elseif ($job.Status -eq "CompletedError") {
                    Write-Output "$($job.XboxUserId) progress resetting has failed, please try again."
                }
            }

            $jobs = Where-Object -InputObject $jobs -FilterScript {$_.Status -eq "InProgress"}
            if ($jobs.length -ne 0){
                Sleep(2)
            }
        }

        Write-Output "All users progress resetting jobs have finished."
    }
}